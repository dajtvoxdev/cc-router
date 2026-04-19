using Microsoft.Win32;

namespace CCRouter.Services;

public static class AutostartService
{
    private const string RegKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "CCRouter";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegKey, false);
        return key?.GetValue(AppName) != null;
    }

    public static void Enable(string exePath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegKey, true)!;
        key.SetValue(AppName, $"\"{exePath}\" --tray");
    }

    public static void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegKey, true)!;
        key.DeleteValue(AppName, throwOnMissingValue: false);
    }
}
