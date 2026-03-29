using AppLocker.Application.Services;
using AppLocker.Domain.Interfaces;
using AppLocker.Domain.Models;
using Moq;

namespace AppLocker.Tests.Application;

/// <summary>
/// TDD Red: Tests cho EventBasedMonitorService TRƯỚC KHI implement.
/// Thay vì polling loop, nó lắng nghe event từ IProcessEventWatcher.
/// </summary>
public class EventBasedMonitorServiceTests
{
    private readonly Mock<IProcessEventWatcher> _watcherMock = new();
    private readonly Mock<IRuleEvaluator> _evaluatorMock = new();
    private readonly Mock<IEnforcementService> _enforcementMock = new();

    private EventBasedMonitorService CreateService() =>
        new(_watcherMock.Object, _evaluatorMock.Object, _enforcementMock.Object);

    [Fact]
    public void Start_ShouldCallWatcherStartListening()
    {
        var service = CreateService();
        service.Start();

        _watcherMock.Verify(w => w.StartListening(), Times.Once);
    }

    [Fact]
    public void Stop_ShouldCallWatcherStopListening()
    {
        var service = CreateService();
        service.Start();
        service.Stop();

        _watcherMock.Verify(w => w.StopListening(), Times.Once);
    }

    [Fact]
    public void OnProcessStarted_WhenViolatingRule_ShouldCallKill()
    {
        var service = CreateService();
        service.Start();

        var processArgs = new ProcessInfo { Name = "chrome", ProcessId = 1234 };
        _evaluatorMock.Setup(e => e.Evaluate(processArgs))
            .Returns(new RuleResult { ShouldBlock = true, ProcessName = "chrome" });

        // Simulate event firing from OS WMI watcher
        _watcherMock.Raise(w => w.ProcessStarted += null, this, processArgs);

        _enforcementMock.Verify(e => e.Kill("chrome"), Times.Once);
    }

    [Fact]
    public void OnProcessStarted_WhenNotViolating_ShouldNotCallKill()
    {
        var service = CreateService();
        service.Start();

        var processArgs = new ProcessInfo { Name = "notepad" };
        _evaluatorMock.Setup(e => e.Evaluate(processArgs))
            .Returns(new RuleResult { ShouldBlock = false });

        _watcherMock.Raise(w => w.ProcessStarted += null, this, processArgs);

        _enforcementMock.Verify(e => e.Kill(It.IsAny<string>()), Times.Never);
    }
}
