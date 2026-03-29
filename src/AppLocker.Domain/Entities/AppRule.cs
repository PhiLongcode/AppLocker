namespace AppLocker.Domain.Entities;

/// <summary>
/// Đại diện cho một quy tắc chặn/giới hạn ứng dụng.
/// </summary>
public class AppRule
{
    /// <summary>Tên tiến trình (process name) cần áp dụng rule - không cần đuôi .exe</summary>
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>Loại rule: Block hoàn toàn hoặc LimitTime</summary>
    public RuleType Type { get; set; }

    /// <summary>Số phút giới hạn sử dụng (chỉ dùng khi Type = LimitTime)</summary>
    public int? TimeLimitMinutes { get; set; }

    /// <summary>Bật/tắt rule này</summary>
    public bool IsEnabled { get; set; } = true;
}
