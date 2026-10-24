param([Parameter(Mandatory=$true)][string]$WorkDirectory,[Parameter(Mandatory=$true)][string]$Root)
$ErrorActionPreference = 'Stop'
try {
    Add-Type -AssemblyName System.Windows.Forms
    . (Join-Path $WorkDirectory 'Repair-DataPermissions.ps1')
    $Data = Join-Path $env:ProgramData 'HomeLibraryNative\data'
    $Service = Get-CimInstance Win32_Service -Filter "Name='HomeLibraryNative'"
    $Settings = $null
    if ($Service) {
        $Match = [regex]::Match($Service.PathName, '^"([^\"]+HomeLibrary\.Server\.exe)"$|^([^\"]+HomeLibrary\.Server\.exe)$', 'IgnoreCase')
        if (-not $Match.Success) { throw 'A different service already uses the HomeLibraryNative name. Nothing was changed.' }
        $Exe = $Match.Groups[1].Value
        if (-not $Exe) { $Exe = $Match.Groups[2].Value }
        $Settings = Join-Path (Split-Path $Exe) 'appsettings.json'
        $ServerProcess=$null
        if ($Service.ProcessId -gt 0) { $ServerProcess=Get-Process -Id $Service.ProcessId -ErrorAction SilentlyContinue }
        Stop-Service HomeLibraryNative
        (Get-Service HomeLibraryNative).WaitForStatus('Stopped',[TimeSpan]::FromSeconds(30))
        if ($ServerProcess -and !$ServerProcess.WaitForExit(30000)) { throw 'The library process has not exited. No recovery snapshot was created; wait for it to stop before updating.' }
    }
    if (Test-Path -LiteralPath $Data) {
        . (Join-Path $WorkDirectory 'Repair-DataPermissions.ps1')
        Repair-LibraryDataPermissions $Data
    }
    if (-not $Settings -or -not (Test-Path $Settings)) { $Settings = Join-Path $Data 'installation-appsettings.json' }
    if (Test-Path $Settings) { Copy-Item $Settings (Join-Path $WorkDirectory 'saved-appsettings.json') -Force }
    # The server is stopped before copying SQLite, account stores and keyrings.
    # Keep the private safety copy outside the live data directory.
    if (Test-Path (Join-Path $WorkDirectory 'saved-appsettings.json')) {
        $SavedConfig = Get-Content (Join-Path $WorkDirectory 'saved-appsettings.json') -Raw | ConvertFrom-Json
        if ($SavedConfig.Library.DataDirectory) { $Data = [IO.Path]::GetFullPath($SavedConfig.Library.DataDirectory) }
    }
    Get-Process -Name HomeLibrary.Tray -ErrorAction SilentlyContinue | Stop-Process -Force
    $Ports = @(4173,4174,7443)
    if (Test-Path (Join-Path $WorkDirectory 'saved-appsettings.json')) {
        $Config = Get-Content (Join-Path $WorkDirectory 'saved-appsettings.json') -Raw | ConvertFrom-Json
        $Ports = @($Config.Library.AdminPort,$Config.Library.CatalogPort,$Config.Library.PhonePort)
    }
    foreach ($Port in $Ports) {
        if (Get-NetTCPConnection -State Listen -LocalPort $Port -ErrorAction SilentlyContinue) {
            throw "Port $Port is already in use. If your previous library server is running, export its backup and close its window (or stop its startup task in Task Scheduler), then run this installer again. Plex can keep running."
        }
    }
    if (Test-Path $Data) { Repair-LibraryDataPermissions $Data }
    if (Test-Path (Join-Path $Data 'library.sqlite')) {
        if (Test-Path -LiteralPath (Join-Path $WorkDirectory 'saved-appsettings.json')) { Save-LibraryInstallationSettings (Join-Path $WorkDirectory 'saved-appsettings.json') $Data }
        . (Join-Path $WorkDirectory 'Recovery.Windows.ps1')
        $Version='unverified'
        try { $Version=Get-CarribraryProgramVersion $Root } catch { }
        $RecoveryHome=Join-Path $env:ProgramData 'HomeLibraryNative\recovery'
        $Point=New-CarribraryRecoveryPoint -Root $Root -Data $Data -RecoveryHome $RecoveryHome -SourceVersion $Version -Protect {param($Path) Protect-CarribraryRecovery $Path}
        Register-CarribraryRecoveryPoint $Point
        [IO.File]::WriteAllText((Join-Path $WorkDirectory 'recovery-point.txt'),$Point,(New-Object Text.UTF8Encoding($false)))
        if (!(Test-Path 'HKLM:\Software\HomeLibraryNative')) { New-Item 'HKLM:\Software\HomeLibraryNative' -Force | Out-Null }
        Set-ItemProperty 'HKLM:\Software\HomeLibraryNative' -Name DataDirectory -Value $Data

    }
    exit 0
} catch {
    if ($Service) { Start-Service HomeLibraryNative -ErrorAction SilentlyContinue }
    [System.Windows.Forms.MessageBox]::Show($_.Exception.Message,'Carribrary setup', 'OK', 'Error') | Out-Null
    exit 1
}
