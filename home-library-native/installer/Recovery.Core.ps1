# Shared recovery logic; Windows service/UI operations are supplied by the caller.
# Recovery NEVER restores a historical database, account store, vault or keyring.
Add-Type -AssemblyName System.IO.Compression.FileSystem
function Get-CarribraryPath([string]$Path) { return [IO.Path]::GetFullPath($Path).TrimEnd([IO.Path]::DirectorySeparatorChar,[IO.Path]::AltDirectorySeparatorChar) }
function Test-CarribraryWithin([string]$Child,[string]$Parent) {
    $Child=Get-CarribraryPath $Child; $Parent=Get-CarribraryPath $Parent
    return $Child.Equals($Parent,[StringComparison]::OrdinalIgnoreCase) -or $Child.StartsWith($Parent+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)
}
function Assert-CarribraryTree([string]$Path) {
    $Full=Get-CarribraryPath $Path
    if ($Full -eq [IO.Path]::GetPathRoot($Full).TrimEnd([IO.Path]::DirectorySeparatorChar)) { throw 'A drive root cannot be used for recovery.' }
    $Ancestor=$Full
    while ($Ancestor) {
        if ((Test-Path -LiteralPath $Ancestor) -and ((Get-Item -LiteralPath $Ancestor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Recovery cannot follow filesystem links.' }
        $Ancestor=Split-Path $Ancestor -Parent
    }
    if (Test-Path -LiteralPath $Full) {
        foreach ($Node in @(Get-ChildItem -LiteralPath $Full -Recurse -Force)) {
            if ($Node.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Recovery cannot follow filesystem links.' }
        }
    }
}
function Assert-CarribraryRecoveryPaths([string]$Root,[string]$Data,[string]$RecoveryHome) {
    foreach ($Pair in @(@($Root,$Data),@($Root,$RecoveryHome),@($Data,$RecoveryHome))) {
        if ((Test-CarribraryWithin $Pair[0] $Pair[1]) -or (Test-CarribraryWithin $Pair[1] $Pair[0])) { throw 'Program, data and recovery folders must be separate.' }
    }
    foreach ($Path in @($Root,$Data,$RecoveryHome)) { Assert-CarribraryTree $Path }
}
function Get-CarribraryHash([string]$Path) {
    $Stream=[IO.File]::OpenRead($Path); $Sha=[Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($Sha.ComputeHash($Stream))).Replace('-','').ToLowerInvariant() }
    finally { $Stream.Dispose(); $Sha.Dispose() }
}
function Test-CarribraryCompatible([string]$Version) {
    # Format-5 boundary: older programs discard artwork saved within custom themes.
    return $Version -match '^2\.0\.(45|46)(?:[.+-].*)?$'
}
function Get-CarribraryProgramVersion([string]$Root) {
    $Dll=Join-Path $Root 'server/HomeLibrary.Server.dll'
    if (Test-Path -LiteralPath $Dll) {
        $Version=[Diagnostics.FileVersionInfo]::GetVersionInfo($Dll).ProductVersion
        if ($Version) { return $Version }
    }
    throw 'The installed application version could not be verified. Program recovery was stopped; private snapshots were retained.'
}
function Assert-CarribraryArchive([string]$Path) {
    $Archive=[IO.Compression.ZipFile]::OpenRead($Path)
    $Seen=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    try {
        foreach ($Entry in $Archive.Entries) {
            $Name=$Entry.FullName.Replace('\','/')
            if (!$Name -or $Name.StartsWith('/') -or $Name.Contains(':') -or $Name.Contains([char]0) -or @($Name.TrimEnd('/').Split('/') | Where-Object { $_ -in @('','..','.') -or $_.TrimEnd(' ','.') -ne $_ }).Count -gt 0 -or !$Seen.Add($Name)) { throw 'Recovery archive has an unsafe or repeated filename.' }
            if ((($Entry.ExternalAttributes -shr 16) -band 61440) -eq 40960) { throw 'Recovery archives cannot contain symbolic links.' }
        }
    } finally { $Archive.Dispose() }
}
function Get-CarribraryRecoveryPoint([string]$Point) {
    Assert-CarribraryTree $Point
    $Meta=Get-Content -LiteralPath (Join-Path $Point 'point.json') -Raw | ConvertFrom-Json
    if ($Meta.format -ne 1 -or $Meta.product -ne 'Carribrary' -or !$Meta.sourceVersion -or !$Meta.root -or !$Meta.data) { throw 'This is not a supported Carribrary recovery point.' }
    foreach ($Name in @('program','data')) {
        $Archive=Join-Path $Point ($Name+'.zip'); $Expected=$Meta.($Name+'Hash')
        if ($Expected -notmatch '^[a-f0-9]{64}$' -or (Get-CarribraryHash $Archive) -ne $Expected) { throw 'A recovery point failed its integrity check. Nothing was restored.' }
        Assert-CarribraryArchive $Archive
    }
    return $Meta
}
function New-CarribraryRecoveryPoint([string]$Root,[string]$Data,[string]$RecoveryHome,[string]$SourceVersion,[scriptblock]$Protect={param($Path)},[string]$Purpose='update') {
    Assert-CarribraryRecoveryPaths $Root $Data $RecoveryHome
    if (!(Test-Path -LiteralPath (Join-Path $Data 'library.sqlite'))) { throw 'A stopped existing installation is required for a recovery point.' }
    New-Item -ItemType Directory -Path $RecoveryHome -Force | Out-Null; & $Protect $RecoveryHome
    $Id=(Get-Date).ToUniversalTime().ToString('yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N')
    $Stage=Join-Path $RecoveryHome ('.creating-'+$Id); $Point=Join-Path $RecoveryHome $Id
    New-Item -ItemType Directory -Path $Stage | Out-Null
    try {
        $Program=Join-Path $Stage 'program'; New-Item -ItemType Directory -Path $Program | Out-Null
        foreach ($Name in @('server','tray')) {
            if (Test-Path -LiteralPath (Join-Path $Root $Name)) { Copy-Item -LiteralPath (Join-Path $Root $Name) -Destination (Join-Path $Program $Name) -Recurse -Force }
        }
        [IO.Compression.ZipFile]::CreateFromDirectory($Program,(Join-Path $Stage 'program.zip'),[IO.Compression.CompressionLevel]::Optimal,$false)
        [IO.Compression.ZipFile]::CreateFromDirectory($Data,(Join-Path $Stage 'data.zip'),[IO.Compression.CompressionLevel]::Optimal,$false)
        $Meta=[ordered]@{format=1;product='Carribrary';sourceVersion=$SourceVersion;purpose=$Purpose;createdUtc=[DateTimeOffset]::UtcNow.ToString('o');root=(Get-CarribraryPath $Root);data=(Get-CarribraryPath $Data);programHash=(Get-CarribraryHash (Join-Path $Stage 'program.zip'));dataHash=(Get-CarribraryHash (Join-Path $Stage 'data.zip'))}
        $Meta | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $Stage 'point.json') -Encoding UTF8
        Remove-Item -LiteralPath $Program -Recurse -Force
        Get-CarribraryRecoveryPoint $Stage | Out-Null
        Move-Item -LiteralPath $Stage -Destination $Point
        return $Point
    } catch { if (Test-Path -LiteralPath $Stage) { Remove-Item -LiteralPath $Stage -Recurse -Force }; throw }
}
function Restore-CarribraryProgram([string]$Root,[string]$Data,[string]$RecoveryHome,[string]$Point,[string]$CurrentVersion,[scriptblock]$Stop,[scriptblock]$Start,[scriptblock]$Health,[scriptblock]$Protect={param($Path)},[scriptblock]$GrantRead={param($Path)},[string]$SettingsPath='') {
    Assert-CarribraryRecoveryPaths $Root $Data $RecoveryHome
    if (!(Test-CarribraryWithin $Point $RecoveryHome) -or (Get-CarribraryPath $Point) -eq (Get-CarribraryPath $RecoveryHome)) { throw 'Choose a recovery point inside this installation protected recovery folder.' }
    $Meta=Get-CarribraryRecoveryPoint $Point
    if ($Meta.purpose -ne 'update') { throw 'This is a safety copy of a recovery attempt, not a verified previous-application candidate.' }
    if (Test-Path -LiteralPath (Join-Path $Data '.recovery-review-required')) { throw 'Finish the authenticated restored-library access review before program rollback. Keep remote access paused.' }
    if (!(Test-CarribraryCompatible $Meta.sourceVersion) -or !(Test-CarribraryCompatible $CurrentVersion)) { throw 'These releases do not have verified data compatibility. Automatic program recovery cannot cross this boundary. The complete private snapshot is retained for supervised recovery.' }
    if ($Meta.root -ne (Get-CarribraryPath $Root) -or $Meta.data -ne (Get-CarribraryPath $Data)) { throw 'This recovery point belongs to another installation or data folder.' }
    & $Stop
    $Stage=Join-Path $Root ('.recovery-stage-'+[Guid]::NewGuid().ToString('N'))
    $Old=Join-Path $Root ('.recovery-old-'+[Guid]::NewGuid().ToString('N'))
    $Moved=New-Object 'Collections.Generic.List[string]'; $Succeeded=$false
    try {
        # Even a damaged current program is retained with the latest complete data.
        New-CarribraryRecoveryPoint $Root $Data $RecoveryHome $CurrentVersion $Protect 'before-recovery' | Out-Null
        [IO.Compression.ZipFile]::ExtractToDirectory((Join-Path $Point 'program.zip'),$Stage)
        foreach ($Entry in @(Get-ChildItem -LiteralPath $Stage -Force)) { if ($Entry.Name -notin @('server','tray') -or !$Entry.PSIsContainer) { throw 'Unexpected program content in recovery point.' } }
        if (!(Test-Path -LiteralPath (Join-Path $Stage 'server/HomeLibrary.Server.dll'))) { throw 'The previous server program is missing.' }
        # Keep CURRENT connection settings and data path. Never restore stale identities/accounts.
        if (Test-Path -LiteralPath (Join-Path $Root 'server')) { Get-ChildItem -LiteralPath (Join-Path $Root 'server') -Filter 'appsettings*.json' -File | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $Stage ('server/'+$_.Name)) -Force } }
        if ($SettingsPath) { Copy-Item -LiteralPath $SettingsPath -Destination (Join-Path $Stage 'server/appsettings.json') -Force }
        & $GrantRead $Stage
        New-Item -ItemType Directory -Path $Old | Out-Null
        foreach ($Name in @('server','tray')) {
            if (Test-Path -LiteralPath (Join-Path $Stage $Name)) {
                if (Test-Path -LiteralPath (Join-Path $Root $Name)) { Move-Item -LiteralPath (Join-Path $Root $Name) -Destination (Join-Path $Old $Name) }
                $Moved.Add($Name)
                Move-Item -LiteralPath (Join-Path $Stage $Name) -Destination (Join-Path $Root $Name)
            }
        }
        & $Start
        if (!(& $Health)) { throw 'The previous application did not pass its startup and catalog checks.' }
        $Succeeded=$true
        return $Meta.sourceVersion
    } catch {
        $Failure=$_
        if ($Moved.Count -gt 0) {
            & $Stop
            foreach ($Name in $Moved) {
                if (Test-Path -LiteralPath (Join-Path $Root $Name)) { Remove-Item -LiteralPath (Join-Path $Root $Name) -Recurse -Force }
                if (Test-Path -LiteralPath (Join-Path $Old $Name)) { Move-Item -LiteralPath (Join-Path $Old $Name) -Destination (Join-Path $Root $Name) }
            }
        }
        & $Start
        throw $Failure
    } finally {
        if (Test-Path -LiteralPath $Stage) { Remove-Item -LiteralPath $Stage -Recurse -Force }
        # Keep old files after an exceptional failure; never delete the last copy by guesswork.
        if ($Succeeded -and (Test-Path -LiteralPath $Old)) { Remove-Item -LiteralPath $Old -Recurse -Force }
    }
}
