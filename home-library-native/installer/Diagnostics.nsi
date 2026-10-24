Unicode true
!include "x64.nsh"
!include "LogicLib.nsh"
Name "Carribrary Diagnostics"
OutFile "../../Carribrary-Diagnostics.exe"
RequestExecutionLevel admin
SilentInstall silent
ShowInstDetails nevershow
SetCompressor lzma
Section
  ${IfNot} ${RunningX64}
    MessageBox MB_OK "This tool requires 64-bit Windows."
    Quit
  ${EndIf}
  InitPluginsDir
  SetOutPath "$PLUGINSDIR"
  File "Collect-Diagnostics.ps1"
  ExecWait '$\"$WINDIR\Sysnative\WindowsPowerShell\v1.0\powershell.exe$\" -NoProfile -STA -ExecutionPolicy Bypass -WindowStyle Hidden -File $\"$PLUGINSDIR\Collect-Diagnostics.ps1$\"' $0
  ${If} $0 != 0
    MessageBox MB_OK "The report could not be collected. Please send a screenshot of any error shown."
  ${EndIf}
SectionEnd
