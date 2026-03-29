using FluentAssertions;
using AppLocker.Application.Services;

namespace AppLocker.Tests.Application;

/// <summary>
/// TDD Red: Tests cho IPC Watchdog ping/pong logic TRƯỚC KHI implement.
/// Trong WPF app, WatchdogClientService gửi heartbeat tới Watchdog.exe.
/// </summary>
public class WatchdogClientServiceTests
{
    [Fact]
    public void Initialize_ShouldStartHeartbeatLoop()
    {
        var service = new WatchdogClientService();
        service.IsRunning.Should().BeFalse();

        service.Start();

        service.IsRunning.Should().BeTrue();
    }

    [Fact]
    public void Stop_ShouldStopHeartbeatLoop()
    {
        var service = new WatchdogClientService();
        service.Start();
        service.Stop();

        service.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void NotifyWatchdog_ShouldSendTimestamp()
    {
        var service = new WatchdogClientService();

        var action = () => service.SendHeartbeat();

        action.Should().NotThrow(); // Sẽ được kiểm tra chi tiết khi implement NamedPipes
    }
}
