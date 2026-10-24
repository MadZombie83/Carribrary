# Optional: exposes ONLY the read-only catalog, never the admin or phone API.
param([Parameter(Mandatory=$true)][string]$Hostname)
$ErrorActionPreference = 'Stop'
if ($Hostname -notmatch '^(?=.{1,253}$)[a-zA-Z0-9](?:[a-zA-Z0-9.-]*[a-zA-Z0-9])?\.[a-zA-Z]{2,63}$') { throw 'Enter a DNS hostname only.' }
$ConfigPath = Join-Path $PSScriptRoot 'server\appsettings.json'
$Config = Get-Content $ConfigPath -Raw | ConvertFrom-Json
$Proxy = Join-Path $PSScriptRoot 'public-proxy'
New-Item -ItemType Directory -Force -Path $Proxy | Out-Null
$Caddy = Join-Path $Proxy 'caddy.exe'
if (-not (Test-Path $Caddy)) {
    Write-Host 'Downloading the Windows x64 Caddy executable from caddyserver.com...'
    Invoke-WebRequest -UseBasicParsing -Uri 'https://caddyserver.com/api/download?os=windows&arch=amd64' -OutFile $Caddy
}
$ProxyData = Join-Path $env:ProgramData 'HomeLibraryNative\public-proxy'
New-Item -ItemType Directory -Force -Path $ProxyData | Out-Null
& icacls.exe $ProxyData /inheritance:r /grant:r '*S-1-5-32-544:(OI)(CI)F' '*S-1-5-18:(OI)(CI)F' '*S-1-5-19:(OI)(CI)M' /T | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Could not secure the public proxy folder.' }
& icacls.exe $Proxy /grant '*S-1-5-19:(OI)(CI)RX' /T | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Could not grant proxy read/execute access.' }
$Caddyfile = Join-Path $Proxy 'Caddyfile'
@"
${Hostname} {
    reverse_proxy 127.0.0.1:$($Config.Library.CatalogPort)
}
"@ | Set-Content $Caddyfile -Encoding ASCII
$Runner = Join-Path $Proxy 'Run-PublicProxy.ps1'
@'
$ErrorActionPreference = 'Stop'
$env:APPDATA = Join-Path $env:ProgramData 'HomeLibraryNative\public-proxy'
$env:LOCALAPPDATA = $env:APPDATA
& (Join-Path $PSScriptRoot 'caddy.exe') run --config (Join-Path $PSScriptRoot 'Caddyfile') --adapter caddyfile
exit $LASTEXITCODE
'@ | Set-Content $Runner -Encoding UTF8
& $Caddy validate --config $Caddyfile --adapter caddyfile
if ($LASTEXITCODE -ne 0) { throw 'Public proxy configuration is invalid.' }
$Config.Library.PublicCatalogOrigin = "https://${Hostname}"
$Config | ConvertTo-Json -Depth 6 | Set-Content $ConfigPath -Encoding UTF8
$Action = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument ('-NoProfile -ExecutionPolicy RemoteSigned -File "' + $Runner + '"')
$Trigger = New-ScheduledTaskTrigger -AtStartup
$Principal = New-ScheduledTaskPrincipal -UserId 'NT AUTHORITY\LOCAL SERVICE' -LogonType ServiceAccount
$Settings = New-ScheduledTaskSettingsSet -ExecutionTimeLimit ([TimeSpan]::Zero) -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 1) -StartWhenAvailable
Stop-ScheduledTask -TaskName 'Home Library Public Catalog' -ErrorAction SilentlyContinue
Register-ScheduledTask -TaskName 'Home Library Public Catalog' -Action $Action -Trigger $Trigger -Principal $Principal -Settings $Settings -Force | Out-Null
Get-NetFirewallRule -DisplayName 'Home Library public catalog' -ErrorAction SilentlyContinue | Remove-NetFirewallRule
New-NetFirewallRule -DisplayName 'Home Library public catalog' -Direction Inbound -Action Allow -Protocol TCP -LocalPort 80,443 -Program $Caddy -Profile Private,Public | Out-Null
Restart-Service HomeLibraryNative
Start-ScheduledTask -TaskName 'Home Library Public Catalog'
Write-Host "Public origin: https://${Hostname}"
Write-Host 'Forward router TCP 80 and 443 to this computer. Caddy needs these ports available to issue/renew browser-trusted certificates.'
Write-Host 'After HTTPS works from a phone on cellular, use the admin Share catalog QR button. Sharing stays disabled until you explicitly create the link.'
Write-Host 'Native phones continue connecting directly to 7443. Friends see books only; they cannot edit or view borrowers and loans.'
