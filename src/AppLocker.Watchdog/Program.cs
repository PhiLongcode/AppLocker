using System.Diagnostics;

namespace AppLocker.Watchdog;

/// <summary>
/// Chạy nền và giám sát file heartbeat AppLocker.Heartbeat.lock.
/// Nếu file không được cập nhật > 10s => AppLocker đã bị kill/crash => Khởi động lại.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        var tempDir = Path.GetTempPath();
        var lockFile = Path.Combine(tempDir, "AppLocker.Heartbeat.lock");
        var exePath = args.ElementAtOrDefault(0) ?? @"C:\Program Files\AppLocker\AppLocker.exe";

        Console.WriteLine($"[Watchdog] Monitoring {lockFile} for AppLocker...");

        while (true)
        {
            await Task.Delay(5000);

            if (!File.Exists(lockFile)) continue;

            try
            {
                var content = File.ReadAllText(lockFile);
                if (DateTime.TryParse(content, out var lastBeat))
                {
                    var diff = DateTime.UtcNow - lastBeat;
                    if (diff.TotalSeconds > 10)
                    {
                        Console.WriteLine("[Watchdog] AppLocker is DEAD. Restarting...");
                        RestartAppLocker(exePath);
                        // Đợi AppLocker lên tạo lại heartbeat
                        await Task.Delay(10000); 
                    }
                }
            }
            catch
            {
                // File locked error due to concurrency
            }
        }
    }

    static void RestartAppLocker(string exePath)
    {
        try
        {
            if (File.Exists(exePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true,
                    Verb = "runas" // Require admin
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Watchdog] Failed to restart AppLocker: {ex.Message}");
        }
    }
}
