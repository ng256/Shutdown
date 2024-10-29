import ctypes
import sys

# Constants
TOKEN_ADJUST_PRIVILEGES = 0x0020
TOKEN_QUERY = 0x0008
SE_PRIVILEGE_ENABLED = 0x0002
EWX_SHUTDOWN = 0x00000001
SE_SHUTDOWN_NAME = "SeShutdownPrivilege"

# Structure for token privileges
class TOKEN_PRIVILEGES(ctypes.Structure):
    _fields_ = [("PrivilegeCount", ctypes.c_ulong),
                ("Privileges", ctypes.c_void_p)]

# Function to enable shutdown privileges
def enable_shutdown_privilege():
    # Open the process token
    hToken = ctypes.c_void_p()
    if not ctypes.windll.advapi32.OpenProcessToken(
            ctypes.windll.kernel32.GetCurrentProcess(),
            TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY,
            ctypes.byref(hToken)):
        print("Failed to access process token.")
        return False

    # Lookup privilege value
    luid = ctypes.c_ulonglong()
    if not ctypes.windll.advapi32.LookupPrivilegeValueW(None, SE_SHUTDOWN_NAME, ctypes.byref(luid)):
        print("Failed to lookup privilege value.")
        return False

    # Prepare token privileges structure
    tkp = TOKEN_PRIVILEGES()
    tkp.PrivilegeCount = 1
    tkp.Privileges = ctypes.pointer(luid)

    # Enable the privilege
    tkp.Privileges[0] = ctypes.c_ulonglong(luid.value | SE_PRIVILEGE_ENABLED)
    ctypes.windll.advapi32.AdjustTokenPrivileges(hToken, False, ctypes.byref(tkp), 0, None, None)

    # Check if the privilege was adjusted successfully
    if ctypes.windll.kernel32.GetLastError() != 0:
        print("Failed to adjust token privileges.")
        return False

    return True

# Entry point
if __name__ == "__main__":
    if enable_shutdown_privilege():
        # Initiate shutdown
        if ctypes.windll.user32.ExitWindowsEx(EWX_SHUTDOWN, 0) == 0:
            print("Shutdown cannot be initiated.")
            sys.exit(1)
