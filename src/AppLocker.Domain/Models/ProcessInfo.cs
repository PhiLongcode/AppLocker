namespace AppLocker.Domain.Models;

/// <summary>
/// Thông tin về một tiến trình đang chạy.
/// </summary>
public class ProcessInfo
{
    public int ProcessId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
}
