using AppLocker.Domain.Interfaces;
using AppLocker.Domain.Models;

namespace AppLocker.Application.Services;

/// <summary>
/// Thay thế MonitorService cũ. Sử dụng IProcessEventWatcher (WMI event) 
/// thay vì vòng lặp vô hạn (polling), giúp tiết kiệm tài nguyên CPU.
/// </summary>
public class EventBasedMonitorService : IDisposable
{
    private readonly IProcessEventWatcher _watcher;
    private readonly IRuleEvaluator _evaluator;
    private readonly IEnforcementService _enforcement;

    public bool IsRunning { get; private set; }

    public EventBasedMonitorService(
        IProcessEventWatcher watcher,
        IRuleEvaluator evaluator,
        IEnforcementService enforcement)
    {
        _watcher = watcher;
        _evaluator = evaluator;
        _enforcement = enforcement;

        _watcher.ProcessStarted += OnProcessStarted;
    }

    public void Start()
    {
        if (IsRunning) return;
        _watcher.StartListening();
        IsRunning = true;
    }

    public void Stop()
    {
        if (!IsRunning) return;
        _watcher.StopListening();
        IsRunning = false;
    }

    private void OnProcessStarted(object? sender, ProcessInfo process)
    {
        var result = _evaluator.Evaluate(process);
        if (result.ShouldBlock)
        {
            _enforcement.Kill(process.Name);
        }
    }

    public void Dispose()
    {
        Stop();
        _watcher.ProcessStarted -= OnProcessStarted;
        _watcher.Dispose();
    }
}
