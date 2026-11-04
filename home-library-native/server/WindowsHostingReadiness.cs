namespace HomeLibrary;
// A copied data directory must not claim that machine-bound Windows keys and
// firewall consent have already been prepared on its replacement computer.
public static class WindowsHostingReadiness {
 public static string Machine=>OperatingSystem.IsWindows()?(Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography","MachineGuid","") as string??""):"";
 public static bool Ready(string path)=>!OperatingSystem.IsWindows()||File.Exists(path)&&Machine!=""&&File.ReadAllText(path).Trim()==Machine;
 public static void Mark(string path){if(!OperatingSystem.IsWindows())return;if(Machine=="")throw new LibraryError("Windows machine identity could not be checked. Run the hosting assistant again.");File.WriteAllText(path,Machine);}
}
