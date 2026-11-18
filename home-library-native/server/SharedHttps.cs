using System.Text.Json;

namespace HomeLibrary;

public sealed record SharedHttpsInput(string Hostname, bool UseDuckDns = false);
public sealed record SharedSite(string Name, string Hostname, int Port);
public sealed record SharedSiteChange(SharedSite? Site = null, string Remove = "", bool ConfirmPublic = false);
public sealed record JoinSharedHttps(string Hostname, string DuckDnsToken = "", bool ClearToken = false);

public static class SharedHttps {
    public const int FamilyPort = 8123;
    private static readonly HttpClient LocalHttp = new(new SocketsHttpHandler {
        AllowAutoRedirect = false, UseProxy = false
    }) { Timeout = TimeSpan.FromSeconds(3), MaxResponseContentBufferSize = 4096 };

    public static SharedSite ValidateSite(SharedSite site, string primary, IEnumerable<string> claimed, IEnumerable<int> reserved) {
        var name = site.Name?.Trim() ?? "";
        var hostname = PublicConnection.Hostname(site.Hostname);
        if (name.Length is <1 or >80 || name.Any(char.IsControl)) throw new LibraryError("Enter an app name from 1 to 80 characters.");
        if (site.Port is <1024 or >65535 || reserved.Contains(site.Port) || site.Port is 2019 or 4173 or 4174 or 7443 or 7445 or 7446 or 8124)
            throw new LibraryError("Enter the app's local web port. Admin, pairing, and reserved ports are blocked.");
        if (new[]{primary}.Concat(claimed).Any(h => hostname.Equals(h, StringComparison.OrdinalIgnoreCase)))
            throw new LibraryError("That public hostname is already in use.");
        return new(name, hostname, site.Port);
    }

    public static string RenderSite(SharedSite site) {
        var host = PublicConnection.Hostname(site.Hostname);
        if (site.Port is <1024 or >65535) throw new LibraryError("Invalid local web port.");
        return $"{host} {{\n    reverse_proxy 127.0.0.1:{site.Port}\n}}\nhttp://{host} {{\n    redir https://{host}{{uri}} 308\n}}\n";
    }

    public static string ValidateHostname(string value, string libraryHostname, IReadOnlyList<HostedProxyRoute> routes) {
        var hostname = PublicConnection.Hostname(value);
        if (!System.Text.RegularExpressions.Regex.IsMatch(hostname, @"^[a-z0-9][a-z0-9-]{0,62}\.duckdns\.org$"))
            throw new LibraryError("Enter a separate DuckDNS hostname such as family.duckdns.org.");
        if (hostname.Equals(libraryHostname, StringComparison.OrdinalIgnoreCase) ||
            routes.Any(route => hostname.Equals(route.Hostname, StringComparison.OrdinalIgnoreCase)))
            throw new LibraryError("That hostname already belongs to a library. Choose a different DuckDNS hostname.");
        return hostname;
    }

    public static IReadOnlyList<HostedProxyRoute> WithFamilyRoute(IReadOnlyList<HostedProxyRoute> routes, SharedHttpsInput? family) {
        if (family is null) return routes;
        return [..routes, new("8ca758b7c7094687a6fba091d43072af", family.Hostname, "127.0.0.1", FamilyPort, FamilyCommandCenter:true)];
    }

    public static async Task<string> FindFamilyHostname(CancellationToken stop) {
        try {
            using var response = await LocalHttp.GetAsync($"http://127.0.0.1:{FamilyPort}/api/shared-https-probe", stop);
            response.EnsureSuccessStatusCode();
            using var health = JsonDocument.Parse(await response.Content.ReadAsStringAsync(stop));
            var info = health.RootElement;
            if (info.ValueKind == JsonValueKind.Object &&
                info.TryGetProperty("product", out var product) &&
                product.ValueKind == JsonValueKind.String &&
                product.GetString() == "family-command-center" &&
                info.TryGetProperty("sharedHttpsOrigin", out var origin) &&
                origin.ValueKind == JsonValueKind.String &&
                Uri.TryCreate(origin.GetString(), UriKind.Absolute, out var url) &&
                origin.GetString() == "https://" + url.Host) return url.Host;
        } catch (Exception error) when (error is HttpRequestException or JsonException ||
                                      error is OperationCanceledException && !stop.IsCancellationRequested) { }
        return "";
    }

    public static async Task VerifyFamily(CancellationToken stop, string hostname) {
        if (await FindFamilyHostname(stop) != hostname)
            throw new LibraryError("Open Family Command Center on this PC, save Join Carribrary HTTPS with the same hostname, and wait for it to restart.");
    }
}
