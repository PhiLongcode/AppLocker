namespace AppLocker.Presentation.Models;

/// <summary>
/// Một ứng dụng đã cài (từ Registry), dùng để tham chiếu khi cấu hình rule theo tên process.
/// </summary>
public sealed class InstalledAppItem
{
    public string DisplayName { get; init; } = string.Empty;
    public string ProcessName { get; init; } = string.Empty;
    public string? Publisher { get; init; }
    public string? ExecutablePath { get; init; }
}
