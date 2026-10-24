# Read-only service diagnostics. No database, keys, tokens or contacts are read.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
$Lines = New-Object System.Collections.Generic.List[string]
function Add-Line([string]$Value) { $Lines.Add($Value) }
function Inspect([string]$Title,[scriptblock]$Read) {
    Add-Line ''; Add-Line ('--- '+$Title+' ---')
    try { $Value = & $Read; Add-Line ($Value | Out-String -Width 180) } catch { Add-Line ('Could not read: '+$_.Exception.Message) }
}
Add-Line 'HOME LIBRARY STARTUP DIAGNOSTICS'
Add-Line ('Collected: '+(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz'))
Add-Line 'This tool does not start/stop services, import data, or change settings.'
$Service = Get-CimInstance Win32_Service -Filter "Name='HomeLibraryNative'" -ErrorAction SilentlyContinue
Inspect 'Windows version' { Get-CimInstance Win32_OperatingSystem | Select-Object Caption,Version,BuildNumber,OSArchitecture }
Inspect 'Service status and registration' {
    if ($Service) { $Service | Select-Object Name,State,StartMode,StartName,PathName,ExitCode,ServiceSpecificExitCode }
    else { 'HomeLibraryNative is not registered.' }
}
$Server = $null
if ($Service) {
    $Match = [regex]::Match($Service.PathName, '^"([^\"]+HomeLibrary\.Server\.exe)"$|^([^\"]+HomeLibrary\.Server\.exe)$', 'IgnoreCase')
    if ($Match.Success) { $Server=$Match.Groups[1].Value; if (-not $Server) { $Server=$Match.Groups[2].Value } }
}
if (-not $Server) {
    $Registered = Get-ItemProperty 'HKLM:\SOFTWARE\HomeLibraryNative' -ErrorAction SilentlyContinue
    if ($Registered.InstallDir) { $Server = Join-Path $Registered.InstallDir 'server\HomeLibrary.Server.exe' }
    else { $Server = Join-Path $env:ProgramFiles 'Carribrary\server\HomeLibrary.Server.exe' }
}
$Root = Split-Path $Server
$Data = Join-Path $env:ProgramData 'HomeLibraryNative\data'
$Ports = @(4173,4174,7443)
Inspect 'Program files' {
    foreach ($Name in @('HomeLibrary.Server.exe','HomeLibrary.Server.dll','HomeLibrary.Server.runtimeconfig.json','coreclr.dll','hostfxr.dll','e_sqlite3.dll','appsettings.json')) {
        $Path=Join-Path $Root $Name
        if (Test-Path -LiteralPath $Path) { Get-Item -LiteralPath $Path | Select-Object Name,Length }
        else { 'MISSING: '+$Name }
    }
}
Inspect 'Runtime version' {
    $Runtime = Get-Content (Join-Path $Root 'HomeLibrary.Server.runtimeconfig.json') -Raw | ConvertFrom-Json
    $Runtime.runtimeOptions | Select-Object tfm,includedFrameworks,frameworks
}
Inspect 'Configured ports (credentials excluded)' {
    $Config=Get-Content (Join-Path $Root 'appsettings.json') -Raw | ConvertFrom-Json
    if ($Config.Library.DataDirectory) { $script:Data=[IO.Path]::GetFullPath($Config.Library.DataDirectory) }
    $script:Ports=@($Config.Library.AdminPort,$Config.Library.CatalogPort,$Config.Library.PhonePort)
    $Config.Library | Select-Object AdminPort,CatalogPort,PhonePort,DiscoveryPort,EnableDiscovery
}
Inspect 'Program and data folder permissions' {
    foreach ($Path in @($Root,$Data,(Join-Path $Data 'keyring'))) {
        'Folder: '+$Path
        if (Test-Path -LiteralPath $Path) { (Get-Acl -LiteralPath $Path).Access | Select-Object IdentityReference,FileSystemRights,AccessControlType,IsInherited }
        else { 'Folder not present.' }
    }
}
Inspect 'Port listeners' {
    foreach ($Port in $Ports) {
        $Listeners=Get-NetTCPConnection -State Listen -LocalPort $Port -ErrorAction SilentlyContinue
        if ($Listeners) {
            foreach($Listener in $Listeners) {
                $Process=Get-Process -Id $Listener.OwningProcess -ErrorAction SilentlyContinue
                [PSCustomObject]@{Port=$Port;Address=$Listener.LocalAddress;Process=$Process.ProcessName;ProcessId=$Listener.OwningProcess}
            }
        } else { 'No listener on TCP '+$Port }
    }
}
Inspect 'HTTPS identity setup log' {
    $IdentityLog=Join-Path $Data 'installer-identity.log'
    if (Test-Path -LiteralPath $IdentityLog) { Get-Content -LiteralPath $IdentityLog -Tail 60 }
    else { 'No HTTPS identity setup log present.' }
}
foreach ($Log in @('Application','System')) {
    Inspect ($Log+' events for Carribrary') {
        $Events=Get-WinEvent -FilterHashtable @{LogName=$Log;StartTime=(Get-Date).AddDays(-3);Level=1,2,3} -MaxEvents 1200 -ErrorAction SilentlyContinue |
            Where-Object { $_.Message -match '(?i)HomeLibrary(?:Native|\.Server|\.)|Home Library|Carribrary' } | Select-Object -First 12
        if (-not $Events) { 'No matching error/warning events found.' }
        foreach($Event in $Events) {
            "Time: $($Event.TimeCreated.ToString('yyyy-MM-dd HH:mm:ss')) | Provider: $($Event.ProviderName) | ID: $($Event.Id)"
            $Event.Message
            ''
        }
    }
}
$Folder=[Environment]::GetFolderPath('MyDocuments')
$Report=Join-Path $Folder ('Carribrary-Diagnostics-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.txt')
try {
    $Lines | Set-Content -LiteralPath $Report -Encoding UTF8
    Start-Process -FilePath 'notepad.exe' -ArgumentList ('"'+$Report+'"')
    [System.Windows.Forms.MessageBox]::Show("The report opened in Notepad and was saved to:`r`n$Report`r`n`r`nSend the report text or a screenshot in the Book Lending chat. It contains technical service errors, not your library database or credentials.",'Carribrary diagnostics','OK','Information') | Out-Null
} catch {
    [System.Windows.Forms.MessageBox]::Show($_.Exception.Message,'Carribrary diagnostics','OK','Error') | Out-Null
    exit 1
}
