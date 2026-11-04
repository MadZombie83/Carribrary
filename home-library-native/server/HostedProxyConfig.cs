using System.Security.Cryptography.X509Certificates;
using System.Text;
namespace HomeLibrary;
public static class HostedProxyConfig {
 public static string Render(string hostname,int catalogPort,IReadOnlyList<HostedProxyRoute> routes,string directory,bool windows){
  hostname=PublicConnection.Hostname(hostname);if(catalogPort is <1 or >65535)throw new LibraryError("Invalid catalog listener.");
  var b=new StringBuilder();b.Append("{\n    admin off\n    http_port ").Append(windows?80:8080).Append("\n    https_port ").Append(windows?443:8443).Append("\n    auto_https disable_redirects\n}\n");
  var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase){hostname};
  b.Append(hostname).Append(" {\n    reverse_proxy 127.0.0.1:").Append(catalogPort).Append("\n}\nhttp://").Append(hostname).Append(" {\n    redir https://").Append(hostname).Append("{uri} 308\n}\n");
  foreach(var route in routes){var host=PublicConnection.Hostname(route.Hostname);if(!Guid.TryParseExact(route.Id,"N",out _)||!names.Add(host)||route.Port is <1024 or >65535)throw new LibraryError("Every connected library needs its own hostname and valid listener.");
   b.Append(host).Append(" {\n");
   if(route.Certificate==""){if(route.Address!="127.0.0.1"||route.Token!=""||route.Port==catalogPort)throw new LibraryError("Invalid local library route.");b.Append("    reverse_proxy 127.0.0.1:").Append(route.Port).Append('\n');}
   else{
    if(!NetworkHosting.PrivateAddress(route.Address)||!System.Text.RegularExpressions.Regex.IsMatch(route.Token,@"^[A-F0-9]{64}$"))throw new LibraryError("Invalid private library route.");NetworkGateway.ValidateCertificate(route.Certificate,route.ServerName);
    Directory.CreateDirectory(directory);if(OperatingSystem.IsLinux())File.SetUnixFileMode(directory,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
    var file=Path.Combine(directory,"peer-"+route.Id+".pem");using(var cert=X509CertificateLoader.LoadCertificate(Convert.FromBase64String(route.Certificate)))File.WriteAllText(file,cert.ExportCertificatePem());if(OperatingSystem.IsLinux())File.SetUnixFileMode(file,UnixFileMode.UserRead|UnixFileMode.UserWrite);
    var quoted=file.Replace('\\','/').Replace("\"","\\\"");if(quoted.Any(char.IsControl))throw new LibraryError("The hosting storage path is not supported.");
    b.Append("    reverse_proxy https://").Append(route.Address).Append(':').Append(route.Port).Append(" {\n        header_up X-Carribrary-Gateway ").Append(route.Token).Append("\n        header_up Host {http.request.host}\n        transport http {\n            tls_server_name ").Append(route.ServerName).Append("\n            tls_trust_pool file \"").Append(quoted).Append("\"\n        }\n    }\n");
   }
   b.Append("}\nhttp://").Append(host).Append(" {\n    redir https://").Append(host).Append("{uri} 308\n}\n");
  }
  return b.ToString();
 }
}
