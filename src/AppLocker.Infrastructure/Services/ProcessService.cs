using AppLocker.Domain.Interfaces;
using AppLocker.Domain.Models;
using System.Diagnostics;

namespace AppLocker.Infrastructure.Services;

/// <summary>
/// Triển khai IProcessService sử dụng System.Diagnostics để lấy danh sách tiến trình đang chạy.
/// </summary>
public class ProcessService : IProcessService
{
    public IEnumerable<ProcessInfo> GetRunningProcesses()
    {
        var result = new List<ProcessInfo>();

        foreach (var p in Process.GetProcesses())
        {
            try
            {
                result.Add(new ProcessInfo
                {
                    ProcessId = p.Id,
                    Name = p.ProcessName,
                    FullPath = GetSafePath(p),
                    StartTime = GetSafeStartTime(p)
                });
            }
            catch
            {
                // Bỏ qua process không có quyền truy cập
            }
        }

        return result;
    }

    private static string GetSafePath(Process p)
    {
        try { return p.MainModule?.FileName ?? string.Empty; }
        catch { return string.Empty; }
    }

    private static DateTime GetSafeStartTime(Process p)
    {
        try { return p.StartTime; }
        catch { return DateTime.MinValue; }
    }
}
