# Shared Windows setup progress. Logs contain stage names only, never command arguments/keys.
function Start-CarribrarySetupProgress([string]$Title) {
    Add-Type -AssemblyName System.Windows.Forms
    $script:SetupClock=[Diagnostics.Stopwatch]::StartNew()
    $script:SetupLogDirectory=Join-Path $env:ProgramData 'HomeLibraryNative\setup-logs'
    New-Item -ItemType Directory -Force -Path $script:SetupLogDirectory | Out-Null
    $script:SetupLog=Join-Path $script:SetupLogDirectory ('setup-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,8)+'.log')
    # Bound setup logs separately from library backups; retain the ten newest logs.
    Get-ChildItem -LiteralPath $script:SetupLogDirectory -Filter 'setup-*.log' -File | Sort-Object LastWriteTimeUtc -Descending | Select-Object -Skip 9 | Remove-Item -Force -ErrorAction SilentlyContinue
    $script:SetupWindow=New-Object Windows.Forms.Form
    $script:SetupWindow.Text=$Title
    $script:SetupWindow.Width=570;$script:SetupWindow.Height=235
    $script:SetupWindow.StartPosition='CenterScreen';$script:SetupWindow.FormBorderStyle='FixedDialog'
    $script:SetupWindow.ControlBox=$false;$script:SetupWindow.TopMost=$true
    $script:SetupLabel=New-Object Windows.Forms.Label
    $script:SetupLabel.SetBounds(18,18,520,52);$script:SetupLabel.Text='Preparing setup...'
    $bar=New-Object Windows.Forms.ProgressBar;$bar.SetBounds(18,78,520,20);$bar.Style='Marquee'
    $script:SetupElapsed=New-Object Windows.Forms.Label;$script:SetupElapsed.SetBounds(18,110,520,70)
    $script:SetupWindow.Controls.AddRange(@($script:SetupLabel,$bar,$script:SetupElapsed))
    $script:SetupWindow.Show();Update-CarribrarySetupProgress
    Write-CarribrarySetupStep 'Setup started. Library data and recovery copies are retained.'
}
function Update-CarribrarySetupProgress {
    if($script:SetupWindow -and !$script:SetupWindow.IsDisposed) {
        $script:SetupElapsed.Text='Elapsed: '+$script:SetupClock.Elapsed.ToString('mm\:ss')+"`r`nKeep setup open. Large recovery copies can take several minutes.`r`nLog: "+$script:SetupLog
        [Windows.Forms.Application]::DoEvents()
    }
}
function Write-CarribrarySetupStep([string]$Message) {
    $entry=(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')+' '+$Message
    if($script:SetupLog){Add-Content -LiteralPath $script:SetupLog -Value $entry -Encoding UTF8}
    Write-Output $Message
    if($script:SetupLabel){$script:SetupLabel.Text=$Message}
    Update-CarribrarySetupProgress
}
function Invoke-CarribrarySetupCommand([string]$FilePath,[string[]]$Arguments,[string]$OutputPath,[int]$TimeoutSeconds=300) {
    # These arguments are switches and Windows file paths, which cannot contain quotes.
    foreach($value in $Arguments){if($value.Contains('"') -or $value.Contains([char]0)){throw 'Setup received an invalid file path.'}}
    $info=New-Object Diagnostics.ProcessStartInfo
    $info.FileName=$FilePath;$info.Arguments=($Arguments | ForEach-Object {'"'+$_+'"'}) -join ' '
    $info.UseShellExecute=$false;$info.CreateNoWindow=$true;$info.RedirectStandardOutput=$true;$info.RedirectStandardError=$true
    $process=New-Object Diagnostics.Process;$process.StartInfo=$info
    try {
        if(!$process.Start()){throw 'The setup helper could not start.'}
        $stdout=$process.StandardOutput.ReadToEndAsync();$stderr=$process.StandardError.ReadToEndAsync();$clock=[Diagnostics.Stopwatch]::StartNew()
        while(!$process.WaitForExit(100)) {
            Update-CarribrarySetupProgress
            if($clock.Elapsed.TotalSeconds -ge $TimeoutSeconds){$process.Kill();if($process.WaitForExit(5000)){[IO.File]::WriteAllText($OutputPath,$stdout.GetAwaiter().GetResult()+$stderr.GetAwaiter().GetResult())};throw ('This setup step exceeded '+$TimeoutSeconds+' seconds. Setup stopped; keep your library data and recovery copy. Review the setup log before trying again.')}
        }
        [IO.File]::WriteAllText($OutputPath,$stdout.GetAwaiter().GetResult()+$stderr.GetAwaiter().GetResult())
        if($process.ExitCode -ne 0){throw ('The setup helper failed. Review its local log: '+$OutputPath)}
    } finally {$process.Dispose()}
}
function Show-CarribrarySetupFailure([string]$Message) {
    Write-CarribrarySetupStep ('Setup stopped. Review local diagnostics; no data was intentionally removed.')
    $detail=$Message+"`r`nSetup log: "+$script:SetupLog+"`r`nKeep this log private. Do not delete your library to retry setup."
    if($script:SetupWindow){[Windows.Forms.MessageBox]::Show($script:SetupWindow,$detail,'Carribrary setup','OK','Error') | Out-Null}
    else{[Windows.Forms.MessageBox]::Show($detail,'Carribrary setup','OK','Error') | Out-Null}
}
function Close-CarribrarySetupProgress {
    if($script:SetupWindow){$script:SetupWindow.Close();$script:SetupWindow.Dispose()}
}
