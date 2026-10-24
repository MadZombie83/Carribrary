# Run as Administrator. Installs only the new service; it does not modify the old app.
$ErrorActionPreference = 'Stop'
$Server = Join-Path $PSScriptRoot 'server\HomeLibrary.Server.exe'
if (-not (Test-Path $Server)) { throw 'Extract the entire Windows ZIP first.' }
& icacls.exe $PSScriptRoot /grant '*S-1-5-19:(OI)(CI)RX' /T | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Could not grant service read/execute access to binaries.' }
$Data = Join-Path $env:ProgramData 'HomeLibraryNative\data'
New-Item -ItemType Directory -Force -Path $Data | Out-Null
& icacls.exe $Data /inheritance:r /grant:r '*S-1-5-32-544:(OI)(CI)F' '*S-1-5-18:(OI)(CI)F' '*S-1-5-19:(OI)(CI)M' /T | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Could not secure the data folder.' }
& $Server --prepare-windows-identity
if ($LASTEXITCODE -ne 0) { throw 'Could not prepare the Windows HTTPS identity.' }
$Existing = Get-Service -Name HomeLibraryNative -ErrorAction SilentlyContinue
if ($Existing) { throw 'HomeLibraryNative already exists. Stop it before updating binaries; then start it again.' }
New-Service -Name HomeLibraryNative -DisplayName 'Carribrary' -BinaryPathName ('"' + $Server + '"') -StartupType Automatic -Description 'Household library database and paired-phone API' -Credential (New-Object System.Management.Automation.PSCredential('NT AUTHORITY\LocalService', (New-Object System.Security.SecureString))) | Out-Null
& sc.exe failure HomeLibraryNative reset= 86400 actions= restart/15000/restart/30000/restart/60000 | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Could not configure service recovery.' }
$Config = Get-Content (Join-Path $PSScriptRoot 'server\appsettings.json') -Raw | ConvertFrom-Json
New-NetFirewallRule -DisplayName 'Carribrary paired phones' -Direction Inbound -Action Allow -Protocol TCP -LocalPort $Config.Library.PhonePort -Program $Server -Profile Private,Public | Out-Null
New-NetFirewallRule -DisplayName 'Carribrary discovery' -Direction Inbound -Action Allow -Protocol UDP -LocalPort $Config.Library.DiscoveryPort -RemoteAddress LocalSubnet -Program $Server -Profile Private | Out-Null
Start-Service HomeLibraryNative
$Tray = Join-Path $PSScriptRoot 'tray\HomeLibrary.Tray.exe'
if (Test-Path $Tray) { Start-Process $Tray }
Write-Host "Installed Carribrary. Admin: http://localhost:$($Config.Library.AdminPort)"
Write-Host "Data: $Data"
