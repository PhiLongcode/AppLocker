using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using AppLocker.Application.Services;
using AppLocker.Infrastructure.Storage;
using AppLocker.Presentation.ViewModels;
using AppLocker.Presentation.Views;

namespace AppLocker.Presentation;

public partial class App : System.Windows.Application
{
    private NotifyIcon? _tray;
    private MainViewModel? _viewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var startBackground = e.Args.Contains("--background", StringComparer.OrdinalIgnoreCase);

        var storage = new SqliteStorageService(AppDataPaths.GetDatabasePath());
        var passwordService = new PasswordService(storage);

        if (passwordService.IsPasswordSet)
        {
            var pwdVm = new PasswordViewModel(passwordService);
            var pwdWin = new PasswordWindow
            {
                DataContext = pwdVm,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Owner = null
            };
            if (pwdWin.ShowDialog() != true)
            {
                storage.Dispose();
                Shutdown();
                return;
            }
        }

        var mw = new MainWindow();
        _viewModel = new MainViewModel(storage, passwordService);
        mw.DataContext = _viewModel;
        MainWindow = mw;

        _tray = new NotifyIcon
        {
            Icon = SystemIcons.Shield,
            Visible = true,
            Text = "AppLocker — nhấp đúp để mở"
        };
        _tray.DoubleClick += (_, _) => ShowMain();

        var menu = new ContextMenuStrip();
        menu.Items.Add("Mở AppLocker", null, (_, _) => ShowMain());
        menu.Items.Add("Thoát hoàn toàn", null, (_, _) => ShutdownFromTray());
        _tray.ContextMenuStrip = menu;

        mw.Show();

        if (startBackground)
        {
            mw.Loaded += (_, _) =>
            {
                mw.Hide();
                _viewModel?.StartMonitoring();
                _tray?.ShowBalloonTip(
                    4000,
                    "AppLocker",
                    "Đang chạy nền — giám sát đã bật. Mở từ icon khay hệ thống.",
                    ToolTipIcon.Info);
            };
        }
    }

    private void ShowMain()
    {
        if (MainWindow is not MainWindow mw)
            return;
        mw.Show();
        mw.WindowState = WindowState.Normal;
        mw.Activate();
    }

    private void ShutdownFromTray()
    {
        if (_tray is { } t)
        {
            t.Visible = false;
            t.Dispose();
            _tray = null;
        }

        if (MainWindow is MainWindow mw)
        {
            mw.ForceShutdown();
            return;
        }

        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _viewModel?.Dispose();
        base.OnExit(e);
    }
}
