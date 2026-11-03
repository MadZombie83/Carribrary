param([Parameter(Mandatory=$true)][string]$Root,[string]$SavedSettings='', [string]$BackupPath='')
$ErrorActionPreference = 'Stop'
$Service = $null
try {
    Add-Type -AssemblyName System.Windows.Forms
    . (Join-Path $Root 'Setup-Progress.ps1')
    Start-CarribrarySetupProgress 'Carribrary - configuring installation'
    Write-CarribrarySetupStep 'Verifying the installed program files...'
    . (Join-Path $Root 'Repair-DataPermissions.ps1')
    . (Join-Path $Root 'Recovery.Core.ps1')
    $Release=Get-Content -LiteralPath (Join-Path $Root 'release-files.json') -Raw | ConvertFrom-Json
    if ($Release.version -ne '2.0.58-preview' -or @($Release.files).Count -lt 2 -or !($Release.files.path -contains 'server/HomeLibrary.Server.dll') -or !($Release.files.path -contains 'tray/HomeLibrary.Tray.dll')) { throw 'The installed package release could not be verified.' }
    foreach ($File in $Release.files) {
        Update-CarribrarySetupProgress
        $Relative=$File.path.Replace('\','/')
        if ($Relative -notmatch '^(server|tray)/' -or $Relative.Contains('..') -or $Relative.Contains(':') -or (Get-CarribraryHash (Join-Path $Root $Relative)) -ne $File.sha256) { throw 'The program files did not pass package verification. Recovery will be offered.' }
    }
    $Server = Join-Path $Root 'server\HomeLibrary.Server.exe'
    $Settings = Join-Path $Root 'server\appsettings.json'
    if ($SavedSettings -and (Test-Path $SavedSettings)) {
        $Attributes=[IO.File]::GetAttributes($Settings)
        [IO.File]::SetAttributes($Settings,($Attributes -band (-bnot [IO.FileAttributes]::ReadOnly)))
        [IO.File]::WriteAllText($Settings,[IO.File]::ReadAllText($SavedSettings),(New-Object Text.UTF8Encoding($false)))
    }
    $Config = Get-Content $Settings -Raw | ConvertFrom-Json
    $Data = Join-Path $env:ProgramData 'HomeLibraryNative\data'
    if ($Config.Library.DataDirectory) { $Data = [IO.Path]::GetFullPath($Config.Library.DataDirectory) }
    if ($Data.TrimEnd('\').StartsWith($Root.TrimEnd('\')+'\',[StringComparison]::OrdinalIgnoreCase)) { throw 'The data folder must be outside the program installation folder so updates and uninstall preserve it.' }
    Write-CarribrarySetupStep 'Preserving settings and configuring service permissions...'
    New-Item -ItemType Directory -Force -Path $Data | Out-Null
    & icacls.exe $Root /grant '*S-1-5-19:(OI)(CI)RX' /T | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Unable to grant the service access to the program files.' }
    Repair-LibraryDataPermissions $Data
    Save-LibraryInstallationSettings $Settings $Data
    if (!(Test-Path 'HKLM:\Software\HomeLibraryNative')) { New-Item 'HKLM:\Software\HomeLibraryNative' -Force | Out-Null }
    Set-ItemProperty 'HKLM:\Software\HomeLibraryNative' -Name DataDirectory -Value $Data
    Write-CarribrarySetupStep 'Preparing the local HTTPS identity...'
    Invoke-CarribrarySetupCommand -FilePath $Server -Arguments @('--prepare-windows-identity') -OutputPath (Join-Path $Data 'installer-identity.log') -TimeoutSeconds 180
    if ($BackupPath) {
        if (-not (Test-Path -LiteralPath $BackupPath -PathType Leaf)) { throw 'The selected backup file was not found.' }
        $Mode = '--import'
        if ([IO.Path]::GetExtension($BackupPath) -eq '.sqlite') { $Mode = '--import-db' }
        Write-CarribrarySetupStep 'Importing the selected backup and checking its records...'
        Invoke-CarribrarySetupCommand -FilePath $Server -Arguments @($Mode,$BackupPath) -OutputPath (Join-Path $Data 'installer-import.log') -TimeoutSeconds 900
    }
    Write-CarribrarySetupStep 'Registering the service and firewall rules...'
    $Service = Get-Service HomeLibraryNative -ErrorAction SilentlyContinue
    if ($Service) {
        $Registered=Get-CimInstance Win32_Service -Filter "Name='HomeLibraryNative'"
        $Changed=Invoke-CimMethod -InputObject $Registered -MethodName Change -Arguments @{PathName=('"'+$Server+'"');StartMode='Automatic'}
        if ($Changed.ReturnValue -ne 0) { throw ('Unable to update the library service. Windows code: '+$Changed.ReturnValue) }
    } else {
        New-Service -Name HomeLibraryNative -DisplayName 'Carribrary' -BinaryPathName ('"' + $Server + '"') -StartupType Automatic -Description 'Household library database and paired-phone API' -Credential (New-Object System.Management.Automation.PSCredential('NT AUTHORITY\LocalService', (New-Object System.Security.SecureString))) | Out-Null
        $Service = Get-Service HomeLibraryNative
    }
    Set-Service -Name HomeLibraryNative -DisplayName 'Carribrary'
    & sc.exe failure HomeLibraryNative reset= 86400 actions= restart/15000/restart/30000/restart/60000 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Unable to configure automatic service recovery.' }
    Get-NetFirewallRule -DisplayName 'Home Library paired phones','Home Library discovery','Carribrary paired phones','Carribrary discovery' -ErrorAction SilentlyContinue | Remove-NetFirewallRule
    New-NetFirewallRule -DisplayName 'Carribrary paired phones' -Direction Inbound -Action Allow -Protocol TCP -LocalPort $Config.Library.PhonePort -Program $Server -Profile Private,Public | Out-Null
    New-NetFirewallRule -DisplayName 'Carribrary discovery' -Direction Inbound -Action Allow -Protocol UDP -LocalPort $Config.Library.DiscoveryPort -RemoteAddress LocalSubnet -Program $Server -Profile Private | Out-Null
    Write-CarribrarySetupStep 'Starting Carribrary and waiting for its health check...'
    Start-Service HomeLibraryNative
    $Ready = $false
    for ($Attempt=0; $Attempt -lt 30; $Attempt++) {
        try { $Response = Invoke-WebRequest -UseBasicParsing -Uri "http://localhost:$($Config.Library.AdminPort)/local-auth/health" -TimeoutSec 2; if ($Response.StatusCode -eq 200 -and ($Response.Content | ConvertFrom-Json).ready) { $Ready=$true; break } } catch {}
        Update-CarribrarySetupProgress
        Start-Sleep -Seconds 1
    }
    if (-not $Ready) { throw 'The service did not become ready. Check Carribrary in Windows Services and the Windows Application event log.' }
    Set-Content (Join-Path $Root 'Open-Library.url') "[InternetShortcut]`r`nURL=http://localhost:$($Config.Library.AdminPort)/`r`nIconFile=$Root\Carribrary.ico`r`nIconIndex=0" -Encoding ASCII
    Set-Content (Join-Path $Root 'Setup-and-Updates.url') "[InternetShortcut]`r`nURL=http://localhost:$($Config.Library.AdminPort)/?setup=1`r`nIconFile=$Root\Carribrary.ico`r`nIconIndex=0" -Encoding ASCII
    Write-CarribrarySetupStep 'Installation verified. Carribrary is ready to open.'
    exit 0
} catch {
    # Keep the original exception; a failed retry must not mask the actual failure.
    $Failure=$_.Exception.Message
    try { & (Join-Path $Root 'Collect-Diagnostics.ps1') } catch {}
    if(Get-Command Show-CarribrarySetupFailure -ErrorAction SilentlyContinue){Show-CarribrarySetupFailure ($Failure+' A diagnostic report has also been opened if collection succeeded.')}else{[System.Windows.Forms.MessageBox]::Show($Failure,'Carribrary setup','OK','Error') | Out-Null}
    exit 1
}

finally {if(Get-Command Close-CarribrarySetupProgress -ErrorAction SilentlyContinue){Close-CarribrarySetupProgress}}
