using AppLocker.Application.Services;
using AppLocker.Infrastructure.Services.Ipc;
using Microsoft.Extensions.Hosting;

namespace AppLocker.Service;

/// <summary>
/// Background worker process quản lý EventBasedMonitorService và IpcServer.
/// Chạy nền dạng Windows Service.
/// </summary>
public class Worker : BackgroundService
{
    private readonly EventBasedMonitorService _monitorService;
    private readonly NamedPipeIpcServer _ipcServer;

    public Worker(EventBasedMonitorService monitorService)
    {
        _monitorService = monitorService;
        _ipcServer = new NamedPipeIpcServer();

        _ipcServer.OnMessageReceived = HandleIpcRequest;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("[Service] Starting AppLocker Core Engine...");
        
        // Chạy engine chặn app
        _monitorService.Start();
        
        // Bật IPC server lắng nghe lệnh từ UI
        _ipcServer.Start();

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(5000, stoppingToken);
            // Có thể thêm ghi log heartbeat tại đây (tương tự watchdog)
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("[Service] Stopping AppLocker Core Engine...");
        _monitorService.Stop();
        _ipcServer.Stop();

        await base.StopAsync(cancellationToken);
    }

    private string HandleIpcRequest(string request)
    {
        return request switch
        {
            "PING" => "PONG",
            "RELOAD_RULES" => ReloadRules(),
            "STATUS" => _monitorService.IsRunning ? "RUNNING" : "STOPPED",
            _ => "UNKNOWN_COMMAND"
        };
    }

    private string ReloadRules()
    {
        // Ở thực tế sẽ trigger RuleEngineService load lại từ DB
        return "RULES_RELOADED";
    }
}
