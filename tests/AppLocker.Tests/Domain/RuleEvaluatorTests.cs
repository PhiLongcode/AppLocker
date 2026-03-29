using AppLocker.Domain.Entities;
using AppLocker.Domain.Models;
using AppLocker.Application.Services;
using FluentAssertions;

namespace AppLocker.Tests.Domain;

/// <summary>
/// Unit Tests cho RuleEvaluator - bước Red/Green/Refactor theo TDD.
/// </summary>
public class RuleEvaluatorTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static ProcessInfo MakeProcess(string name) =>
        new() { ProcessId = 1, Name = name, FullPath = $@"C:\{name}.exe" };

    private static AppRule BlockRule(string name, bool enabled = true) =>
        new() { ProcessName = name, Type = RuleType.Block, IsEnabled = enabled };

    private static AppRule LimitRule(string name, int minutes, bool enabled = true) =>
        new() { ProcessName = name, Type = RuleType.LimitTime, TimeLimitMinutes = minutes, IsEnabled = enabled };

    // ── Block Rule ────────────────────────────────────────────────────────────

    [Fact]
    public void Evaluate_WhenProcessMatchesBlockRule_ShouldReturnShouldBlockTrue()
    {
        // Arrange
        var rules = new[] { BlockRule("chrome") };
        var evaluator = new RuleEvaluator(rules);

        // Act
        var result = evaluator.Evaluate(MakeProcess("chrome"));

        // Assert
        result.ShouldBlock.Should().BeTrue();
        result.ProcessName.Should().Be("chrome");
        result.Reason.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Evaluate_WhenProcessNameCaseInsensitive_ShouldStillBlock()
    {
        var rules = new[] { BlockRule("CHROME") };
        var evaluator = new RuleEvaluator(rules);

        var result = evaluator.Evaluate(MakeProcess("chrome"));

        result.ShouldBlock.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_WhenProcessNotInRules_ShouldReturnShouldBlockFalse()
    {
        var rules = new[] { BlockRule("notepad") };
        var evaluator = new RuleEvaluator(rules);

        var result = evaluator.Evaluate(MakeProcess("chrome"));

        result.ShouldBlock.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_WhenBlockRuleIsDisabled_ShouldNotBlock()
    {
        var rules = new[] { BlockRule("chrome", enabled: false) };
        var evaluator = new RuleEvaluator(rules);

        var result = evaluator.Evaluate(MakeProcess("chrome"));

        result.ShouldBlock.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_WhenRulesEmpty_ShouldReturnShouldBlockFalse()
    {
        var evaluator = new RuleEvaluator(Array.Empty<AppRule>());

        var result = evaluator.Evaluate(MakeProcess("chrome"));

        result.ShouldBlock.Should().BeFalse();
    }

    // ── LimitTime Rule ────────────────────────────────────────────────────────

    [Fact]
    public void Evaluate_WhenProcessMatchesLimitTimeRule_ShouldNotBlockByDefault()
    {
        // LimitTime chỉ được xử lý bởi UsageTrackingService ở Phase 3
        var rules = new[] { LimitRule("chrome", minutes: 60) };
        var evaluator = new RuleEvaluator(rules);

        var result = evaluator.Evaluate(MakeProcess("chrome"));

        result.ShouldBlock.Should().BeFalse();
    }

    // ── PasswordLock ──────────────────────────────────────────────────────────

    [Fact]
    public void Evaluate_WhenPasswordLock_ShouldBlockAndRequirePasswordUnlock()
    {
        var rules = new[] { new AppRule { ProcessName = "chrome", Type = RuleType.PasswordLock } };
        var evaluator = new RuleEvaluator(rules);

        var result = evaluator.Evaluate(MakeProcess("chrome"));

        result.ShouldBlock.Should().BeTrue();
        result.RequiresPasswordUnlock.Should().BeTrue();
        result.ExecutablePath.Should().Be($@"C:\chrome.exe");
    }

    // ── AppRule Entity ────────────────────────────────────────────────────────

    [Fact]
    public void AppRule_DefaultIsEnabled_ShouldBeTrue()
    {
        var rule = new AppRule { ProcessName = "chrome", Type = RuleType.Block };

        rule.IsEnabled.Should().BeTrue();
        rule.TimeLimitMinutes.Should().BeNull();
    }

    [Theory]
    [InlineData("Block")]
    [InlineData("LimitTime")]
    [InlineData("PasswordLock")]
    public void RuleType_AllValues_ShouldParseCorrectly(string typeName)
    {
        var parsed = Enum.Parse<RuleType>(typeName);
        parsed.Should().BeOneOf(RuleType.Block, RuleType.LimitTime, RuleType.PasswordLock);
    }
}
