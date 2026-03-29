using AppLocker.Application.Services;
using AppLocker.Presentation.Infrastructure;
using System.Windows.Input;

namespace AppLocker.Presentation.ViewModels;

public class PasswordViewModel : BaseViewModel, IDialogCloseNotifier
{
    private readonly PasswordService _passwordService;
    private string _passwordText = "";
    private string _statusMessage = "";

    public bool IsSettingPassword => !_passwordService.IsPasswordSet;

    public string Title => IsSettingPassword ? "Thiết lập Mật khẩu" : "Nhập Mật khẩu";
    public string PromptText => IsSettingPassword 
        ? "Ứng dụng chưa có mật khẩu bảo vệ. Vui lòng tạo mật khẩu mới để khóa cài đặt:" 
        : "Vui lòng nhập mật khẩu để truy cập cài đặt:";

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

    public PasswordViewModel(PasswordService passwordService)
    {
        _passwordService = passwordService;
        SubmitCommand = new RelayCommand(Submit);
    }

    private void Submit()
    {
        if (string.IsNullOrWhiteSpace(PasswordText))
        {
            StatusMessage = "⚠ Mật khẩu không được để trống!";
            return;
        }

        if (IsSettingPassword)
        {
            _passwordService.SetPassword(PasswordText);
            RequestClose?.Invoke(true); // Thành công
        }
        else
        {
            if (_passwordService.Verify(PasswordText))
            {
                RequestClose?.Invoke(true); // Thành công
            }
            else
            {
                StatusMessage = "❌ Mật khẩu không chính xác!";
                PasswordText = "";
            }
        }
    }
}
