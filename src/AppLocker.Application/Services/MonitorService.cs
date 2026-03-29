using AppLocker.Domain.Interfaces;
using AppLocker.Domain.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace AppLocker.Application.Services;

/// <summary>
/// Service điều phối: quét tiến trình → đánh giá rule → thực thi lệnh kill (hoặc mở khóa bằng mật khẩu).
/// </summary>
public class MonitorService
{
    private static readonly TimeSpan UnlockDuration = TimeSpan.FromMinutes(30);

    private readonly IProcessService _processService;
    private readonly IRuleEvaluator _ruleEvaluator;
    private readonly IEnforcementService _enforcement;
    private readonly IPasswordLockPresenter? _passwordLockPresenter;

    private readonly ConcurrentDictionary<string, DateTimeOffset> _temporaryUnlockUntil = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _dialogLock = new();
    private volatile bool _passwordDialogOpen;

    private CancellationTokenSource? _cts;

    public MonitorService(
        IProcessService processService,
        IRuleEvaluator ruleEvaluator,
        IEnforcementService enforcement,
        IPasswordLockPresenter? passwordLockPresenter = null)
    {
        _processService = processService;
        _ruleEvaluator = ruleEvaluator;
        _enforcement = enforcement;
        _passwordLockPresenter = passwordLockPresenter;
    }

    public bool IsRunning { get; private set; }

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
                Console.Error.WriteLine($"[MonitorService] Error: {ex.Message}");
            }
        }

        IsRunning = false;
    }

    public void Stop() => _cts?.Cancel();

    public void Check()
    {
        PruneExpiredUnlocks();
        var processes = _processService.GetRunningProcesses();

        foreach (var process in processes)
        {
            if (IsTemporarilyUnlocked(process.Name))
                continue;

            var result = _ruleEvaluator.Evaluate(process);

            if (!result.ShouldBlock)
                continue;

            if (result.RequiresPasswordUnlock && _passwordLockPresenter is not null)
                HandlePasswordLock(process, result);
            else
                _enforcement.Kill(process.Name);
        }
    }

    private void PruneExpiredUnlocks()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var kv in _temporaryUnlockUntil.ToArray())
        {
            if (kv.Value <= now)
                _temporaryUnlockUntil.TryRemove(kv.Key, out _);
        }
    }

    private bool IsTemporarilyUnlocked(string processName) =>
        _temporaryUnlockUntil.TryGetValue(processName, out var until) && until > DateTimeOffset.UtcNow;

    private void GrantUnlock(string processName)
    {
        _temporaryUnlockUntil[processName] = DateTimeOffset.UtcNow.Add(UnlockDuration);
    }

    private void HandlePasswordLock(ProcessInfo process, RuleResult result)
    {
        _enforcement.Kill(process.Name);
        Thread.Sleep(150);

        lock (_dialogLock)
        {
            if (_passwordDialogOpen)
                return;
            _passwordDialogOpen = true;
        }

        try
        {
            var exePath = result.ExecutablePath;
            if (string.IsNullOrWhiteSpace(exePath))
                exePath = process.FullPath;

            var ok = _passwordLockPresenter!.PromptUnlock(process.Name, exePath);
            if (ok)
            {
                GrantUnlock(process.Name);
                TryRelaunch(exePath);
            }
        }
        finally
        {
            lock (_dialogLock)
                _passwordDialogOpen = false;
        }
    }

    private static void TryRelaunch(string? exePath)
    {
        if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[MonitorService] Relaunch failed: {ex.Message}");
        }
    }
}
