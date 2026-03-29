namespace AppLocker.Domain.Models;

/// <summary>
/// Kết quả đánh giá rule cho một tiến trình.
/// </summary>
public class RuleResult
{
    public bool ShouldBlock { get; set; }
    public string? Reason { get; set; }
    public string ProcessName { get; set; } = string.Empty;
}
