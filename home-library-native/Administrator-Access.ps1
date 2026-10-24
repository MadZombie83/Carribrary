# Read-only access to an existing private first-run/recovery code. No credential reset.
param([switch]$OnlyIfCodeExists)
$ErrorActionPreference='Stop'
$Identity=[Security.Principal.WindowsIdentity]::GetCurrent()
$Principal=New-Object Security.Principal.WindowsPrincipal($Identity)
if (!$Principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    $Arguments='-NoProfile -STA -ExecutionPolicy Bypass -File "'+$PSCommandPath+'"'
    if ($OnlyIfCodeExists) { $Arguments+=' -OnlyIfCodeExists' }
    Start-Process -FilePath 'powershell.exe' -Verb RunAs -ArgumentList $Arguments
    exit
}
Add-Type -AssemblyName System.Windows.Forms
try {
    $Config=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'server\appsettings.json') -Raw | ConvertFrom-Json
    $Data=Join-Path $env:ProgramData 'HomeLibraryNative\data'
    if ($Config.Library.DataDirectory) { $Data=[IO.Path]::GetFullPath($Config.Library.DataDirectory) }
    $CodePath=Join-Path $Data 'local-admin-setup.txt'
    if (!(Test-Path -LiteralPath $CodePath -PathType Leaf)) {
        if (!$OnlyIfCodeExists) { [System.Windows.Forms.MessageBox]::Show('No unused setup code is present. Open Carribrary and sign in, or select Recover local administrator access with your saved recovery code. See Help for stopped-server lost-code recovery. Your credentials have not been changed.','Carribrary administrator access','OK','Information') | Out-Null }
        exit 0
    }
    if ((Get-Item -LiteralPath $CodePath).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'The setup-code file is a link. Use supervised recovery.' }
    $Code=[IO.File]::ReadAllText($CodePath).Trim()
    if ($Code -notmatch '^[a-zA-Z0-9_-]{43}$') { throw 'The private setup code is invalid. See the recovery instructions.' }
    $Form=New-Object Windows.Forms.Form;$Form.Text='Carribrary private administrator setup code';$Form.Size=New-Object Drawing.Size(640,270);$Form.StartPosition='CenterScreen'
    $Label=New-Object Windows.Forms.Label;$Label.Text='Copy this one-time code into Carribrary local administrator setup or recovery. Windows administrator approval permits this read; it does not sign you into Carribrary. Keep this code private.';$Label.SetBounds(20,20,580,60);$Form.Controls.Add($Label)
    $Text=New-Object Windows.Forms.TextBox;$Text.ReadOnly=$true;$Text.Text=$Code;$Text.SetBounds(20,90,580,30);$Form.Controls.Add($Text)
    $Copy=New-Object Windows.Forms.Button;$Copy.Text='Copy setup code';$Copy.SetBounds(20,145,170,35);$Copy.Add_Click({[Windows.Forms.Clipboard]::SetText($Code);$Copy.Text='Copied'});$Form.Controls.Add($Copy)
    $Open=New-Object Windows.Forms.Button;$Open.Text='Open local sign-in';$Open.SetBounds(205,145,190,35);$Open.Add_Click({Start-Process ('http://localhost:'+$Config.Library.AdminPort+'/login')});$Form.Controls.Add($Open)
    $Close=New-Object Windows.Forms.Button;$Close.Text='Close';$Close.SetBounds(410,145,170,35);$Close.Add_Click({$Form.Close()});$Form.Controls.Add($Close)
    [void]$Form.ShowDialog()
} catch {
    [System.Windows.Forms.MessageBox]::Show($_.Exception.Message,'Carribrary administrator access','OK','Error') | Out-Null
    exit 1
}
