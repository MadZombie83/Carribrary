# GUI setup; launched from the Start menu. No terminal commands are needed.
$ErrorActionPreference = 'Stop'
$Identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$Principal = New-Object Security.Principal.WindowsPrincipal($Identity)
if (-not $Principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    try { Start-Process -FilePath (Join-Path $PSHOME 'powershell.exe') -ArgumentList ('-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File "'+$PSCommandPath+'"') -Verb RunAs } catch {}
    exit
}
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
. (Join-Path $PSScriptRoot 'Repair-DataPermissions.ps1')
[System.Windows.Forms.Application]::EnableVisualStyles()
$Form = New-Object System.Windows.Forms.Form
$Form.Text = 'Carribrary - Connection setup'
$Form.ClientSize = New-Object System.Drawing.Size(650,620)
$Form.StartPosition = 'CenterScreen'
$Form.FormBorderStyle = 'FixedDialog'
$Form.MaximizeBox = $false
function Label([string]$Text,[int]$Y,[int]$Height=30) {
    $Control = New-Object System.Windows.Forms.Label
    $Control.Text=$Text; $Control.Location=New-Object System.Drawing.Point(20,$Y); $Control.Size=New-Object System.Drawing.Size(610,$Height)
    $Form.Controls.Add($Control)
}
Label '1. Set up work access with a DNS hostname and a router port forward. Your private phone pairs from the Windows library page.' 15 45
Label 'Server hostname (for example: yourname.duckdns.org)' 65
$Hostname=New-Object System.Windows.Forms.TextBox; $Hostname.Location=New-Object System.Drawing.Point(20,95); $Hostname.Size=New-Object System.Drawing.Size(610,25);$Form.Controls.Add($Hostname)
Label 'Optional DuckDNS token: enter it only to enable automatic IP updates.' 130
$Token=New-Object System.Windows.Forms.TextBox; $Token.UseSystemPasswordChar=$true;$Token.Location=New-Object System.Drawing.Point(20,160);$Token.Size=New-Object System.Drawing.Size(610,25);$Form.Controls.Add($Token)
Label 'Create a free hostname at DuckDNS.org, then return here. Copy your hostname and token above. Leave the token blank if your router updates DNS. The token is saved encrypted on this computer.' 195 45
$DuckDns=New-Object System.Windows.Forms.LinkLabel;$DuckDns.Text='Open DuckDNS.org';$DuckDns.Location=New-Object System.Drawing.Point(440,288);$DuckDns.Size=New-Object System.Drawing.Size(190,20);$Form.Controls.Add($DuckDns)
$DuckDns.Add_LinkClicked({Start-Process 'https://www.duckdns.org/'})
$PortInfo=New-Object System.Windows.Forms.Label; $PortInfo.Location=New-Object System.Drawing.Point(20,245);$PortInfo.Size=New-Object System.Drawing.Size(610,50);$Form.Controls.Add($PortInfo)
$ConfigPath=Join-Path $PSScriptRoot 'server\appsettings.json'
$Config=Get-Content $ConfigPath -Raw | ConvertFrom-Json
if ($Config.Library.RemoteAddress) { $Hostname.Text=([Uri]$Config.Library.RemoteAddress).Host }
$PortInfo.Text="On your router, forward TCP $($Config.Library.PhonePort) to this Windows computer at the same port. Do not forward admin ports $($Config.Library.AdminPort) or $($Config.Library.CatalogPort). Router setup cannot be performed by this installer."
$Save=New-Object System.Windows.Forms.Button;$Save.Text='Save remote connection';$Save.Location=New-Object System.Drawing.Point(20,310);$Save.Size=New-Object System.Drawing.Size(250,35);$Form.Controls.Add($Save)
$Open=New-Object System.Windows.Forms.Button;$Open.Text='Open library / pair phone';$Open.Location=New-Object System.Drawing.Point(280,310);$Open.Size=New-Object System.Drawing.Size(250,35);$Form.Controls.Add($Open)
$Public=New-Object System.Windows.Forms.Button;$Public.Text='Set up public book-list sharing...';$Public.Location=New-Object System.Drawing.Point(20,365);$Public.Size=New-Object System.Drawing.Size(300,35);$Form.Controls.Add($Public)
Label 'Public sharing is optional. It downloads Caddy and needs router ports 80/443. Only book-list links are public; phone pairing remains private.' 410 45
$Status=New-Object System.Windows.Forms.Label;$Status.Location=New-Object System.Drawing.Point(20,565);$Status.Size=New-Object System.Drawing.Size(610,35);$Form.Controls.Add($Status)
$Open.Add_Click({Start-Process "http://localhost:$($Config.Library.AdminPort)/"})
$Save.Add_Click({
    $Save.Enabled=$false;$Public.Enabled=$false;$Form.UseWaitCursor=$true;$Status.Text='Saving settings...';[System.Windows.Forms.Application]::DoEvents()
    try {
        $Name=$Hostname.Text.Trim().ToLowerInvariant()
        if ($Name -notmatch '^(?=.{1,253}$)[a-z0-9](?:[a-z0-9.-]*[a-z0-9])?\.[a-z]{2,63}$') { throw 'Enter a DNS hostname only, without https:// or a port.' }
        if ($Token.Text) {
            if ($Name -notmatch '^([a-z0-9][a-z0-9-]{0,62})\.duckdns\.org$') { throw 'A DuckDNS token can only be used with yourname.duckdns.org.' }
            $Subdomain=$Name.Substring(0,$Name.Length - '.duckdns.org'.Length)
            $Secure=ConvertTo-SecureString $Token.Text -AsPlainText -Force
            & (Join-Path $PSScriptRoot 'Configure-DuckDNS.ps1') -Subdomain $Subdomain -ApiToken $Secure
            $Token.Clear();$Secure.Dispose()
        }
        & (Join-Path $PSScriptRoot 'Configure-Remote.ps1') -Hostname $Name
        Restart-Service HomeLibraryNative
        Save-LibraryInstallationSettings $ConfigPath (Join-Path $env:ProgramData 'HomeLibraryNative\data')
        $Status.Text='Saved. Forward the router port, then open the library to create a pairing QR.'
    } catch { [System.Windows.Forms.MessageBox]::Show($_.Exception.Message,'Connection setup','OK','Error') | Out-Null; $Status.Text='Settings were not fully applied. Review the message and try again.' }
    finally { $Form.UseWaitCursor=$false;$Save.Enabled=$true;$Public.Enabled=$true }
})
$Public.Add_Click({
    $Answer=[System.Windows.Forms.MessageBox]::Show('Set up browser-accessible book sharing? This downloads Caddy from its official site and opens Windows firewall ports 80/443. You must also forward those ports on your router. No link becomes public until you enable it in the library.','Public book-list sharing','YesNo','Question')
    if ($Answer -ne 'Yes') { return }
    $Save.Enabled=$false;$Public.Enabled=$false;$Form.UseWaitCursor=$true;$Status.Text='Setting up public HTTPS...';[System.Windows.Forms.Application]::DoEvents()
    try {
        & (Join-Path $PSScriptRoot 'Setup-PublicCatalog.ps1') -Hostname $Hostname.Text.Trim()
        Save-LibraryInstallationSettings $ConfigPath (Join-Path $env:ProgramData 'HomeLibraryNative\data')
        $Status.Text='Proxy configured. Forward ports 80/443, then use Share by QR in the library.'
    } catch { [System.Windows.Forms.MessageBox]::Show($_.Exception.Message,'Public catalog setup','OK','Error') | Out-Null; $Status.Text='Public setup was not completed.' }
    finally { $Form.UseWaitCursor=$false;$Save.Enabled=$true;$Public.Enabled=$true }
})
$Check=New-Object System.Windows.Forms.Button;$Check.Text='Check DNS and local service';$Check.Location=New-Object System.Drawing.Point(20,465);$Check.Size=New-Object System.Drawing.Size(290,35);$Form.Controls.Add($Check)
$Router=New-Object System.Windows.Forms.Button;$Router.Text='Open router settings';$Router.Location=New-Object System.Drawing.Point(325,465);$Router.Size=New-Object System.Drawing.Size(290,35);$Form.Controls.Add($Router)
Label '2. Reserve this PC address in the router. 3. Forward the ports shown above. 4. Test on your phone with Wi-Fi OFF. A local check cannot verify outside access.' 510 45
$Check.Add_Click({
    $Check.Enabled=$false;$Form.UseWaitCursor=$true
    try {
        $Name=$Hostname.Text.Trim()
        if ($Name -notmatch '^(?=.{1,253}$)[a-zA-Z0-9](?:[a-zA-Z0-9.-]*[a-zA-Z0-9])?\.[a-zA-Z]{2,63}$') { throw 'Enter the DNS hostname first.' }
        $Addresses=[Net.Dns]::GetHostAddresses($Name)
        $Response=Invoke-WebRequest -UseBasicParsing -Uri "http://localhost:$($Config.Library.AdminPort)/local-auth/health" -TimeoutSec 3
        [System.Windows.Forms.MessageBox]::Show(('DNS: '+(($Addresses | ForEach-Object {$_.IPAddressToString}) -join ', ')+"`r`nLibrary service is responding.`r`nNow test the phone with Wi-Fi OFF to verify router access."),'Connection check','OK','Information') | Out-Null
    } catch { [System.Windows.Forms.MessageBox]::Show($_.Exception.Message,'Connection check','OK','Error') | Out-Null }
    finally {$Check.Enabled=$true;$Form.UseWaitCursor=$false}
})
$Router.Add_Click({
    try {
        $Gateway=Get-NetIPConfiguration | Where-Object {$_.IPv4DefaultGateway} | Select-Object -First 1 -ExpandProperty IPv4DefaultGateway | Select-Object -ExpandProperty NextHop
        $Parsed=$null
        if (-not [Net.IPAddress]::TryParse([string]$Gateway,[ref]$Parsed)) {throw 'No router address found. Open your router administration page manually.'}
        Start-Process ('http://'+$Parsed.IPAddressToString)
    } catch { [System.Windows.Forms.MessageBox]::Show($_.Exception.Message,'Router settings','OK','Error') | Out-Null }
})
[void]$Form.ShowDialog()
$Token.Clear();$Form.Dispose()
