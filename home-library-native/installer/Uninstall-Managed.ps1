param([Parameter(Mandatory=$true)][string]$Root)
$ErrorActionPreference = 'Stop'
try {
    Add-Type -AssemblyName System.Windows.Forms
    $Service = Get-CimInstance Win32_Service -Filter "Name='HomeLibraryNative'"
    $Expected = '"' + (Join-Path $Root 'server\HomeLibrary.Server.exe') + '"'
    if ($Service -and $Service.PathName -ne $Expected) { throw 'The service belongs to another installation. Uninstall it from that installation instead.' }
    if ($Service) {
        Stop-Service HomeLibraryNative
        (Get-Service HomeLibraryNative).WaitForStatus('Stopped',[TimeSpan]::FromSeconds(30))
        & sc.exe delete HomeLibraryNative | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Could not remove the service.' }
    }
    Get-Process -Name HomeLibrary.Tray -ErrorAction SilentlyContinue | Stop-Process -Force
    foreach ($Name in @('Home Library Public Catalog','Home Library DDNS')) {
        $Task = Get-ScheduledTask -TaskName $Name -ErrorAction SilentlyContinue
        if ($Task -and ($Task.Actions.Arguments -like "*$Root*")) {
            Stop-ScheduledTask -TaskName $Name -ErrorAction SilentlyContinue
            Unregister-ScheduledTask -TaskName $Name -Confirm:$false
        }
    }
    Get-NetFirewallRule -DisplayName 'Home Library paired phones','Home Library discovery','Home Library public catalog','Carribrary paired phones','Carribrary discovery' -ErrorAction SilentlyContinue | Remove-NetFirewallRule
    exit 0
} catch {
    [System.Windows.Forms.MessageBox]::Show($_.Exception.Message,'Carribrary uninstall', 'OK', 'Error') | Out-Null
    exit 1
}
