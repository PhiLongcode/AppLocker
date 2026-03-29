namespace AppLocker.Application.Services;

/// <summary>
/// Hiển thị hộp thoại nhập mật khẩu khi rule PasswordLock kích hoạt (triển khai ở UI layer).
/// </summary>
public interface IPasswordLockPresenter
{
    /// <summary>True nếu người dùng nhập đúng mật khẩu master.</summary>
    bool PromptUnlock(string protectedProcessName, string? executablePath);
}
