# Scoped to this application's data directory. File contents and key material are retained.
function Repair-LibraryDataPermissions([string]$Data) {
    $Path=[IO.Path]::GetFullPath($Data).TrimEnd('\')
    if ($Path -eq [IO.Path]::GetPathRoot($Path).TrimEnd('\')) { throw 'Refusing to change permissions on a drive root.' }
    New-Item -ItemType Directory -Force -Path $Path | Out-Null
    $Nodes=@(Get-Item -LiteralPath $Path) + @(Get-ChildItem -LiteralPath $Path -Force -Recurse)
    foreach($Node in $Nodes) {
        if ($Node.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'The data directory contains a filesystem link. Automatic permission repair was stopped.' }
    }
    $Admin=New-Object Security.Principal.SecurityIdentifier('S-1-5-32-544')
    $System=New-Object Security.Principal.SecurityIdentifier('S-1-5-18')
    $LocalService=New-Object Security.Principal.SecurityIdentifier('S-1-5-19')
    foreach ($Node in $Nodes) {
        if ($Node.PSIsContainer) {
            $Acl=New-Object Security.AccessControl.DirectorySecurity
            $Inheritance=[Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [Security.AccessControl.InheritanceFlags]::ObjectInherit
        } else {
            $Acl=New-Object Security.AccessControl.FileSecurity
            $Inheritance=[Security.AccessControl.InheritanceFlags]::None
        }
        $Acl.SetAccessRuleProtection($true,$false)
        foreach ($Sid in @($Admin,$System)) {
            $Rule=New-Object Security.AccessControl.FileSystemAccessRule($Sid,[Security.AccessControl.FileSystemRights]::FullControl,$Inheritance,[Security.AccessControl.PropagationFlags]::None,[Security.AccessControl.AccessControlType]::Allow)
            $Acl.AddAccessRule($Rule)
        }
        $Rule=New-Object Security.AccessControl.FileSystemAccessRule($LocalService,[Security.AccessControl.FileSystemRights]::Modify,$Inheritance,[Security.AccessControl.PropagationFlags]::None,[Security.AccessControl.AccessControlType]::Allow)
        $Acl.AddAccessRule($Rule)
        try { Set-Acl -LiteralPath $Node.FullName -AclObject $Acl }
        catch {
            # Recovery for a file left with an owner-only/protected ACL by the earlier setup.
            & takeown.exe /F $Node.FullName /A | Out-Null
            if ($LASTEXITCODE -ne 0) { throw 'Could not recover ownership of '+$Node.FullName }
            Set-Acl -LiteralPath $Node.FullName -AclObject $Acl
        }
        if (-not $Node.PSIsContainer -and ($Node.Attributes -band [IO.FileAttributes]::ReadOnly)) {
            [IO.File]::SetAttributes($Node.FullName,($Node.Attributes -band (-bnot [IO.FileAttributes]::ReadOnly)))
        }
    }
}
function Save-LibraryInstallationSettings([string]$Source,[string]$Data) {
    # Write text instead of copying source metadata/attributes into writable ProgramData.
    $Target=Join-Path $Data 'installation-appsettings.json'
    if (Test-Path -LiteralPath $Target) {
        $Attributes=[IO.File]::GetAttributes($Target)
        [IO.File]::SetAttributes($Target,($Attributes -band (-bnot [IO.FileAttributes]::ReadOnly)))
    }
    [IO.File]::WriteAllText($Target,[IO.File]::ReadAllText($Source),(New-Object Text.UTF8Encoding($false)))
}
