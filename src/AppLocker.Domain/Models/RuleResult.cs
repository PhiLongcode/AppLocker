namespace AppLocker.Domain.Models;

/// <summary>
/// Kết quả đánh giá rule cho một tiến trình.
/// </summary>
public class RuleResult
{
    public bool ShouldBlock { get; set; }
    public string? Reason { get; set; }
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>Rule PasswordLock: cần hộp thoại nhập mật khẩu rồi có thể mở lại tiến trình.</summary>
    public bool RequiresPasswordUnlock { get; set; }

    /// <summary>Đường dẫn file .exe khi có (để chạy lại sau khi mở khóa).</summary>
    public string? ExecutablePath { get; set; }
}
