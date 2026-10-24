$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Security
$Data = Join-Path $env:ProgramData 'HomeLibraryNative\data'
$Domain = (Get-Content (Join-Path $Data 'duckdns-subdomain.txt') -Raw).Trim()
$Encrypted = [IO.File]::ReadAllBytes((Join-Path $Data 'duckdns-token.protected'))
$Bytes = [Security.Cryptography.ProtectedData]::Unprotect($Encrypted,$null,[Security.Cryptography.DataProtectionScope]::LocalMachine)
try {
    $Token = [Text.Encoding]::UTF8.GetString($Bytes)
    $Url = 'https://www.duckdns.org/update?domains=' + [Uri]::EscapeDataString($Domain) + '&token=' + [Uri]::EscapeDataString($Token) + '&ip='
    try { $Response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 20 } catch { throw 'DuckDNS update failed. Check your internet connection and account token.' }
    if ($Response.Content.Trim() -ne 'OK') { throw 'DuckDNS rejected the update. Check your account token and subdomain.' }
} finally { [Array]::Clear($Bytes,0,$Bytes.Length); $Token = $null; $Url = $null }
