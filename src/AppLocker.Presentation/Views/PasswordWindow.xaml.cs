using System.Windows;
using AppLocker.Presentation.ViewModels;

namespace AppLocker.Presentation.Views;

public partial class PasswordWindow : Window
{
    public PasswordWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is IDialogCloseNotifier n)
            {
                n.RequestClose = result =>
                {
                    DialogResult = result;
                    Close();
                };
            }
        };
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
