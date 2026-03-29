using AppLocker.Application.Services;
using AppLocker.Domain.Entities;
using FluentAssertions;

namespace AppLocker.Tests.Application;

/// <summary>
/// TDD Red: Tests cho UsageTrackingService TRƯỚC KHI implement.
/// </summary>
public class UsageTrackingServiceTests
{
    private readonly UsageTrackingService _service = new();

    [Fact]
    public void RecordUsage_FirstTime_ShouldInitializeTracking()
    {
        _service.RecordUsage("chrome", TimeSpan.FromMinutes(5));

        _service.GetUsageToday("chrome").Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void RecordUsage_Cumulative_ShouldAddUp()
    {
        _service.RecordUsage("chrome", TimeSpan.FromMinutes(30));
        _service.RecordUsage("chrome", TimeSpan.FromMinutes(20));

        _service.GetUsageToday("chrome").Should().Be(TimeSpan.FromMinutes(50));
    }

    [Fact]
    public void GetUsageToday_WhenNoTracking_ShouldReturnZero()
    {
        var usage = _service.GetUsageToday("chrome");

        usage.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void IsOverLimit_WhenUsageExceedsLimit_ShouldReturnTrue()
    {
        _service.RecordUsage("chrome", TimeSpan.FromMinutes(121));
        var rule = new AppRule
        {
            ProcessName = "chrome",
            Type = RuleType.LimitTime,
            TimeLimitMinutes = 120,
            IsEnabled = true
        };

        _service.IsOverLimit("chrome", rule).Should().BeTrue();
    }

    [Fact]
    public void IsOverLimit_WhenUsageBelowLimit_ShouldReturnFalse()
    {
        _service.RecordUsage("chrome", TimeSpan.FromMinutes(60));
        var rule = new AppRule
        {
            ProcessName = "chrome",
            Type = RuleType.LimitTime,
            TimeLimitMinutes = 120,
            IsEnabled = true
        };

        _service.IsOverLimit("chrome", rule).Should().BeFalse();
    }

    [Fact]
    public void IsOverLimit_WhenRuleTypeIsBlock_ShouldReturnFalse()
    {
        _service.RecordUsage("chrome", TimeSpan.FromMinutes(999));
        var rule = new AppRule { ProcessName = "chrome", Type = RuleType.Block, IsEnabled = true };

        _service.IsOverLimit("chrome", rule).Should().BeFalse();
    }

    [Fact]
    public void ResetUsage_ShouldClearTrackedTime()
    {
        _service.RecordUsage("chrome", TimeSpan.FromMinutes(90));
        _service.ResetUsage("chrome");

        _service.GetUsageToday("chrome").Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void RecordUsage_CaseInsensitive_ShouldTrackSameProcess()
    {
        _service.RecordUsage("CHROME", TimeSpan.FromMinutes(30));
        _service.RecordUsage("chrome", TimeSpan.FromMinutes(30));

        _service.GetUsageToday("chrome").Should().Be(TimeSpan.FromMinutes(60));
    }
}
