$ErrorActionPreference = 'Stop'
$Tray = Join-Path $PSScriptRoot 'tray\HomeLibrary.Tray.exe'
if (-not (Test-Path $Tray)) { throw 'Tray app not found.' }
$Startup = [Environment]::GetFolderPath('Startup')
$Shell = New-Object -ComObject WScript.Shell
Remove-Item (Join-Path $Startup 'Home Library.lnk') -Force -ErrorAction SilentlyContinue
$Shortcut = $Shell.CreateShortcut((Join-Path $Startup 'Carribrary.lnk'))
$Shortcut.TargetPath = $Tray
$Shortcut.WorkingDirectory = Split-Path $Tray
$Shortcut.Save()
Write-Host 'Tray icon starts when you sign in. The Windows service starts at boot independently.'
