namespace AppLocker.Presentation.ViewModels;

public interface IDialogCloseNotifier
{
    Action<bool>? RequestClose { get; set; }
}
