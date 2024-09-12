#include <windows.h>
#include <winuser.h>
#include <cwchar>

#define ERR_T L"Error"
#define MB_OK_ERROR (MB_OK | MB_ICONERROR)

int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPWSTR szCmdLine, int iCmdShow)
{
    UINT ewxFlags = 0;
    HANDLE hToken;
    TOKEN_PRIVILEGES tkp;

    if (szCmdLine == NULL || *szCmdLine == L'\0')
    {
        MessageBox(NULL, L"No command line argument specified.", ERR_T, MB_OK_ERROR);
        return 1;
    }

    size_t len = wcslen(szCmdLine);
    wchar_t* szCmdLower = static_cast<wchar_t*>(malloc((len + 1) * sizeof(wchar_t)));
    if (!szCmdLower) {
        MessageBox(NULL, L"Memory allocation failed.", ERR_T, MB_OK_ERROR);
        return 5;
    }
    wcscpy_s(szCmdLower, len + 1, szCmdLine);
    for (size_t i = 0; i < len; i++)
        szCmdLower[i] = towlower(szCmdLower[i]);

    if (len == 1)
    {
        switch (szCmdLower[0])
        {
        case L's':
            ewxFlags = EWX_SHUTDOWN;
            break;
        case L'r':
            ewxFlags = EWX_REBOOT;
            break;
        case L'a':
            ewxFlags = EWX_RESTARTAPPS;
            break;
        case L'l':
            ewxFlags = EWX_LOGOFF;
            break;
        case L'h':
            ewxFlags = EWX_HYBRID_SHUTDOWN;
            break;
        default:
            MessageBox(NULL, L"Invalid command line argument.", ERR_T, MB_OK_ERROR);
            free(szCmdLower);
            return 2;
        }
    }
    else
    {
        MessageBox(NULL, L"Invalid command line argument.", ERR_T, MB_OK_ERROR);
        free(szCmdLower);
        return 2;
    }

    free(szCmdLower);

    if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, &hToken))
    {
        MessageBox(NULL, L"Failed to access process token.", ERR_T, MB_OK_ERROR);
        return 3;
    }

    if (!LookupPrivilegeValue(NULL, SE_SHUTDOWN_NAME, &tkp.Privileges[0].Luid)) {
        MessageBox(NULL, L"Failed to lookup privilege value.", ERR_T, MB_OK_ERROR);
        return 3;
    }
    tkp.PrivilegeCount = 1;
    tkp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;

    AdjustTokenPrivileges(hToken, FALSE, &tkp, 0, (PTOKEN_PRIVILEGES)nullptr, 0);
    if (GetLastError() != ERROR_SUCCESS) {
        MessageBox(NULL, L"Failed to adjust token privileges.", ERR_T, MB_OK_ERROR);
        return 3;
    }

    if (ExitWindowsEx(ewxFlags, 0) == 0)
    {
        MessageBox(NULL, L"Shutdown cannot be initiated.", ERR_T, MB_OK_ERROR);
        return 4;
    }

    return 0;
}
