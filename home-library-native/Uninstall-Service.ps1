$ErrorActionPreference = 'Stop'
$Service = Get-Service HomeLibraryNative -ErrorAction SilentlyContinue
if ($Service) { Stop-Service HomeLibraryNative; & sc.exe delete HomeLibraryNative | Out-Null; if ($LASTEXITCODE -ne 0) { throw 'Could not remove service.' } }
Get-NetFirewallRule -DisplayName 'Home Library paired phones','Home Library discovery','Carribrary paired phones','Carribrary discovery' -ErrorAction SilentlyContinue | Remove-NetFirewallRule
Write-Host 'Service removed. Your database, backups and phone identities are retained in ProgramData\HomeLibraryNative\data.'
