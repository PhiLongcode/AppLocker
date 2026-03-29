using Microsoft.Win32;

namespace AppLocker.Infrastructure.Services;

/// <summary>
/// Quản lý khởi động cùng Windows (Auto Start) thông qua Registry.
/// </summary>
public class StartupService
{
    private const string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private readonly string _appName;

    public StartupService(string appName = "AppLocker")
    {
        _appName = appName;
    }

    /// <summary>Đăng ký AppLocker khởi động cùng Windows.</summary>
    public void Register(string executablePath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, writable: true);
        key?.SetValue(_appName, $"\"{executablePath}\"");
    }

    /// <summary>Hủy đăng ký khởi động cùng Windows.</summary>
    public void Unregister()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, writable: true);
        key?.DeleteValue(_appName, throwOnMissingValue: false);
    }

    /// <summary>Kiểm tra xem AppLocker có đang được đăng ký khởi động không.</summary>
    public bool IsRegistered()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, writable: false);
        return key?.GetValue(_appName) is string;
    }
}
