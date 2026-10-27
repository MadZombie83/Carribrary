param([Parameter(Mandatory=$true)][string]$Root)
$ErrorActionPreference='Stop'
try {
    $Principal=New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    if (!$Principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        Start-Process -FilePath (Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe') -Verb RunAs -ArgumentList ('-NoProfile -STA -ExecutionPolicy Bypass -File "'+$PSCommandPath+'" -Root "'+$Root+'"') | Out-Null
        exit
    }
    $Maintenance=New-Object Threading.Mutex($false,'Global\CarribraryMaintenance')
    try {$Acquired=$Maintenance.WaitOne(0)} catch [Threading.AbandonedMutexException] {$Acquired=$true}
    if (!$Acquired) {throw 'Another Carribrary update/recovery tool is open. Close it first.'}
    & (Join-Path $Root 'server\HomeLibrary.Server.exe') --complete-recovery windows
    if ($LASTEXITCODE -ne 0) {throw 'Complete Backup Recovery could not finish. Keep the backup and retained data; review the message above.'}
} catch {
    Add-Type -AssemblyName System.Windows.Forms
    [Windows.Forms.MessageBox]::Show($_.Exception.Message,'Carribrary Complete Backup Recovery','OK','Error') | Out-Null
} finally {if($Acquired){$Maintenance.ReleaseMutex()};if($Maintenance){$Maintenance.Dispose()}}
