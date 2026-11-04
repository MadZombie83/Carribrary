namespace HomeLibrary;

// Keep complete-data restoration separate from program-only rollback. Format-7
// archives from released versions are migrated by the current library reader;
// future versions remain blocked until explicitly supported and tested.
public static class RecoveryCompatibility {
 public static bool CanRestore(BackupVerificationReport report) {
  if(!report.Verified||!report.SupportedLibraryFormat)return false;
  var platform=OperatingSystem.IsWindows()?"Windows":"Linux";
  if(!report.Portable&&report.Platform!=platform)return false;
  var text=report.Version.Split(['-','+'],2)[0];
  if(!Version.TryParse(text,out var version)||version.Major!=2||version.Minor!=0||version.Revision>0)return false;
  var patch=version.Build;
  return report.LibraryFormat switch {
   4=>patch is 43 or 44,
   5=>patch is 45 or 46,
   6=>patch>=47&&patch<=52,
   7=>patch>=53&&patch<=60,
   _=>false
  };
 }
}
