# GUI-only Windows approval for machine certificates and the optional public proxy.
# Library content and existing administrator credentials are never reset here.
$ErrorActionPreference='Stop'
$TaskName='Home Library Public Catalog'
$Root=$PSScriptRoot
$Identity=[Security.Principal.WindowsIdentity]::GetCurrent()
if (!(New-Object Security.Principal.WindowsPrincipal($Identity)).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    try { Start-Process -FilePath (Join-Path $PSHOME 'powershell.exe') -ArgumentList ('-NoProfile -STA -ExecutionPolicy RemoteSigned -File "'+$PSCommandPath+'"') -Verb RunAs } catch {}
    exit
}
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
. (Join-Path $Root 'Repair-DataPermissions.ps1')
[Windows.Forms.Application]::EnableVisualStyles()
$ConfigPath=Join-Path $Root 'server\appsettings.json'
$Config=Get-Content -LiteralPath $ConfigPath -Raw | ConvertFrom-Json
$Data=Join-Path $env:ProgramData 'HomeLibraryNative\data'
if ($Config.Library.DataDirectory) { $Data=[IO.Path]::GetFullPath($Config.Library.DataDirectory) }
$Registry=Join-Path $Data 'network-hosting.json'
$PublicFile=Join-Path $Data 'public-connection.json'
$State=if(Test-Path -LiteralPath $Registry){Get-Content -LiteralPath $Registry -Raw | ConvertFrom-Json}else{$null}
$RegistryReview=if(Test-Path -LiteralPath $Registry){[IO.File]::ReadAllText($Registry)}else{''}
$PublicReview=if(Test-Path -LiteralPath $PublicFile){[IO.File]::ReadAllText($PublicFile)}else{''}
$Public=if(Test-Path -LiteralPath $PublicFile){Get-Content -LiteralPath $PublicFile -Raw | ConvertFrom-Json}else{$null}
$Libraries=Join-Path (Split-Path $Data -Parent) ((Split-Path $Data -Leaf)+'-libraries')
$Server=Join-Path $Root 'server\HomeLibrary.Server.exe'
$UndoFile=Join-Path $Data 'network-windows-undo.json'
$Form=New-Object Windows.Forms.Form
$Form.Text='Carribrary - Libraries and network assistant'
$Form.ClientSize=New-Object Drawing.Size(700,590)
$Form.StartPosition='CenterScreen'
$Form.MinimumSize=New-Object Drawing.Size(650,560)
$Heading=New-Object Windows.Forms.Label
$Heading.Text='Review the changes, then finish Windows setup'
$Heading.SetBounds(24,20,650,32);$Heading.Font=New-Object Drawing.Font('Segoe UI',14,[Drawing.FontStyle]::Bold);$Form.Controls.Add($Heading)
$Summary=New-Object Windows.Forms.TextBox
$Summary.Multiline=$true;$Summary.ReadOnly=$true;$Summary.ScrollBars='Vertical';$Summary.SetBounds(24,65,650,300);$Summary.Anchor='Top,Left,Right';$Summary.Font=New-Object Drawing.Font('Segoe UI',10);$Form.Controls.Add($Summary)
$Lines=New-Object Collections.Generic.List[string]
$Lines.Add('This assistant prepares private HTTPS keys for separate libraries and adds only Carribrary firewall rules. Books, loans, accounts and backups are kept.')
foreach($Library in @($State.libraries)){if($Library){$Lines.Add('Library: '+$Library.name+' - phone port '+$Library.phonePort)}}
if($State.join.enabled){$Lines.Add('Connect through the first computer: '+$State.join.hostname+'; allow private home-network port '+$State.join.port+'. The old Carribrary public proxy will be paused.')}
if($Public.enabled){$Lines.Add('Managed public HTTPS: '+$Public.hostname+'. Allow TCP 80/443 for Carribrary''s verified proxy. An existing Carribrary scheduled proxy will be paused, not deleted.')}
$Lines.Add('The main Carribrary service briefly stops and restarts. Existing data stays in place. You can undo the public-proxy migration with the button below.')
$Summary.Lines=$Lines.ToArray()
$Agree=New-Object Windows.Forms.CheckBox;$Agree.Text='I reviewed these changes and want to apply this setup.';$Agree.SetBounds(24,385,650,30);$Form.Controls.Add($Agree)
$Apply=New-Object Windows.Forms.Button;$Apply.Text='Apply reviewed setup';$Apply.SetBounds(24,430,220,44);$Apply.Enabled=$false;$Form.Controls.Add($Apply)
$Open=New-Object Windows.Forms.Button;$Open.Text='Open library';$Open.SetBounds(260,430,180,44);$Form.Controls.Add($Open)
$Undo=New-Object Windows.Forms.Button;$Undo.Text='Undo public-proxy migration';$Undo.SetBounds(24,490,260,38);$Undo.Enabled=Test-Path -LiteralPath $UndoFile;$Form.Controls.Add($Undo)
$Status=New-Object Windows.Forms.Label;$Status.SetBounds(24,540,650,38);$Status.Anchor='Left,Right,Bottom';$Status.Text='Close Carribrary pages if you are editing a record. No changes occur until you choose Apply.';$Form.Controls.Add($Status)
$Agree.Add_CheckedChanged({$Apply.Enabled=$Agree.Checked})
$Open.Add_Click({Start-Process ('http://localhost:'+ [int]$Config.Library.AdminPort +'/#settings/system-advanced/network-hosting')})
function Stage([string]$Text){$Status.Text=$Text;[Windows.Forms.Application]::DoEvents()}
function PrepareIdentity([string]$Folder,$Library,[switch]$Gateway){
    $Info=New-Object Diagnostics.ProcessStartInfo
    $Info.FileName=$Server;$Info.UseShellExecute=$false;$Info.CreateNoWindow=$true
    $Info.RedirectStandardOutput=$true;$Info.RedirectStandardError=$true
    $Info.EnvironmentVariables['HOME_LIBRARY_DATA']=$Folder
    $Info.EnvironmentVariables['Library__ManagedInstance']='false'
    $Info.EnvironmentVariables['Library__GatewayPort']='0'
    if($Library){
        $Info.EnvironmentVariables['Library__ManagedInstance']='true'
        $Info.EnvironmentVariables['Library__InstanceId']=[string]$Library.id
        $Info.EnvironmentVariables['Library__InitialLibraryName']=[string]$Library.name
        foreach($Name in @('AdminPort','CatalogPort','PhonePort','DiscoveryPort')){$Info.EnvironmentVariables['Library__'+$Name]=[string]$Library.$Name}
        $Info.EnvironmentVariables['Library__PublicCatalogOrigin']=''
        $Info.EnvironmentVariables['Library__RemoteAddress']=''
        $Info.EnvironmentVariables['Library__EnableDiscovery']='false'
    }
    $Info.Arguments=if($Gateway){'--prepare-windows-identity --prepare-network-gateway'}else{'--prepare-windows-identity'}
    $Process=New-Object Diagnostics.Process;$Process.StartInfo=$Info
    if(!$Process.Start()){throw 'The secure connection helper could not start.'}
    $Output=$Process.StandardOutput.ReadToEndAsync();$Errors=$Process.StandardError.ReadToEndAsync()
    $Deadline=[DateTime]::UtcNow.AddSeconds(90)
    while(!$Process.WaitForExit(200)){[Windows.Forms.Application]::DoEvents();if([DateTime]::UtcNow -gt $Deadline){$Process.Kill();throw 'Preparing a secure library connection timed out. Existing files were kept. Retry after checking Windows security software.'}}
    $Code=$Process.ExitCode;$Process.Dispose()
    if($Code -ne 0){throw 'A library HTTPS key could not be prepared. Existing folders were kept. Run the current Carribrary installer to repair service permissions, then retry.'}
}
function CheckRegistry($Current){
    $Ports=New-Object 'Collections.Generic.HashSet[int]'
    foreach($Name in @('AdminPort','CatalogPort','PhonePort','DiscoveryPort')){[void]$Ports.Add([int]$Config.Library.$Name)}
    foreach($Library in @($Current.libraries)){
        if(!$Library){continue}
        $Id=[Guid]::Empty
        if(![Guid]::TryParseExact([string]$Library.id,'N',[ref]$Id)){throw 'A library identity is invalid. Nothing was applied.'}
        if(!$Library.name -or ([string]$Library.name).Length -gt 80){throw 'A library name is invalid.'}
        foreach($Name in @('AdminPort','CatalogPort','PhonePort','DiscoveryPort')){if([int]$Library.$Name -lt 1024 -or [int]$Library.$Name -gt 65535 -or !$Ports.Add([int]$Library.$Name)){throw 'Library ports overlap. Review Libraries & network before applying.'}}
        $Folder=Join-Path (Join-Path $Libraries ([string]$Library.id)) 'data'
        if(!(Test-Path -LiteralPath $Folder -PathType Container)){throw ('Data for '+$Library.name+' is missing. Restore that library''s own backup first. No empty replacement was created.')}
    }
    if($Current.join.enabled){$Id=[Guid]::Empty;if(![Guid]::TryParseExact([string]$Current.join.id,'N',[ref]$Id) -or [int]$Current.join.port -lt 1024 -or [int]$Current.join.port -gt 65535 -or !$Ports.Add([int]$Current.join.port)){throw 'The private connection port or identity is invalid.'}}
}
$Apply.Add_Click({
    $Apply.Enabled=$false;$Undo.Enabled=$false;$Form.UseWaitCursor=$true;$Stopped=$false;$PausedTask=$false;$TaskWasEnabled=$false;$TaskWasRunning=$false;$ReadyBefore=$false
    try{
        $ReadyPath=Join-Path $Data 'network-windows-ready'
        $Machine=[string](Get-ItemPropertyValue 'HKLM:\SOFTWARE\Microsoft\Cryptography' -Name MachineGuid)
        $ReadyBefore=(Test-Path -LiteralPath $ReadyPath) -and ([IO.File]::ReadAllText($ReadyPath).Trim() -eq $Machine)
        Stage 'Pausing Carribrary safely...'
        Stop-Service HomeLibraryNative -ErrorAction Stop;$Stopped=$true
        (Get-Service HomeLibraryNative).WaitForStatus('Stopped',[TimeSpan]::FromSeconds(40));$Stopped=$true
        $Current=if(Test-Path -LiteralPath $Registry){Get-Content -LiteralPath $Registry -Raw | ConvertFrom-Json}else{$null}
        $PublicNow=if(Test-Path -LiteralPath $PublicFile){Get-Content -LiteralPath $PublicFile -Raw | ConvertFrom-Json}else{$null}
        $RegistryNow=if(Test-Path -LiteralPath $Registry){[IO.File]::ReadAllText($Registry)}else{''}
        $PublicTextNow=if(Test-Path -LiteralPath $PublicFile){[IO.File]::ReadAllText($PublicFile)}else{''}
        if($RegistryNow -ne $RegistryReview -or $PublicTextNow -ne $PublicReview){throw 'Settings changed after this review opened. Close and reopen the assistant to review the current setup.'}
        CheckRegistry $Current
        Repair-LibraryDataPermissions $Libraries
        foreach($Library in @($Current.libraries)){
            if(!$Library){continue}
            Stage ('Preparing '+$Library.name+'...')
            $Folder=Join-Path (Join-Path $Libraries ([string]$Library.id)) 'data'
            Repair-LibraryDataPermissions $Folder
            PrepareIdentity $Folder $Library
            if(!$Library.initialized){$Library.initialized=$true;$Library.enabled=$true}
            $Rule='Carribrary hosted phone '+$Library.id
            Get-NetFirewallRule -DisplayName $Rule -ErrorAction SilentlyContinue | Remove-NetFirewallRule
            New-NetFirewallRule -DisplayName $Rule -Direction Inbound -Action Allow -Protocol TCP -LocalPort ([int]$Library.phonePort) -Program $Server -Profile Private,Public | Out-Null
        }
        if($Current){$Temp=$Registry+'.assistant.tmp';[IO.File]::WriteAllText($Temp,($Current | ConvertTo-Json -Depth 12),(New-Object Text.UTF8Encoding($false)));Move-Item -LiteralPath $Temp -Destination $Registry -Force}
        if($Current.join.enabled){
            Stage 'Preparing the private connection to the first computer...'
            PrepareIdentity $Data $null -Gateway
            Get-NetFirewallRule -DisplayName 'Carribrary private library bridge' -ErrorAction SilentlyContinue | Remove-NetFirewallRule
            New-NetFirewallRule -DisplayName 'Carribrary private library bridge' -Direction Inbound -Action Allow -Protocol TCP -LocalPort ([int]$Current.join.port) -RemoteAddress LocalSubnet -Program $Server -Profile Private,Public | Out-Null
        }
        if($PublicNow.enabled -or $Current.join.enabled){
            $Task=Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
            if($Task -and !([string]$Task.Actions.Arguments).Contains((Join-Path $Root 'public-proxy\Run-PublicProxy.ps1'))){throw 'An existing public proxy task belongs to a different installation. It was not changed. Use the advanced proxy instructions.'}
            if(!(Test-Path -LiteralPath $UndoFile)){[IO.File]::WriteAllText($UndoFile,(@{legacyTaskPresent=[bool]$Task;legacyTaskEnabled=([bool]$Task -and $Task.State -ne 'Disabled');legacyTaskRunning=([bool]$Task -and $Task.State -eq 'Running')} | ConvertTo-Json),(New-Object Text.UTF8Encoding($false)))}
            if($Task -and !$ReadyBefore){$TaskWasEnabled=$Task.State -ne 'Disabled';$TaskWasRunning=$Task.State -eq 'Running';Stage 'Moving this installation to the reviewed public routing...';Stop-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue;Disable-ScheduledTask -TaskName $TaskName | Out-Null;$PausedTask=$true}
        }
        if($PublicNow.enabled){
            $Caddy=Join-Path $Data 'public-https\caddy.exe'
            if(!(Test-Path -LiteralPath $Caddy)){throw 'Return to Public sharing setup and approve the verified Caddy download first.'}
            Stage 'Allowing only Carribrary public HTTPS ports...'
            Get-NetFirewallRule -DisplayName 'Carribrary managed public HTTPS' -ErrorAction SilentlyContinue | Remove-NetFirewallRule
            New-NetFirewallRule -DisplayName 'Carribrary managed public HTTPS' -Direction Inbound -Action Allow -Protocol TCP -LocalPort 80,443 -Program $Caddy -Profile Private,Public | Out-Null
            [IO.File]::WriteAllText((Join-Path $Data 'network-windows-ready'),[string](Get-ItemPropertyValue 'HKLM:\SOFTWARE\Microsoft\Cryptography' -Name MachineGuid))
        }
        Start-Service HomeLibraryNative;$Stopped=$false
        Stage 'Finished. Open Carribrary and check each library. Test public HTTPS from a phone with Wi-Fi off.'
        $Undo.Enabled=Test-Path -LiteralPath $UndoFile
    }catch{
        if(!$ReadyBefore){Remove-Item -LiteralPath (Join-Path $Data 'network-windows-ready') -ErrorAction SilentlyContinue}
        if($PausedTask){try{if($TaskWasEnabled){Enable-ScheduledTask -TaskName $TaskName | Out-Null;if($TaskWasRunning){Start-ScheduledTask -TaskName $TaskName}}else{Disable-ScheduledTask -TaskName $TaskName | Out-Null}}catch{}}
        [Windows.Forms.MessageBox]::Show($_.Exception.Message,'Carribrary hosting setup','OK','Error') | Out-Null
        Stage 'Setup did not finish. Existing library folders were kept. Review the message and retry.'
    }finally{if($Stopped){try{Start-Service HomeLibraryNative}catch{}}$Form.UseWaitCursor=$false;$Apply.Enabled=$Agree.Checked;$Undo.Enabled=Test-Path -LiteralPath $UndoFile}
})
$Undo.Add_Click({
    if([Windows.Forms.MessageBox]::Show('Restore the previous Carribrary public-proxy task? New managed public routes will stop. Library data and secure keys are kept.','Undo public migration','YesNo','Question') -ne 'Yes'){return}
    $Stopped=$false
    try{
        Stop-Service HomeLibraryNative;$Stopped=$true;(Get-Service HomeLibraryNative).WaitForStatus('Stopped',[TimeSpan]::FromSeconds(40));$Stopped=$true
        if(Test-Path -LiteralPath $PublicFile){$Saved=Get-Content -LiteralPath $PublicFile -Raw | ConvertFrom-Json;$Saved.enabled=$false;[IO.File]::WriteAllText($PublicFile,($Saved | ConvertTo-Json -Depth 8),(New-Object Text.UTF8Encoding($false)))}
        if(Test-Path -LiteralPath $Registry){$Saved=Get-Content -LiteralPath $Registry -Raw | ConvertFrom-Json;$Saved.join.enabled=$false;[IO.File]::WriteAllText($Registry,($Saved | ConvertTo-Json -Depth 12),(New-Object Text.UTF8Encoding($false)))}
        Get-NetFirewallRule -DisplayName 'Carribrary managed public HTTPS' -ErrorAction SilentlyContinue | Remove-NetFirewallRule
        $Previous=Get-Content -LiteralPath $UndoFile -Raw | ConvertFrom-Json
        if($Previous.legacyTaskPresent -and $Previous.legacyTaskEnabled){Enable-ScheduledTask -TaskName $TaskName | Out-Null;if($Previous.legacyTaskRunning){Start-ScheduledTask -TaskName $TaskName}}
        Start-Service HomeLibraryNative;$Stopped=$false
        Stage 'Previous public setup restored. Libraries and their data were kept.'
    }catch{[Windows.Forms.MessageBox]::Show($_.Exception.Message,'Undo public migration','OK','Error') | Out-Null}finally{if($Stopped){try{Start-Service HomeLibraryNative}catch{}}}
})
[void]$Form.ShowDialog()
