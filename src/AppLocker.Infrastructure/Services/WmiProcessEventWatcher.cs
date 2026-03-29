using AppLocker.Domain.Interfaces;
using AppLocker.Domain.Models;
using System.Management;

namespace AppLocker.Infrastructure.Services;

/// <summary>
/// Sử dụng System.Management (WMI) để bắt sự kiện khi Process Start (Win32_ProcessStartTrace).
/// </summary>
public class WmiProcessEventWatcher : IProcessEventWatcher
{
    public event EventHandler<ProcessInfo>? ProcessStarted;

    private ManagementEventWatcher? _watcher;

    public void StartListening()
    {
        if (_watcher is not null) return;

        // Bắt sự kiện tạo tiến trình mới trong vòng 1 giây
        var query = new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace");
        _watcher = new ManagementEventWatcher(query);
        _watcher.EventArrived += Watcher_EventArrived;
        _watcher.Start();
    }

    private void Watcher_EventArrived(object sender, EventArrivedEventArgs e)
    {
        try
        {
            var processName = e.NewEvent.Properties["ProcessName"]?.Value?.ToString() ?? string.Empty;
            var processIdObj = e.NewEvent.Properties["ProcessID"]?.Value;

            if (string.IsNullOrEmpty(processName)) return;

            // Bỏ đuôi .exe cho đồng bộ với rule đánh giá
            var normalizedName = processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ? processName[..^4]
                : processName;

            var processInfo = new ProcessInfo
            {
                Name = normalizedName,
                ProcessId = processIdObj != null ? Convert.ToInt32(processIdObj) : 0,
                StartTime = DateTime.Now
            };

            ProcessStarted?.Invoke(this, processInfo);
        }
        catch
        {
            // Bỏ qua lỗi parse
        }
    }

    public void StopListening()
    {
        if (_watcher is null) return;

        _watcher.Stop();
        _watcher.EventArrived -= Watcher_EventArrived;
        _watcher.Dispose();
        _watcher = null;
    }

    public void Dispose()
    {
        StopListening();
        GC.SuppressFinalize(this);
    }
}
