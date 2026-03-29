using System.Windows;

namespace AppLocker.Presentation;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var mw = new Views.MainWindow();
        mw.Show();
    }
}
