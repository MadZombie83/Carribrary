. (Join-Path $PSScriptRoot 'Recovery.Core.ps1')
function Protect-CarribraryRecovery([string]$Path) {
    $Acl=New-Object Security.AccessControl.DirectorySecurity
    $Acl.SetAccessRuleProtection($true,$false)
    foreach ($Id in @('S-1-5-32-544','S-1-5-18')) {
        $Sid=New-Object Security.Principal.SecurityIdentifier($Id)
        $Rule=New-Object Security.AccessControl.FileSystemAccessRule($Sid,'FullControl','ContainerInherit,ObjectInherit','None','Allow'); $Acl.AddAccessRule($Rule)
    }
    Set-Acl -LiteralPath $Path -AclObject $Acl
}
function Register-CarribraryRecoveryPoint([string]$Point) {
    Get-CarribraryRecoveryPoint $Point | Out-Null
    $Id=Split-Path $Point -Leaf
    if ($Id -notmatch '^[0-9]{8}-[0-9]{6}-[a-f0-9]{32}$') { throw 'Invalid recovery point identity.' }
    $Key='HKLM:\Software\HomeLibraryNative\RecoveryPoints'
    if (!(Test-Path $Key)) { New-Item $Key -Force | Out-Null }
    Set-ItemProperty $Key -Name $Id -Value (Get-CarribraryHash (Join-Path $Point 'point.json'))
}
function Assert-CarribraryTrustedPoint([string]$Point) {
    Assert-CarribraryTree $Point
    $Id=Split-Path $Point -Leaf
    if ($Id -notmatch '^[0-9]{8}-[0-9]{6}-[a-f0-9]{32}$') { throw 'Invalid recovery point identity.' }
    $Key=Get-Item 'HKLM:\Software\HomeLibraryNative\RecoveryPoints' -ErrorAction Stop
    $Expected=$Key.GetValue($Id,'')
    if (!$Expected -or $Expected -ne (Get-CarribraryHash (Join-Path $Point 'point.json'))) { throw 'This point is not registered as a trusted local recovery point. Nothing was restored.' }
}
function Stop-CarribraryRecoveryService([string]$Root) {
    $Service=Get-CimInstance Win32_Service -Filter "Name='HomeLibraryNative'"
    if (!$Service -or $Service.PathName.Trim('"') -ne (Join-Path $Root 'server\HomeLibrary.Server.exe')) { throw 'The registered library service does not belong to this installation.' }
    $ServerProcess=$null
    if ($Service.ProcessId -gt 0) { $ServerProcess=Get-Process -Id $Service.ProcessId -ErrorAction SilentlyContinue }
    Stop-Service HomeLibraryNative
    (Get-Service HomeLibraryNative).WaitForStatus('Stopped',[TimeSpan]::FromSeconds(30))
    if ($ServerProcess -and !$ServerProcess.WaitForExit(30000)) { throw 'The library process has not exited. Recovery cannot copy open databases safely.' }
    Get-Process -Name HomeLibrary.Tray -ErrorAction SilentlyContinue | Stop-Process -Force
}
function Test-CarribraryRecoveryHealth([string]$Root) {
    $Config=Get-Content -LiteralPath (Join-Path $Root 'server/appsettings.json') -Raw | ConvertFrom-Json
    for ($Attempt=0;$Attempt -lt 20;$Attempt++) {
        try {
            $Origin='http://localhost:'+$Config.Library.AdminPort
            $Health=Invoke-WebRequest -UseBasicParsing -Uri ($Origin+'/local-auth/health') -TimeoutSec 2
            if ($Health.StatusCode -eq 200 -and ($Health.Content | ConvertFrom-Json).ready) { return $true }
        } catch { }
        Start-Sleep -Seconds 1
    }
    return $false
}
function Invoke-CarribraryWindowsRecovery([string]$Root,[string]$Point,[switch]$UseSavedSettings) {
    Assert-CarribraryTrustedPoint $Point
    $Data=Join-Path $env:ProgramData 'HomeLibraryNative\data'
    $SavedData=(Get-ItemProperty 'HKLM:\Software\HomeLibraryNative' -ErrorAction SilentlyContinue).DataDirectory
    if ($SavedData) { $Data=$SavedData }
    $SettingsPath=Join-Path $Root 'server/appsettings.json'
    if ($UseSavedSettings) { $SettingsPath=Join-Path $Data 'installation-appsettings.json' }
    try { $Config=Get-Content -LiteralPath $SettingsPath -Raw | ConvertFrom-Json; if (!$Config.Library.AdminPort) { throw 'Missing settings' }; if ($Config.Library.DataDirectory) { $Data=[IO.Path]::GetFullPath($Config.Library.DataDirectory) } }
    catch {
        if (!$SavedData) { $Meta=Get-CarribraryRecoveryPoint $Point; if ($Meta.root -ne (Get-CarribraryPath $Root)) { throw 'Recovery point belongs to another installation.' }; $Data=$Meta.data }
        $SettingsPath=Join-Path $Data 'installation-appsettings.json'
        $Config=Get-Content -LiteralPath $SettingsPath -Raw | ConvertFrom-Json
        if (!$Config.Library.AdminPort) { throw 'Current connection settings are unavailable. Private snapshots were retained.' }
    }
    $RecoveryHome=Join-Path $env:ProgramData 'HomeLibraryNative\recovery'
    # Use protected installed-version metadata if a damaged DLL cannot report its version.
    $Current='unverified'
    try { $Current=Get-CarribraryProgramVersion $Root } catch {
        $SavedVersion=(Get-ItemProperty 'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\HomeLibraryNative' -ErrorAction SilentlyContinue).DisplayVersion
        if ($SavedVersion) { $Current=$SavedVersion }
    }
    $Version=Restore-CarribraryProgram -Root $Root -Data $Data -RecoveryHome $RecoveryHome -Point $Point -CurrentVersion $Current -SettingsPath $SettingsPath -Stop { Stop-CarribraryRecoveryService $Root } -Start { Start-Service HomeLibraryNative } -Health { Test-CarribraryRecoveryHealth $Root } -Protect {param($Path) Protect-CarribraryRecovery $Path} -GrantRead {param($Path) & icacls.exe $Path /grant '*S-1-5-19:(OI)(CI)RX' /T | Out-Null; if ($LASTEXITCODE -ne 0) { throw 'Could not grant the library service access to the recovered application.' }}
    Set-ItemProperty 'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\HomeLibraryNative' -Name DisplayVersion -Value $Version
    return $Version
}
