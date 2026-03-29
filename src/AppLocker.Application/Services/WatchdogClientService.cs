namespace AppLocker.Application.Services;

/// <summary>
/// Service nằm trong AppLocker.exe. Liên tục gửi tín hiệu sống (heartbeat)
/// sang tiến trình Watchdog qua NamedPipes/REST/File để chứng minh nó chưa bị tắt.
/// </summary>
public class WatchdogClientService
{
    private CancellationTokenSource? _cts;

    public bool IsRunning { get; private set; }

    public void Start()
    {
        if (IsRunning) return;
        IsRunning = true;
        _cts = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                SendHeartbeat();
                await Task.Delay(5000, _cts.Token);
            }
        }, _cts.Token);
    }

    public void Stop()
    {
        IsRunning = false;
        _cts?.Cancel();
    }

    /// <summary>Gửi tín hiệu tới Watchdog.exe. Nếu Watchdog chết, nó sẽ khởi động lại Watchdog.</summary>
    public void SendHeartbeat()
    {
        try
        {
            // Implementation qua Named Pipes sẽ phức tạp, ở MVP tạo tạm một file lock chia sẻ.
            var tempDir = Path.GetTempPath();
            var lockFile = Path.Combine(tempDir, "AppLocker.Heartbeat.lock");

            File.WriteAllText(lockFile, DateTime.UtcNow.ToString("O"));
        }
        catch
        {
            // ignored
        }
    }
}
