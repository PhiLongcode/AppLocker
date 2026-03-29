using AppLocker.Application.Services;
using AppLocker.Domain.Entities;
using AppLocker.Domain.Interfaces;
using AppLocker.Domain.Models;
using FluentAssertions;
using Moq;

namespace AppLocker.Tests.Application;

/// <summary>
/// Unit Tests cho RuleEngineService (sprint-0.3).
/// </summary>
public class RuleEngineServiceTests
{
    private readonly RuleEngineService _service = new();

    [Fact]
    public void AddRule_ShouldAppendToRulesList()
    {
        var rule = new AppRule { ProcessName = "chrome", Type = RuleType.Block };

        _service.AddRule(rule);

        _service.Rules.Should().HaveCount(1);
        _service.Rules[0].ProcessName.Should().Be("chrome");
    }

    [Fact]
    public void ClearRules_ShouldRemoveAll()
    {
        _service.AddRule(new AppRule { ProcessName = "a", Type = RuleType.Block });
        _service.AddRule(new AppRule { ProcessName = "b", Type = RuleType.Block });

        _service.ClearRules();

        _service.Rules.Should().BeEmpty();
    }

    [Fact]
    public void RemoveRule_ByProcessName_ShouldDeleteFromList()
    {
        _service.AddRule(new AppRule { ProcessName = "chrome", Type = RuleType.Block });
        _service.AddRule(new AppRule { ProcessName = "notepad", Type = RuleType.Block });

        _service.RemoveRule("chrome");

        _service.Rules.Should().HaveCount(1);
        _service.Rules[0].ProcessName.Should().Be("notepad");
    }

    [Fact]
    public void RemoveRule_CaseInsensitive_ShouldWork()
    {
        _service.AddRule(new AppRule { ProcessName = "CHROME", Type = RuleType.Block });

        _service.RemoveRule("chrome");

        _service.Rules.Should().BeEmpty();
    }

    [Fact]
    public void ToggleRule_Enable_ShouldSetIsEnabledTrue()
    {
        _service.AddRule(new AppRule { ProcessName = "chrome", Type = RuleType.Block, IsEnabled = false });

        _service.ToggleRule("chrome", true);

        _service.Rules[0].IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void ToggleRule_Disable_ShouldSetIsEnabledFalse()
    {
        _service.AddRule(new AppRule { ProcessName = "chrome", Type = RuleType.Block, IsEnabled = true });

        _service.ToggleRule("chrome", false);

        _service.Rules[0].IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void GetActiveRules_ShouldReturnOnlyEnabledRules()
    {
        _service.AddRule(new AppRule { ProcessName = "chrome", Type = RuleType.Block, IsEnabled = true });
        _service.AddRule(new AppRule { ProcessName = "notepad", Type = RuleType.Block, IsEnabled = false });

        var active = _service.GetActiveRules().ToList();

        active.Should().HaveCount(1);
        active[0].ProcessName.Should().Be("chrome");
    }

    [Fact]
    public void Rules_InitiallyEmpty()
    {
        _service.Rules.Should().BeEmpty();
    }
}

/// <summary>
/// Unit Tests cho MonitorService (sprint-0.3) - sử dụng Moq để mock dependencies.
/// </summary>
public class MonitorServiceTests
{
    private readonly Mock<IProcessService> _processServiceMock = new();
    private readonly Mock<IRuleEvaluator> _ruleEvaluatorMock = new();
    private readonly Mock<IEnforcementService> _enforcementMock = new();

    private MonitorService CreateMonitor() =>
        new(_processServiceMock.Object, _ruleEvaluatorMock.Object, _enforcementMock.Object);

    private static ProcessInfo MakeProcess(string name) =>
        new() { ProcessId = 1, Name = name };

    [Fact]
    public void Check_WhenProcessShouldBlock_ShouldCallEnforcementKill()
    {
        // Arrange
        var process = MakeProcess("chrome");
        _processServiceMock
            .Setup(s => s.GetRunningProcesses())
            .Returns(new[] { process });
        _ruleEvaluatorMock
            .Setup(e => e.Evaluate(process))
            .Returns(new RuleResult { ShouldBlock = true, ProcessName = "chrome" });

        var monitor = CreateMonitor();

        // Act
        monitor.Check();

        // Assert
        _enforcementMock.Verify(e => e.Kill("chrome"), Times.Once);
    }

    [Fact]
    public void Check_WhenProcessShouldNotBlock_ShouldNotCallKill()
    {
        var process = MakeProcess("notepad");
        _processServiceMock
            .Setup(s => s.GetRunningProcesses())
            .Returns(new[] { process });
        _ruleEvaluatorMock
            .Setup(e => e.Evaluate(process))
            .Returns(new RuleResult { ShouldBlock = false, ProcessName = "notepad" });

        var monitor = CreateMonitor();
        monitor.Check();

        _enforcementMock.Verify(e => e.Kill(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Check_WithMultipleProcesses_ShouldOnlyKillViolators()
    {
        var chrome = MakeProcess("chrome");
        var notepad = MakeProcess("notepad");

        _processServiceMock
            .Setup(s => s.GetRunningProcesses())
            .Returns(new[] { chrome, notepad });

        _ruleEvaluatorMock
            .Setup(e => e.Evaluate(chrome))
            .Returns(new RuleResult { ShouldBlock = true, ProcessName = "chrome" });
        _ruleEvaluatorMock
            .Setup(e => e.Evaluate(notepad))
            .Returns(new RuleResult { ShouldBlock = false, ProcessName = "notepad" });

        var monitor = CreateMonitor();
        monitor.Check();

        _enforcementMock.Verify(e => e.Kill("chrome"), Times.Once);
        _enforcementMock.Verify(e => e.Kill("notepad"), Times.Never);
    }

    [Fact]
    public void Check_WhenNoProcessesRunning_ShouldNotCallKill()
    {
        _processServiceMock
            .Setup(s => s.GetRunningProcesses())
            .Returns(Enumerable.Empty<ProcessInfo>());

        var monitor = CreateMonitor();
        monitor.Check();

        _enforcementMock.Verify(e => e.Kill(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Check_WhenPasswordLock_ShouldKillAndInvokePresenter()
    {
        var process = new ProcessInfo { Name = "chrome", ProcessId = 1, FullPath = @"C:\chrome.exe" };
        _processServiceMock
            .Setup(s => s.GetRunningProcesses())
            .Returns(new[] { process });
        _ruleEvaluatorMock
            .Setup(e => e.Evaluate(process))
            .Returns(new RuleResult
            {
                ShouldBlock = true,
                RequiresPasswordUnlock = true,
                ProcessName = "chrome",
                ExecutablePath = @"C:\chrome.exe"
            });
        var presenterMock = new Mock<IPasswordLockPresenter>();
        presenterMock
            .Setup(p => p.PromptUnlock("chrome", @"C:\chrome.exe"))
            .Returns(false);

        var monitor = new MonitorService(
            _processServiceMock.Object,
            _ruleEvaluatorMock.Object,
            _enforcementMock.Object,
            presenterMock.Object);
        monitor.Check();

        _enforcementMock.Verify(e => e.Kill("chrome"), Times.AtLeastOnce);
        presenterMock.Verify(p => p.PromptUnlock("chrome", @"C:\chrome.exe"), Times.Once);
    }

    [Fact]
    public void IsRunning_InitiallyFalse()
    {
        var monitor = CreateMonitor();
        monitor.IsRunning.Should().BeFalse();
    }
}
