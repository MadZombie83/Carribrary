param([Parameter(Mandatory=$true)][string]$BackupPath)
$ErrorActionPreference = 'Stop'
$Running = Get-Service HomeLibraryNative -ErrorAction SilentlyContinue
if ($Running -and $Running.Status -eq 'Running') { throw 'Stop-Service HomeLibraryNative first. Start it again after importing.' }
$Source = (Resolve-Path $BackupPath).Path
$Server = Join-Path $PSScriptRoot 'server\HomeLibrary.Server.exe'
$Mode = '--import'
if ([IO.Path]::GetExtension($Source) -eq '.sqlite') { $Mode = '--import-db' }
& $Server $Mode $Source
if ($LASTEXITCODE -ne 0) { throw 'Import failed. The original source was not modified.' }
