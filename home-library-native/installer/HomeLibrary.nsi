Unicode true
!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "x64.nsh"
!include "WinVer.nsh"
!include "nsDialogs.nsh"
!ifndef PAYLOAD
!define PAYLOAD "../release"
!endif
!ifndef PROJECT
!define PROJECT ".."
!endif
!ifndef OUTPUT
!define OUTPUT "../../Carribrary-Setup-2.0.54-preview.exe"
!endif
Icon "${PROJECT}/assets/home-library.ico"
!define MUI_ICON "${PROJECT}/assets/home-library.ico"
Name "Carribrary"
OutFile "${OUTPUT}"
InstallDir "$PROGRAMFILES64\Carribrary"
InstallDirRegKey HKLM "Software\HomeLibraryNative" "InstallDir"
RequestExecutionLevel admin
SetCompressor /SOLID lzma
SetCompressorDictSize 32
ShowInstDetails show
ShowUninstDetails show
BrandingText "Carribrary - household library server"
VIProductVersion "2.0.54.0"
VIAddVersionKey "ProductName" "Carribrary"
VIAddVersionKey "FileDescription" "Carribrary Windows installer"
VIAddVersionKey "FileVersion" "2.0.54-preview"
VIAddVersionKey "LegalCopyright" "Copyright 2026 Chezburber Studios"
VIAddVersionKey "CompanyName" "Chezburber Studios"
Var MaintenanceHandle
Var RecoveryPoint
Var RecoveryAttempted
Var PowerShell
Var BackupPath
Var BackupInput
Var BackupBrowse
Var TrayCheckbox
Var StartTray
Var Dialog
Var Result
!define MUI_WELCOMEPAGE_TITLE "Install Carribrary"
!define MUI_WELCOMEPAGE_TEXT "This wizard installs the Windows library server and tray app, starts the service automatically, and creates shortcuts.$\r$\n$\r$\nExport a backup from your previous library first. Close its server window before continuing. You may select your backup during setup.$\r$\n$\r$\nExisting native library data, backups and paired phones are retained. Node.js and a separate .NET installation are not needed."
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
Page custom OptionsPage OptionsLeave
!insertmacro MUI_PAGE_INSTFILES
!define MUI_FINISHPAGE_TITLE "Carribrary is installed"
!define MUI_FINISHPAGE_TEXT "The library service starts automatically with Windows. Use the Carribrary shortcuts on your desktop or Start menu.$\r$\n$\r$\nFor access from work, open Connection setup from the Start menu. Router port forwarding still needs to be configured on your router."
!define MUI_FINISHPAGE_RUN
!define MUI_FINISHPAGE_RUN_TEXT "Open Carribrary"
!define MUI_FINISHPAGE_RUN_FUNCTION OpenLibrary
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "English"
Function .onInit
  ${IfNot} ${RunningX64}
    MessageBox MB_OK|MB_ICONSTOP "This installer needs 64-bit Windows 10 or 11."
    Abort
  ${EndIf}
  ${IfNot} ${AtLeastWin10}
    MessageBox MB_OK|MB_ICONSTOP "Carribrary needs Windows 10 or later."
    Abort
  ${EndIf}
  System::Call 'kernel32::CreateMutexW(p 0, i 0, w "Global\CarribraryMaintenance") p .r0'
  StrCpy $MaintenanceHandle $0
  System::Call 'kernel32::WaitForSingleObject(p r0, i 0) i .r1'
  ${If} $1 != 0
  ${AndIf} $1 != 128
    MessageBox MB_OK|MB_ICONSTOP "Another Carribrary update or recovery tool is open. Close it before continuing."
    Abort
  ${EndIf}
  SetRegView 64
  SetShellVarContext all
  ReadRegStr $0 HKLM "Software\HomeLibraryNative" "InstallDir"
  ${If} $0 != ""
    StrCpy $INSTDIR $0
  ${EndIf}
  StrCpy $PowerShell "$WINDIR\Sysnative\WindowsPowerShell\v1.0\powershell.exe"
  StrCpy $RecoveryPoint ""
  StrCpy $RecoveryAttempted "0"
  StrCpy $BackupPath ""
  StrCpy $StartTray ${BST_CHECKED}
FunctionEnd
Function OptionsPage
  !insertmacro MUI_HEADER_TEXT "Library setup" "Choose an optional backup and tray startup preference."
  nsDialogs::Create 1018
  Pop $Dialog
  ${If} $Dialog == error
    Abort
  ${EndIf}
  ${NSD_CreateLabel} 0 0 100% 30u "Optional: select your existing JSON backup (or stopped library.sqlite). Import works into an empty library only. Leave blank to retain existing native records."
  Pop $0
  ${NSD_CreateFileRequest} 0 36u 78% 14u "$BackupPath"
  Pop $BackupInput
  ${NSD_CreateBrowseButton} 81% 36u 19% 14u "Browse..."
  Pop $BackupBrowse
  ${NSD_OnClick} $BackupBrowse BrowseBackup
  ${NSD_CreateCheckbox} 0 65u 100% 16u "Show the Carribrary tray icon when anyone signs in"
  Pop $TrayCheckbox
  ${NSD_SetState} $TrayCheckbox $StartTray
  ${NSD_CreateLabel} 0 92u 100% 35u "Your database and phone pairing keys are stored in ProgramData, separately from the installed program. Uninstalling keeps those records."
  Pop $0
  nsDialogs::Show
FunctionEnd
Function BrowseBackup
  Pop $0
  nsDialogs::SelectFileDialog open "" "Library backup (*.json;*.sqlite)|*.json;*.sqlite|All files (*.*)|*.*"
  Pop $0
  ${If} $0 != ""
    ${NSD_SetText} $BackupInput $0
  ${EndIf}
FunctionEnd
Function OptionsLeave
  ${NSD_GetText} $BackupInput $BackupPath
  ${NSD_GetState} $TrayCheckbox $StartTray
  ${If} $BackupPath != ""
    IfFileExists "$BackupPath" valid_backup 0
    MessageBox MB_OK|MB_ICONSTOP "The selected backup file does not exist. Choose a file or clear the field."
    Abort
    valid_backup:
  ${EndIf}
FunctionEnd
Section "Carribrary" SEC_MAIN
  SectionIn RO
  InitPluginsDir
  SetOutPath "$PLUGINSDIR"
  File "${PROJECT}/installer/Prepare-Install.ps1"
  File "${PROJECT}/Repair-DataPermissions.ps1"
  File "${PROJECT}/installer/Recovery.Core.ps1"
  File "${PROJECT}/installer/Recovery.Windows.ps1"
  File "${PROJECT}/installer/Recover-Previous.ps1"
  DetailPrint "Checking ports and stopping an existing native service..."
  ClearErrors
  ExecWait '$\"$PowerShell$\" -NoProfile -STA -ExecutionPolicy Bypass -WindowStyle Hidden -File $\"$PLUGINSDIR\Prepare-Install.ps1$\" -WorkDirectory $\"$PLUGINSDIR$\" -Root $\"$INSTDIR$\"' $Result
  ${If} ${Errors}
    MessageBox MB_OK|MB_ICONSTOP "Windows could not run the setup helper. Installation stopped."
    Abort
  ${EndIf}
  ${If} $Result != 0
    Abort
  ${EndIf}
  IfFileExists "$PLUGINSDIR\recovery-point.txt" 0 +4
    FileOpen $0 "$PLUGINSDIR\recovery-point.txt" r
    FileRead $0 $RecoveryPoint
    FileClose $0
  SetOutPath "$INSTDIR"
  File "${PROJECT}/installer/Recovery.Core.ps1"
  File "${PROJECT}/installer/Recovery.Windows.ps1"
  File "${PROJECT}/installer/Recover-Previous.ps1"
  File "${PROJECT}/installer/Complete-Backup-Recovery.ps1"
  File "${PAYLOAD}/release-files.json"
  CreateDirectory "$SMPROGRAMS\Carribrary"
  WriteRegStr HKCR "carribrary-recovery" "" "URL:Carribrary Complete Backup Recovery"
  WriteRegStr HKCR "carribrary-recovery" "URL Protocol" ""
  WriteRegStr HKCR "carribrary-recovery\shell\open\command" "" '$\"$WINDIR\System32\WindowsPowerShell\v1.0\powershell.exe$\" -NoProfile -STA -ExecutionPolicy Bypass -File $\"$INSTDIR\Complete-Backup-Recovery.ps1$\" -Root $\"$INSTDIR$\"'
  CreateShortCut "$SMPROGRAMS\Carribrary\Complete Backup Recovery.lnk" "$WINDIR\System32\WindowsPowerShell\v1.0\powershell.exe" '-NoProfile -STA -ExecutionPolicy Bypass -File $\"$INSTDIR\Complete-Backup-Recovery.ps1$\" -Root $\"$INSTDIR$\"' "$INSTDIR\Carribrary.ico" 0 SW_SHOWNORMAL "" "Verify, test and restore a complete backup"
  CreateShortCut "$SMPROGRAMS\Carribrary\Update and recovery.lnk" "$WINDIR\System32\WindowsPowerShell\v1.0\powershell.exe" '-NoProfile -STA -ExecutionPolicy Bypass -WindowStyle Hidden -File $\"$INSTDIR\Recover-Previous.ps1$\" -Root $\"$INSTDIR$\"' "$INSTDIR\Carribrary.ico" 0 SW_SHOWNORMAL "" "Recover even if the library will not start"
  SetOutPath "$INSTDIR\server"
  File /r "${PAYLOAD}/server/*"
  SetOutPath "$INSTDIR\tray"
  File /r "${PAYLOAD}/tray/*"
  SetOutPath "$INSTDIR"
  File "${PROJECT}/Connection-Setup.ps1"
  File "${PROJECT}/Administrator-Access.ps1"
  File "${PROJECT}/Repair-DataPermissions.ps1"
  File /oname=Collect-Diagnostics.ps1 "${PROJECT}/installer/Collect-Diagnostics.ps1"
  File "${PROJECT}/Configure-DuckDNS.ps1"
  File "${PROJECT}/Update-DuckDNS.ps1"
  File "${PROJECT}/Configure-Remote.ps1"
  File "${PROJECT}/Setup-PublicCatalog.ps1"
  File "${PROJECT}/Caddyfile.example"
  File /oname=Carribrary.ico "${PROJECT}/assets/home-library.ico"
  File /oname=README.md "${PROJECT}/distribution/INSTALLED-README.md"
  File "${PROJECT}/ADVANCED-OPERATIONS.md"
  File "${PROJECT}/INSTALLER-GUIDE.txt"
  File "${PROJECT}/THIRD-PARTY-NOTICES.txt"
  File "${PROJECT}/LICENSE.txt"
  File /oname=START-HERE.txt "${PROJECT}/INSTALLER-GUIDE.txt"
  File /oname=Install-Managed.ps1 "${PROJECT}/installer/Install-Managed.ps1"
  File /oname=Uninstall-Managed.ps1 "${PROJECT}/installer/Uninstall-Managed.ps1"
  SetOutPath "$INSTDIR\licenses"
  File /r "${PROJECT}/licenses/*"
  SetOutPath "$INSTDIR"
  DetailPrint "Configuring the service, permissions and firewall; importing the selected backup..."
  ClearErrors
  ExecWait '$\"$PowerShell$\" -NoProfile -STA -ExecutionPolicy Bypass -WindowStyle Hidden -File $\"$INSTDIR\Install-Managed.ps1$\" -Root $\"$INSTDIR$\" -SavedSettings $\"$PLUGINSDIR\saved-appsettings.json$\" -BackupPath $\"$BackupPath$\"' $Result
  ${If} ${Errors}
    MessageBox MB_OK|MB_ICONSTOP "The Windows setup helper could not start. Your library data was retained."
    Abort
  ${EndIf}
  ${If} $Result != 0
    Abort
  ${EndIf}
  ; Remove only shortcuts created by the previous product name. Keep its install registration/data.
  Delete "$DESKTOP\Home Library.lnk"
  Delete "$SMSTARTUP\Home Library.lnk"
  Delete "$SMPROGRAMS\Home Library\Home Library.lnk"
  Delete "$SMPROGRAMS\Home Library\Setup and updates.lnk"
  Delete "$SMPROGRAMS\Home Library\Tray icon.lnk"
  Delete "$SMPROGRAMS\Home Library\Connection setup.lnk"
  Delete "$SMPROGRAMS\Home Library\Setup guide.lnk"
  RMDir "$SMPROGRAMS\Home Library"
  CreateDirectory "$SMPROGRAMS\Carribrary"
  CreateShortCut "$SMPROGRAMS\Carribrary\Carribrary.lnk" "$INSTDIR\Open-Library.url" "" "$INSTDIR\Carribrary.ico" 0
  CreateShortCut "$DESKTOP\Carribrary.lnk" "$INSTDIR\Open-Library.url" "" "$INSTDIR\Carribrary.ico" 0
  CreateShortCut "$SMPROGRAMS\Carribrary\Setup and updates.lnk" "$INSTDIR\Setup-and-Updates.url" "" "$INSTDIR\Carribrary.ico" 0
  CreateShortCut "$SMPROGRAMS\Carribrary\Tray icon.lnk" "$INSTDIR\tray\HomeLibrary.Tray.exe"
  CreateShortCut "$SMPROGRAMS\Carribrary\Connection setup.lnk" "$WINDIR\System32\WindowsPowerShell\v1.0\powershell.exe" '-NoProfile -STA -ExecutionPolicy Bypass -WindowStyle Hidden -File $\"$INSTDIR\Connection-Setup.ps1$\"'
  CreateShortCut "$SMPROGRAMS\Carribrary\Administrator access.lnk" "$WINDIR\System32\WindowsPowerShell\v1.0\powershell.exe" '-NoProfile -STA -ExecutionPolicy Bypass -WindowStyle Hidden -File $\"$INSTDIR\Administrator-Access.ps1$\"'
  CreateShortCut "$SMPROGRAMS\Carribrary\Setup guide.lnk" "$INSTDIR\INSTALLER-GUIDE.txt"
  Delete "$SMSTARTUP\Carribrary.lnk"
  ${If} $StartTray == ${BST_CHECKED}
    CreateShortCut "$SMSTARTUP\Carribrary.lnk" "$INSTDIR\tray\HomeLibrary.Tray.exe"
  ${EndIf}
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  WriteRegStr HKLM "Software\HomeLibraryNative" "InstallDir" "$INSTDIR"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\HomeLibraryNative" "DisplayName" "Carribrary"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\HomeLibraryNative" "DisplayVersion" "2.0.54-preview"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\HomeLibraryNative" "InstallLocation" "$INSTDIR"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\HomeLibraryNative" "UninstallString" '$\"$INSTDIR\Uninstall.exe$\"'
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\HomeLibraryNative" "DisplayIcon" "$INSTDIR\tray\HomeLibrary.Tray.exe"
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\HomeLibraryNative" "NoModify" 1
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\HomeLibraryNative" "NoRepair" 1
  Exec '$\"$INSTDIR\tray\HomeLibrary.Tray.exe$\"'
SectionEnd
Function .onInstFailed
  ${If} $RecoveryPoint != ""
  ${AndIf} $RecoveryAttempted == "0"
    StrCpy $RecoveryAttempted "1"
    ExecWait '$\"$PowerShell$\" -NoProfile -STA -ExecutionPolicy Bypass -WindowStyle Hidden -File $\"$PLUGINSDIR\Recover-Previous.ps1$\" -Root $\"$INSTDIR$\" -Point $\"$RecoveryPoint$\" -Automatic' $Result
  ${EndIf}
FunctionEnd
Function OpenLibrary
  ExecWait '$\"$PowerShell$\" -NoProfile -STA -ExecutionPolicy Bypass -WindowStyle Hidden -File $\"$INSTDIR\Administrator-Access.ps1$\" -OnlyIfCodeExists' $Result
  ExecShell "open" "$INSTDIR\Open-Library.url"
FunctionEnd
Function un.onInit
  SetRegView 64
  SetShellVarContext all
  StrCpy $PowerShell "$WINDIR\Sysnative\WindowsPowerShell\v1.0\powershell.exe"
FunctionEnd
Section "Uninstall"
  ClearErrors
  ExecWait '$\"$PowerShell$\" -NoProfile -STA -ExecutionPolicy Bypass -WindowStyle Hidden -File $\"$INSTDIR\Uninstall-Managed.ps1$\" -Root $\"$INSTDIR$\"' $Result
  ${If} ${Errors}
    MessageBox MB_OK|MB_ICONSTOP "The uninstall helper could not start. No program files were removed."
    Abort
  ${EndIf}
  ${If} $Result != 0
    Abort
  ${EndIf}
  Delete "$DESKTOP\Home Library.lnk"
  Delete "$SMSTARTUP\Home Library.lnk"
  Delete "$DESKTOP\Carribrary.lnk"
  Delete "$SMSTARTUP\Carribrary.lnk"
  RMDir /r "$SMPROGRAMS\Carribrary"
  RMDir /r /REBOOTOK "$INSTDIR\server"
  RMDir /r /REBOOTOK "$INSTDIR\tray"
  RMDir /r /REBOOTOK "$INSTDIR\public-proxy"
  RMDir /r "$INSTDIR\licenses"
  Delete "$INSTDIR\Connection-Setup.ps1"
  Delete "$INSTDIR\Administrator-Access.ps1"
  Delete "$INSTDIR\Setup-and-Updates.url"
  Delete "$INSTDIR\Repair-DataPermissions.ps1"
  Delete "$INSTDIR\Collect-Diagnostics.ps1"
  Delete "$INSTDIR\Recovery.Core.ps1"
  Delete "$INSTDIR\Recovery.Windows.ps1"
  Delete "$INSTDIR\Recover-Previous.ps1"
  Delete "$INSTDIR\Complete-Backup-Recovery.ps1"
  DeleteRegKey HKCR "carribrary-recovery"
  Delete "$INSTDIR\release-files.json"
  Delete "$INSTDIR\Configure-DuckDNS.ps1"
  Delete "$INSTDIR\Update-DuckDNS.ps1"
  Delete "$INSTDIR\Configure-Remote.ps1"
  Delete "$INSTDIR\Setup-PublicCatalog.ps1"
  Delete "$INSTDIR\Install-Managed.ps1"
  Delete "$INSTDIR\Uninstall-Managed.ps1"
  Delete "$INSTDIR\Open-Library.url"
  Delete "$INSTDIR\Caddyfile.example"
  Delete "$INSTDIR\README.md"
  Delete "$INSTDIR\ADVANCED-OPERATIONS.md"
  Delete "$INSTDIR\INSTALLER-GUIDE.txt"
  Delete "$INSTDIR\Carribrary.ico"
  Delete "$INSTDIR\HomeLibrary.ico"
  Delete "$INSTDIR\START-HERE.txt"
  Delete "$INSTDIR\THIRD-PARTY-NOTICES.txt"
  Delete "$INSTDIR\LICENSE.txt"
  Delete /REBOOTOK "$INSTDIR\Uninstall.exe"
  RMDir "$INSTDIR"
  ; Retain private data-location and recovery manifest seals for a later reinstall.
  DeleteRegValue HKLM "Software\HomeLibraryNative" "InstallDir"
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\HomeLibraryNative"
  DetailPrint "Program removed. Your library, backups and phone keys were retained in ProgramData."
SectionEnd
