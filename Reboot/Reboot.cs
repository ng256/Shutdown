using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

class Program
{
    const uint TokenAdjustPrivileges = 0x0020;
    const uint TokenQuery = 0x0008;
    const uint SePrivilegeEnabled = 0x0002;
    const uint RebootFlags = 0x0002;
    const string SeShutdownName = "SeShutdownPrivilege";

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
    static extern uint ExitWindowsEx(uint uFlags, uint dwReason);

    static int Main(string[] args)
    {
        IntPtr hToken;
        LUID luid;

        if (!OpenProcessToken(GetCurrentProcess(), TokenAdjustPrivileges | TokenQuery, out hToken))
        {
            MessageBox.Show("Failed to access process token.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }

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

        if (ExitWindowsEx(RebootFlags, 0) == 0)
        {
            MessageBox.Show("Shutdown cannot be initiated.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
        
        return 0;
    }
}
