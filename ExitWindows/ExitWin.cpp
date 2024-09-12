#define ERR_T L"Error"
#include <windows.h>
#include <winuser.h>

int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, const PSTR szCmdLine, int iCmdShow)
{
    UINT ewxFlags = 0;
    HANDLE hToken;
    TOKEN_PRIVILEGES tkp;

    if (szCmdLine == NULL || *szCmdLine == '\0')
    {
        MessageBox(NULL, L"No command line argument specified.", ERR_T, MB_OK);
        return 1;
    }

    char* szCmdLower = static_cast<char*>(malloc(strlen(szCmdLine) + 1));
    strcpy_s(szCmdLower, strlen(szCmdLine), szCmdLine);
    for (int i = 0; i < strlen(szCmdLower); i++)
        szCmdLower[i] = tolower(szCmdLower[i]);

    switch (szCmdLower)
    {
    case L"s":
        ewxFlags = EWX_SHUTDOWN;
        break;
    case L"r":
        ewxFlags = EWX_REBOOT;
        break;
    case L"a":
        ewxFlags = EWX_RESTARTAPPS;
        break;
    case L"l":
        ewxFlags = EWX_LOGOFF;
        break;
    case L"h":
        ewxFlags = EWX_HYBRID_SHUTDOWN;
        break;
    default:
        MessageBox(NULL, L"Invalid command line argument.", ERR_T, MB_OK);
        return 2;
    }

    if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, &hToken)) {
        MessageBox(NULL, L"Failed to access process token.", ERR_T, MB_OK);
        return 3;
    }

    LookupPrivilegeValue(NULL, SE_SHUTDOWN_NAME, &tkp.Privileges[0].Luid);
    tkp.PrivilegeCount = 1;
    tkp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;

    OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, &hToken);
    LookupPrivilegeValue(nullptr, SE_SHUTDOWN_NAME, &tkp.Privileges[0].Luid);
    tkp.PrivilegeCount = 1;
    tkp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;
    AdjustTokenPrivileges(hToken, FALSE, &tkp, 0, (PTOKEN_PRIVILEGES)nullptr, 0);
    if(ExitWindowsEx(ewxFlags, 0) == 0)
    {
        MessageBox(NULL, L"Shutdown cannot be initiated.", ERR_T, MB_OK);
        return 4;
    }

    return 0;
}