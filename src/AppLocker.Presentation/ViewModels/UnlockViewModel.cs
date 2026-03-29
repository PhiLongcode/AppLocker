using AppLocker.Application.Services;
using AppLocker.Presentation.Infrastructure;
using System.Windows.Input;

namespace AppLocker.Presentation.ViewModels;

/// <summary>
/// Hộp thoại mở khóa ứng dụng khi rule PasswordLock chặn tiến trình.
/// </summary>
public class UnlockViewModel : BaseViewModel, IDialogCloseNotifier
{
    private readonly PasswordService _passwordService;
    private string _passwordText = "";
    private string _statusMessage = "";

    public UnlockViewModel(PasswordService passwordService, string protectedProcessName)
    {
        _passwordService = passwordService;
        ProtectedProcessName = protectedProcessName;
        SubmitCommand = new RelayCommand(Submit);
    }

    public string ProtectedProcessName { get; }

    public string Title => $"Ứng dụng được bảo vệ: {ProtectedProcessName}";

    public string PromptText =>
        "Tiến trình đã được tạm dừng. Nhập mật khẩu master AppLocker để mở lại trong khoảng 30 phút.";

    public string PasswordText
    {
        get => _passwordText;
        set => SetProperty(ref _passwordText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand SubmitCommand { get; }
    public Action<bool>? RequestClose { get; set; }

    private void Submit()
    {
        if (!_passwordService.IsPasswordSet)
        {
            StatusMessage = "⚠ Chưa thiết lập mật khẩu — mở Cài đặt và tạo mật khẩu trước.";
            return;
        }

        if (string.IsNullOrWhiteSpace(PasswordText))
        {
            StatusMessage = "⚠ Vui lòng nhập mật khẩu.";
            return;
        }

        if (_passwordService.Verify(PasswordText))
            RequestClose?.Invoke(true);
        else
        {
            StatusMessage = "❌ Mật khẩu không đúng.";
            PasswordText = "";
        }
    }
}
