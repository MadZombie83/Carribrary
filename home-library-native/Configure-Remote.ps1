param([Parameter(Mandatory=$true)][string]$Hostname,[string]$PublicCatalogOrigin='')
$ErrorActionPreference = 'Stop'
if ($Hostname -notmatch '^(?=.{1,253}$)[a-zA-Z0-9](?:[a-zA-Z0-9.-]*[a-zA-Z0-9])?\.[a-zA-Z]{2,63}$') { throw 'Enter a DNS hostname only, such as yourname.duckdns.org.' }
$Path = Join-Path $PSScriptRoot 'server\appsettings.json'
$Config = Get-Content $Path -Raw | ConvertFrom-Json
$Config.Library.RemoteAddress = "https://${Hostname}:$($Config.Library.PhonePort)"
if ($PublicCatalogOrigin) {
    $Uri = [Uri]$PublicCatalogOrigin
    if ($Uri.Scheme -ne 'https' -or $Uri.AbsolutePath -ne '/' -or $Uri.UserInfo -or $Uri.Query -or $Uri.Fragment) { throw 'Catalog origin must be an HTTPS origin only.' }
    $Config.Library.PublicCatalogOrigin = $PublicCatalogOrigin.TrimEnd('/')
}
$Config | ConvertTo-Json -Depth 6 | Set-Content $Path -Encoding UTF8
Write-Host "Phone address: $($Config.Library.RemoteAddress)"
Write-Host "Forward router TCP $($Config.Library.PhonePort) to this computer at the SAME port. Do not forward 4173, 4174 or 7445."
Write-Host 'Restart-Service HomeLibraryNative, then create a pairing QR in the local admin page.'
Write-Host 'Keep your DDNS record updated using your router DDNS feature or the separate DuckDNS setup script.'
