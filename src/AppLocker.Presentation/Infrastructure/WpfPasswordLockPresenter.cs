using AppLocker.Application.Services;
using AppLocker.Presentation.ViewModels;
using System.Windows;
using WpfApp = System.Windows.Application;

namespace AppLocker.Presentation.Infrastructure;

/// <summary>
/// Hiển thị hộp thoại mở khóa trên UI thread (gọi từ MonitorService).
/// </summary>
public sealed class WpfPasswordLockPresenter : IPasswordLockPresenter
{
    private readonly PasswordService _passwordService;

    public WpfPasswordLockPresenter(PasswordService passwordService)
    {
        _passwordService = passwordService;
    }

    public bool PromptUnlock(string protectedProcessName, string? executablePath)
    {
        if (WpfApp.Current?.Dispatcher is null)
            return false;

        return WpfApp.Current.Dispatcher.Invoke(() =>
        {
            var vm = new UnlockViewModel(_passwordService, protectedProcessName);
            var win = new Views.PasswordWindow { DataContext = vm };
            Window? owner = WpfApp.Current.MainWindow;
            if (owner is { IsLoaded: true })
                win.Owner = owner;
            win.WindowStartupLocation = owner is null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner;
            return win.ShowDialog() == true;
        });
    }
}
