using System.ComponentModel;
using System.Windows;

namespace AppLocker.Presentation.Views;

public partial class MainWindow : Window
{
    private bool _forceShutdown;

    public MainWindow() => InitializeComponent();

    /// <summary>Đóng thật sự (từ menu khay hệ thống). Bình thường bấm X chỉ ẩn xuống tray.</summary>
    public void ForceShutdown()
    {
        _forceShutdown = true;
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_forceShutdown)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnClosing(e);
    }
}
