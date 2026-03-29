using AppLocker.Domain.Interfaces;

namespace AppLocker.Application.Services;

/// <summary>
/// Service điều phối: quét tiến trình → đánh giá rule → thực thi lệnh kill.
/// Chạy theo vòng lặp bất đồng bộ theo chu kỳ.
/// </summary>
public class MonitorService
{
    private readonly IProcessService _processService;
    private readonly IRuleEvaluator _ruleEvaluator;
    private readonly IEnforcementService _enforcement;

    private CancellationTokenSource? _cts;

    public MonitorService(
        IProcessService processService,
        IRuleEvaluator ruleEvaluator,
        IEnforcementService enforcement)
    {
        _processService = processService;
        _ruleEvaluator = ruleEvaluator;
        _enforcement = enforcement;
    }

    public bool IsRunning { get; private set; }

    /// <summary>Bắt đầu vòng lặp giám sát bất đồng bộ</summary>
    public async Task StartAsync(int intervalMs = 2000)
    {
        if (IsRunning) return;

        _cts = new CancellationTokenSource();
        IsRunning = true;

        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                Check();
                await Task.Delay(intervalMs, _cts.Token);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // Log lỗi nhưng không crash vòng lặp
                Console.Error.WriteLine($"[MonitorService] Error: {ex.Message}");
            }
        }

        IsRunning = false;
    }

    /// <summary>Dừng vòng lặp giám sát</summary>
    public void Stop()
    {
        _cts?.Cancel();
    }

    /// <summary>Thực hiện một lần kiểm tra toàn bộ tiến trình</summary>
    public void Check()
    {
        var processes = _processService.GetRunningProcesses();

        foreach (var process in processes)
        {
            var result = _ruleEvaluator.Evaluate(process);

            if (result.ShouldBlock)
            {
                _enforcement.Kill(process.Name);
            }
        }
    }
}
