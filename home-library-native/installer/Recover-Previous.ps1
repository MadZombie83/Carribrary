param([Parameter(Mandatory=$true)][string]$Root,[string]$Point='', [switch]$Automatic)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms
try {
    $Principal=New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    if (!$Principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        Start-Process -FilePath (Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe') -Verb RunAs -ArgumentList ('-NoProfile -STA -ExecutionPolicy Bypass -WindowStyle Hidden -File "'+$PSCommandPath+'" -Root "'+$Root+'"') | Out-Null
        exit 0
    }
    if (!$Automatic) {
        $Maintenance=New-Object Threading.Mutex($false,'Global\CarribraryMaintenance')
        try { $Acquired=$Maintenance.WaitOne(0) } catch [Threading.AbandonedMutexException] { $Acquired=$true }
        if (!$Acquired) { throw 'Another Carribrary update or recovery tool is open. Close it before continuing.' }
    }
    . (Join-Path $PSScriptRoot 'Recovery.Windows.ps1')
    if ($Automatic) {
        if (!$Point) { exit 0 }
        $Version=Invoke-CarribraryWindowsRecovery $Root $Point -UseSavedSettings
        [Windows.Forms.MessageBox]::Show('The update could not finish. Carribrary '+$Version+' was restored. Current library data and private lists were kept.','Carribrary recovery','OK','Information') | Out-Null
        exit 0
    }
    $RecoveryHome=Join-Path $env:ProgramData 'HomeLibraryNative\recovery'
    $Form=New-Object Windows.Forms.Form; $Form.Text='Carribrary Update & Recovery'; $Form.Width=720; $Form.Height=460; $Form.StartPosition='CenterScreen'
    $Label=New-Object Windows.Forms.Label; $Label.Text="Restore a previous compatible application while keeping CURRENT books, loans, accounts, private lists and connection settings.`r`n`r`nThis tool does not restore an old database. Complete private snapshots are retained separately for supervised recovery. Supported program versions: 2.0.39–2.0.42 portrait sidebar-artwork cohort only. Earlier programs cannot read full-sidebar PNG dimensions; automatic recovery cannot cross this boundary."; $Label.SetBounds(20,15,660,110); $Form.Controls.Add($Label)
    $List=New-Object Windows.Forms.ListBox; $List.SetBounds(20,135,660,150); $Form.Controls.Add($List)
    $Points=New-Object 'Collections.Generic.List[string]'
    if (Test-Path -LiteralPath $RecoveryHome) {
        foreach ($Dir in @(Get-ChildItem -LiteralPath $RecoveryHome -Directory | Where-Object {$_.Name -notlike '.*'} | Sort-Object Name -Descending)) {
            try { Assert-CarribraryTrustedPoint $Dir.FullName; $Meta=Get-Content -LiteralPath (Join-Path $Dir.FullName 'point.json') -Raw | ConvertFrom-Json; if ($Meta.product -eq 'Carribrary' -and $Meta.purpose -eq 'update' -and (Test-CarribraryCompatible $Meta.sourceVersion)) { $Points.Add($Dir.FullName); [void]$List.Items.Add($Meta.sourceVersion+' - '+$Meta.createdUtc) } } catch { }
        }
    }
    if ($List.Items.Count) { $List.SelectedIndex=0 } else { [void]$List.Items.Add('No compatible recovery point yet. One is created before a supported update.') }
    $Status=New-Object Windows.Forms.Label; $Status.SetBounds(20,295,660,45); $Status.Text='Recovery also works when the main library page cannot open.'; $Form.Controls.Add($Status)
    $Restore=New-Object Windows.Forms.Button; $Restore.Text='Restore selected application'; $Restore.SetBounds(20,350,240,35); $Restore.Enabled=$Points.Count -gt 0; $Form.Controls.Add($Restore)
    $Restore.Add_Click({
        if ($List.SelectedIndex -lt 0 -or $List.SelectedIndex -ge $Points.Count) { return }
        if ([Windows.Forms.MessageBox]::Show('Stop Carribrary and restore the selected application? Current library data and private lists will be kept. A fresh private safety snapshot is created first.','Confirm recovery','YesNo','Question') -ne 'Yes') { return }
        $Restore.Enabled=$false; $Status.Text='Checking snapshots and restoring the application. Please wait...'; $Form.Refresh()
        try { $Version=Invoke-CarribraryWindowsRecovery $Root $Point -UseSavedSettingss[$List.SelectedIndex]; $Status.Text='Carribrary '+$Version+' is running. Current library data was kept.' }
        catch { $Status.Text='Recovery did not finish. Safety snapshots were retained.'; [Windows.Forms.MessageBox]::Show($_.Exception.Message,'Carribrary recovery','OK','Error') | Out-Null }
        finally { $Restore.Enabled=$true }
    })
    $Create=New-Object Windows.Forms.Button; $Create.Text='Create recovery point'; $Create.SetBounds(280,350,180,35); $Form.Controls.Add($Create)
    $Create.Add_Click({
        $Create.Enabled=$false; $Restore.Enabled=$false; $Status.Text='Stopping the library briefly and creating a complete private snapshot...'; $Form.Refresh()
        try {
            if (!(Test-CarribraryRecoveryHealth $Root)) { throw 'The library is not healthy enough to mark this application as a recovery candidate. Restore a previous application instead.' }
            $Config=Get-Content -LiteralPath (Join-Path $Root 'server/appsettings.json') -Raw | ConvertFrom-Json
            $Data=Join-Path $env:ProgramData 'HomeLibraryNative\data'; if ($Config.Library.DataDirectory) { $Data=$Config.Library.DataDirectory }
            $Version=Get-CarribraryProgramVersion $Root
            if (!(Test-CarribraryCompatible $Version)) { throw 'This application version is outside the supported recovery cohort.' }
            Stop-CarribraryRecoveryService $Root
            try { $NewPoint=New-CarribraryRecoveryPoint -Root $Root -Data $Data -RecoveryHome $RecoveryHome -SourceVersion $Version -Protect {param($Path) Protect-CarribraryRecovery $Path} }
            finally { Start-Service HomeLibraryNative }
            Register-CarribraryRecoveryPoint $NewPoint
            if (!(Test-CarribraryRecoveryHealth $Root)) { throw 'The snapshot was created, but the library did not restart. Your private snapshot was retained.' }
            $Points.Insert(0,$NewPoint); $List.Items.Insert(0,$Version+' - '+[DateTimeOffset]::UtcNow.ToString('o')); $List.SelectedIndex=0; $Status.Text='Complete private recovery point verified. The library is running again.'
        } catch { [Windows.Forms.MessageBox]::Show($_.Exception.Message,'Carribrary recovery','OK','Error') | Out-Null; $Status.Text='The recovery point could not be completed. Review the message before continuing.' }
        finally { $Create.Enabled=$true; $Restore.Enabled=$Points.Count -gt 0 }
    })
    $Clear=New-Object Windows.Forms.Button; $Clear.Text='Delete recovery points'; $Clear.SetBounds(480,350,200,35); $Form.Controls.Add($Clear)
    $Clear.Add_Click({
        $Confirm=New-Object Windows.Forms.Form; $Confirm.Text='Delete private recovery points'; $Confirm.Width=600; $Confirm.Height=240; $Confirm.StartPosition='CenterParent'
        $Explain=New-Object Windows.Forms.Label; $Explain.Text='Permanently delete ALL installation-managed recovery points, including private data snapshots. Previous-version recovery will no longer be available. Current library data is kept. External copies are not removed. Type DELETE RECOVERY POINTS to continue.'; $Explain.SetBounds(15,15,550,90); $Confirm.Controls.Add($Explain)
        $Text=New-Object Windows.Forms.TextBox; $Text.SetBounds(15,110,550,25); $Confirm.Controls.Add($Text)
        $Delete=New-Object Windows.Forms.Button; $Delete.Text='Delete permanently'; $Delete.SetBounds(15,150,180,30); $Confirm.Controls.Add($Delete)
        $Delete.Add_Click({
            if ($Text.Text -cne 'DELETE RECOVERY POINTS') { return }
            try {
                if (Test-Path -LiteralPath $RecoveryHome) {
                    Assert-CarribraryTree $RecoveryHome
                    foreach ($Dir in @(Get-ChildItem -LiteralPath $RecoveryHome -Directory -Force)) {
                        if ($Dir.Name -match '^(\.creating-)?[0-9]{8}-[0-9]{6}-[a-f0-9]{32}$') { Remove-Item -LiteralPath $Dir.FullName -Recurse -Force; Remove-ItemProperty 'HKLM:\Software\HomeLibraryNative\RecoveryPoints' -Name $Dir.Name -ErrorAction SilentlyContinue }
                    }
                }
                $Points.Clear(); $List.Items.Clear(); [void]$List.Items.Add('Recovery points deleted. Current library data was kept.'); $Restore.Enabled=$false; $Status.Text='Private recovery points removed. External copies and ordinary library backups are separate.'; $Confirm.Close()
            } catch { [Windows.Forms.MessageBox]::Show($_.Exception.Message,'Recovery point cleanup','OK','Error') | Out-Null }
        })
        [void]$Confirm.ShowDialog($Form); $Confirm.Dispose()
    })
    [void]$Form.ShowDialog(); $Form.Dispose()
} catch { [Windows.Forms.MessageBox]::Show($_.Exception.Message,'Carribrary recovery','OK','Error') | Out-Null; exit 1 }
