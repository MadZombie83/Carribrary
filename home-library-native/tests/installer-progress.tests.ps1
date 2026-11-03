$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot '../installer/Setup-Progress.ps1')
$errors=$null;$tokens=$null
foreach($name in @('Setup-Progress.ps1','Prepare-Install.ps1','Install-Managed.ps1')) {
    $null=[System.Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot ('../installer/'+$name)),[ref]$tokens,[ref]$errors)
    if($errors.Count){throw ($errors | Out-String)}
}
$folder=Join-Path ([IO.Path]::GetTempPath()) ('carribrary-setup-test-'+[Guid]::NewGuid().ToString('N'));New-Item -ItemType Directory $folder | Out-Null
try {
    $log=Join-Path $folder 'command.log'
    if($IsLinux) {
        Invoke-CarribrarySetupCommand -FilePath '/bin/echo' -Arguments @('two words','café') -OutputPath $log -TimeoutSeconds 5
        if((Get-Content $log -Raw).Trim() -ne 'two words café'){throw 'Argument boundaries or text output were not preserved'}
        $failed=$false
        try{Invoke-CarribrarySetupCommand -FilePath '/bin/false' -Arguments @() -OutputPath $log -TimeoutSeconds 5}catch{if($_.Exception.Message -notlike '*failed*'){throw};$failed=$true}
        if(!$failed){throw 'Nonzero helper exit was accepted'}
        $timedOut=$false
        try {Invoke-CarribrarySetupCommand -FilePath '/bin/sleep' -Arguments @('3') -OutputPath $log -TimeoutSeconds 1}catch{if($_.Exception.Message -notlike '*exceeded*'){throw};$timedOut=$true}
        if(!$timedOut){throw 'Hanging helper did not time out'}
    }
    if($IsLinux){Write-Output 'PASS setup script syntax, process output/arguments and timeout (no Windows installation performed)'}else{Write-Output 'PASS setup script syntax; process fixtures and actual Windows installation were not run'}
} finally {Remove-Item $folder -Recurse -Force}
