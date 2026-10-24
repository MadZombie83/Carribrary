param([Parameter(Mandatory=$true)][string]$Subdomain,[Security.SecureString]$ApiToken)
$ErrorActionPreference = 'Stop'
if ($Subdomain -notmatch '^[a-z0-9][a-z0-9-]{0,62}$') { throw 'Enter the DuckDNS subdomain only, without .duckdns.org.' }
$Token = $ApiToken
if (-not $Token) { $Token = Read-Host 'DuckDNS token (from your own account)' -AsSecureString }
$Plain = [Net.NetworkCredential]::new('', $Token).Password
try {
    $Data = Join-Path $env:ProgramData 'HomeLibraryNative\data'
    New-Item -ItemType Directory -Force -Path $Data | Out-Null
    . (Join-Path $PSScriptRoot 'Repair-DataPermissions.ps1')
    Repair-LibraryDataPermissions $Data
    Add-Type -AssemblyName System.Security
    $Bytes = [Text.Encoding]::UTF8.GetBytes($Plain)
    $Protected = [Security.Cryptography.ProtectedData]::Protect($Bytes,$null,[Security.Cryptography.DataProtectionScope]::LocalMachine)
    [IO.File]::WriteAllBytes((Join-Path $Data 'duckdns-token.protected'),$Protected)
    Set-Content (Join-Path $Data 'duckdns-subdomain.txt') $Subdomain
    [Array]::Clear($Bytes,0,$Bytes.Length)
} finally { $Plain = $null }
$Script = Join-Path $PSScriptRoot 'Update-DuckDNS.ps1'
$Action = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument ('-NoProfile -ExecutionPolicy RemoteSigned -File "' + $Script + '"')
$Trigger = New-ScheduledTaskTrigger -Once -At (Get-Date).AddMinutes(1) -RepetitionInterval (New-TimeSpan -Minutes 5)
$Principal = New-ScheduledTaskPrincipal -UserId 'NT AUTHORITY\LOCAL SERVICE' -LogonType ServiceAccount
Register-ScheduledTask -TaskName 'Home Library DDNS' -Action $Action -Trigger $Trigger -Principal $Principal -Force | Out-Null
& $Script
Write-Host "DDNS enabled: ${Subdomain}.duckdns.org. Use Configure-Remote.ps1 with this hostname."
