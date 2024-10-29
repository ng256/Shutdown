using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

class Program
{
    const uint TokenAdjustPrivileges = 0x0020;
    const uint TokenQuery = 0x0008;
    const uint SePrivilegeEnabled = 0x0002;
    const string SeShutdownName = "SeShutdownPrivilege";

    [Flags]
    public enum ExitFlags : uint
    {
        Shutdown = 0x0001,
        Reboot = 0x0002,
        RestartApps = 0x0040,
        Logoff = 0x0000,
        HybridShutdown = 0x0020
    }

    [StructLayout(LayoutKind.Sequential)]
    struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct TokenPrivileges
    {
        public uint PrivilegeCount;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public LUID[] Privileges;

        public TokenPrivileges(uint privilegeCount)
        {
            PrivilegeCount = privilegeCount;
            Privileges = new LUID[1];
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool MessageBox(IntPtr hWnd, string lpText, string lpCaption, uint uType);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool AdjustTokenPrivileges(IntPtr hToken, bool disableAllPrivileges, ref TokenPrivileges newPrivileges, uint sizeOfPreviousPrivileges, IntPtr previousPrivileges, uint sizeOfTokenInformation);

    [DllImport("kernel32.dll")]
    static extern IntPtr GetCurrentProcess();

    [DllImport("advapi32.dll")]
    static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

    [DllImport("advapi32.dll")]
    static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

    [DllImport("user32.dll")]
    static extern uint ExitWindowsEx(ExitFlags uFlags, uint dwReason);

    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            MessageBox.Show("No command line argument specified.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }

        if (args.Length > 1)
        {
            MessageBox.Show("Invalid number of command line arguments specified.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }

        char cmdArg = args[0].ToLower();

        if (cmdArg.Length > 1)
        {
            MessageBox.Show("Invalid command line argument specified.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }

        ExitFlags exitFlags;

        switch (cmdArg[0])
        {
            case 's':
                exitFlags = ExitFlags.Shutdown;
                break;
            case 'r':
                exitFlags = ExitFlags.Reboot;
                break;
            case 'a':
                exitFlags = ExitFlags.RestartApps;
                break;
            case "l":
                exitFlags = ExitFlags.Logoff;
                break;
            case 'h':
                exitFlags = ExitFlags.HybridShutdown;
                break;
            default:
                MessageBox.Show("Invalid command line argument specified.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
        }

        IntPtr hToken;

        if (!OpenProcessToken(GetCurrentProcess(), TokenAdjustPrivileges | TokenQuery, out hToken))
        {
            MessageBox.Show("Failed to access process token.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }

        LUID luid;
		
        if (!LookupPrivilegeValue(null, SeShutdownName, out luid))
        {
            MessageBox.Show("Failed to lookup privilege value.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }

        TokenPrivileges tkp = new TokenPrivileges(1);
        tkp.Privileges[0] = luid;
        tkp.Privileges[0].HighPart = SePrivilegeEnabled;

        if (!AdjustTokenPrivileges(hToken, false, ref tkp, 0, IntPtr.Zero, 0))
        {
            MessageBox.Show("Failed to adjust token privileges.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }

        if (ExitWindowsEx(exitFlags, 0) == 0)
        {
            MessageBox.Show("Shutdown cannot be initiated.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
        
        return 0;
    }
}
