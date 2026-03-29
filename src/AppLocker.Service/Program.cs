using AppLocker.Application.Services;
using AppLocker.Domain.Interfaces;
using AppLocker.Infrastructure.Services;
using AppLocker.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Cấu hình chạy như một Windows Service
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "AppLocker Core Engine";
});

// Đăng ký Dependency Injection cho toàn bộ Clean Architecture
builder.Services.AddSingleton<IProcessEventWatcher, WmiProcessEventWatcher>();
builder.Services.AddSingleton<IRuleEvaluator>(sp => 
{
    // Cần phải nạp rule thực tế từ SqliteStorageService
    return new RuleEvaluator(Array.Empty<AppLocker.Domain.Entities.AppRule>());
});
builder.Services.AddSingleton<IEnforcementService, EnforcementService>();
builder.Services.AddSingleton<EventBasedMonitorService>();

// Register Worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
