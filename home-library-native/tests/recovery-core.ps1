$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot '../installer/Recovery.Core.ps1')
$Temp=Join-Path ([IO.Path]::GetTempPath()) ('carribrary-recovery-test-'+[Guid]::NewGuid().ToString('N'))
function Check($Value,$Message) { if (!$Value) { throw $Message }; Write-Output ('PASS '+$Message) }
function Reject([scriptblock]$Action,$Message) { try { & $Action; throw ('NOT_REJECTED '+$Message) } catch { if ($_.Exception.Message.StartsWith('NOT_REJECTED')) { throw }; Write-Output ('PASS '+$Message) } }
try {
    Check (!(Test-CarribraryCompatible '2.0.45-preview') -and !(Test-CarribraryCompatible '2.0.44-preview') -and !(Test-CarribraryCompatible '2.0.46-preview') -and (Test-CarribraryCompatible '2.0.47-preview') -and (Test-CarribraryCompatible '2.0.48-preview') -and !(Test-CarribraryCompatible '2.0.49-preview')) 'format-6 boundary rejects older and unverified future programs'
    $Root=Join-Path $Temp 'app'; $Data=Join-Path $Temp 'data'; $RecoveryHome=Join-Path $Temp 'recovery'
    New-Item -ItemType Directory -Path (Join-Path $Root 'server'),(Join-Path $Root 'tray'),$Data -Force | Out-Null
    Set-Content (Join-Path $Root 'server/HomeLibrary.Server.dll') 'old application'; Set-Content (Join-Path $Root 'server/appsettings.json') '{"Library":{"AdminPort":4173}}'; Set-Content (Join-Path $Root 'tray/app') 'old tray'
    Set-Content (Join-Path $Data 'library.sqlite') 'old catalog'; Set-Content (Join-Path $Data 'borrower-vaults.sqlite') 'old encrypted private list'; Set-Content (Join-Path $Data 'accounts.sqlite') 'old account'
    $Point=New-CarribraryRecoveryPoint $Root $Data $RecoveryHome '2.0.47'
    $Meta=Get-CarribraryRecoveryPoint $Point
    Check ($Meta.sourceVersion -eq '2.0.47') 'complete point retains source version and verifies both archives'
    Set-Content (Join-Path $Root 'server/HomeLibrary.Server.dll') 'new application'; Set-Content (Join-Path $Root 'tray/app') 'new tray'; Set-Content (Join-Path $Root 'server/appsettings.json') '{"Library":{"AdminPort":4199}}'
    Set-Content (Join-Path $Data 'library.sqlite') 'current loans and deletions'; Set-Content (Join-Path $Data 'borrower-vaults.sqlite') 'current private list'; Set-Content (Join-Path $Data 'accounts.sqlite') 'deleted account stays deleted'
    $Before=@{}; Get-ChildItem $Data -File | ForEach-Object { $Before[$_.Name]=Get-CarribraryHash $_.FullName }
    if (Test-CarribraryCompatible '2.0.32') { throw 'Pre-login program rollback must be rejected' }
    $Result=Restore-CarribraryProgram -Root $Root -Data $Data -RecoveryHome $RecoveryHome -Point $Point -CurrentVersion '2.0.47' -Stop {} -Start {} -Health {$true}
    Check ($Result -eq '2.0.47' -and (Get-Content (Join-Path $Root 'server/HomeLibrary.Server.dll') -Raw).Contains('old application')) 'compatible recovery switches application version'
    Check ((Get-Content (Join-Path $Root 'server/appsettings.json') -Raw).Contains('4199')) 'current connection settings survive program recovery'
    foreach ($Name in $Before.Keys) { Check ((Get-CarribraryHash (Join-Path $Data $Name)) -eq $Before[$Name]) ('current data unchanged: '+$Name) }
    Check (@(Get-ChildItem $RecoveryHome -Directory).Count -eq 2) 'current complete private state is preserved before recovery'
    Set-Content (Join-Path $Root 'server/HomeLibrary.Server.dll') 'new application after failed health'
    Reject { Restore-CarribraryProgram -Root $Root -Data $Data -RecoveryHome $RecoveryHome -Point $Point -CurrentVersion '2.0.47' -Stop {} -Start {} -Health {$false} } 'failed health restores the pre-attempt application'
    Check ((Get-Content (Join-Path $Root 'server/HomeLibrary.Server.dll') -Raw).Contains('new application after failed health')) 'failed rollback does not strand partially replaced binaries'
    Reject { Restore-CarribraryProgram -Root $Root -Data $Data -RecoveryHome $RecoveryHome -Point $Point -CurrentVersion '2.0.26' -Stop {throw 'stop must not be reached'} -Start {} -Health {$true} } 'privacy-incompatible downgrade rejected before stopping service'
    Reject { New-CarribraryRecoveryPoint $Root $Data (Join-Path $Data 'nested') '2.0.47' } 'overlapping recovery and data paths rejected'
    $SafetyPoint=Get-ChildItem $RecoveryHome -Directory | Where-Object {(Get-Content (Join-Path $_.FullName 'point.json') -Raw | ConvertFrom-Json).purpose -eq 'before-recovery'} | Select-Object -First 1
    Reject { Restore-CarribraryProgram -Root $Root -Data $Data -RecoveryHome $RecoveryHome -Point $SafetyPoint.FullName -CurrentVersion '2.0.47' -Stop {throw 'stop must not be reached'} -Start {} -Health {$true} } 'failed-state safety archives cannot masquerade as known previous applications'
    Remove-Item (Join-Path $Root 'server/HomeLibrary.Server.dll')
    $SavedSettings=Join-Path $Data 'installation-appsettings.json'; Set-Content $SavedSettings '{"Library":{"AdminPort":4199}}'
    Remove-Item (Join-Path $Root 'server/appsettings.json')
    Restore-CarribraryProgram -Root $Root -Data $Data -RecoveryHome $RecoveryHome -Point $Point -CurrentVersion '2.0.47' -SettingsPath $SavedSettings -Stop {} -Start {} -Health {$true} | Out-Null
    Check ((Test-Path (Join-Path $Root 'server/HomeLibrary.Server.dll')) -and (Get-Content (Join-Path $Root 'server/appsettings.json') -Raw).Contains('4199')) 'missing server DLL/config can recover from independent saved current settings'
    $ProgramBefore=Get-CarribraryHash (Join-Path $Root 'server/HomeLibrary.Server.dll')
    [IO.File]::AppendAllText((Join-Path $Point 'program.zip'),'corruption')
    Reject { Restore-CarribraryProgram -Root $Root -Data $Data -RecoveryHome $RecoveryHome -Point $Point -CurrentVersion '2.0.47' -Stop {throw 'stop must not be reached'} -Start {} -Health {$true} } 'corrupt archive rejected without stopping or changing the application'
    Check ((Get-CarribraryHash (Join-Path $Root 'server/HomeLibrary.Server.dll')) -eq $ProgramBefore) 'integrity rejection leaves current application intact'
    $Bad=Join-Path $Temp 'unsafe.zip';$Zip=[IO.Compression.ZipFile]::Open($Bad,[IO.Compression.ZipArchiveMode]::Create);[void]$Zip.CreateEntry('../escape');$Zip.Dispose()
    Reject { Assert-CarribraryArchive $Bad } 'archive traversal rejected'
    $Bad2=Join-Path $Temp 'duplicate.zip';$Zip=[IO.Compression.ZipFile]::Open($Bad2,[IO.Compression.ZipArchiveMode]::Create);[void]$Zip.CreateEntry('Server/file');[void]$Zip.CreateEntry('server/file');$Zip.Dispose()
    Reject { Assert-CarribraryArchive $Bad2 } 'case-insensitive duplicate archive paths rejected'
    if ($IsLinux) { New-Item -ItemType SymbolicLink -Path (Join-Path $Data 'link') -Target $Root | Out-Null; Reject { New-CarribraryRecoveryPoint $Root $Data $RecoveryHome '2.0.47' } 'filesystem links rejected before snapshot copy' }
    Write-Output 'ALL RECOVERY CORE TESTS PASSED'
} finally { if (Test-Path $Temp) { Remove-Item $Temp -Recurse -Force } }
