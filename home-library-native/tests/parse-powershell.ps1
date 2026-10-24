$ErrorActionPreference='Stop'
foreach ($File in Get-ChildItem (Join-Path $PSScriptRoot '..') -Filter '*.ps1' -Recurse | Where-Object {$_.FullName -notmatch '[\\/](release|bin|obj|build)[\\/]'}) {
    $Tokens=$null; $Errors=$null
    [Management.Automation.Language.Parser]::ParseFile($File.FullName,[ref]$Tokens,[ref]$Errors) | Out-Null
    if ($Errors.Count) { throw ($File.Name+': '+($Errors.Message -join '; ')) }
}
Write-Output 'PASS all source PowerShell scripts parse'
