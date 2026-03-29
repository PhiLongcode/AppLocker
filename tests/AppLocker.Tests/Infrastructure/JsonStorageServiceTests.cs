using AppLocker.Domain.Entities;
using AppLocker.Infrastructure.Storage;
using FluentAssertions;
using System.Text.Json;

namespace AppLocker.Tests.Infrastructure;

/// <summary>
/// Unit Tests cho JsonStorageService (sprint-0.4).
/// Sử dụng thư mục tạm để tránh side-effect trên file system thật.
/// </summary>
public class JsonStorageServiceTests : IDisposable
{
    private readonly string _tempFile;
    private readonly JsonStorageService _service;

    public JsonStorageServiceTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"applocker_test_{Guid.NewGuid()}.json");
        _service = new JsonStorageService(_tempFile);
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile)) File.Delete(_tempFile);
    }

    [Fact]
    public void LoadRules_WhenFileNotExists_ShouldReturnEmptyList()
    {
        var rules = _service.LoadRules();

        rules.Should().NotBeNull();
        rules.Should().BeEmpty();
    }

    [Fact]
    public void SaveRules_ThenLoadRules_ShouldReturnSameData()
    {
        // Arrange
        var original = new List<AppRule>
        {
            new() { ProcessName = "chrome", Type = RuleType.Block, IsEnabled = true },
            new() { ProcessName = "steam", Type = RuleType.LimitTime, TimeLimitMinutes = 60, IsEnabled = true }
        };

        // Act
        _service.SaveRules(original);
        var loaded = _service.LoadRules();

        // Assert
        loaded.Should().HaveCount(2);
        loaded[0].ProcessName.Should().Be("chrome");
        loaded[0].Type.Should().Be(RuleType.Block);
        loaded[1].ProcessName.Should().Be("steam");
        loaded[1].TimeLimitMinutes.Should().Be(60);
    }

    [Fact]
    public void SaveRules_ShouldCreateValidJsonFile()
    {
        var rules = new List<AppRule>
        {
            new() { ProcessName = "notepad", Type = RuleType.Block, IsEnabled = true }
        };

        _service.SaveRules(rules);

        File.Exists(_tempFile).Should().BeTrue();
        var json = File.ReadAllText(_tempFile);
        json.Should().Contain("notepad");
        json.Should().Contain("Block");
    }

    [Fact]
    public void SaveRules_EmptyList_ShouldSaveAndLoadEmpty()
    {
        _service.SaveRules(new List<AppRule>());
        var loaded = _service.LoadRules();

        loaded.Should().BeEmpty();
    }

    [Fact]
    public void SaveRules_ShouldPreserveTimeLimitMinutes()
    {
        var rules = new List<AppRule>
        {
            new() { ProcessName = "steam", Type = RuleType.LimitTime, TimeLimitMinutes = 120, IsEnabled = true }
        };

        _service.SaveRules(rules);
        var loaded = _service.LoadRules();

        loaded[0].TimeLimitMinutes.Should().Be(120);
    }

    [Fact]
    public void LoadRules_WhenFileCorrupted_ShouldReturnEmptyList()
    {
        File.WriteAllText(_tempFile, "{ invalid json !!! }");

        var rules = _service.LoadRules();

        rules.Should().NotBeNull();
        rules.Should().BeEmpty();
    }

    [Fact]
    public void SaveRules_OverwriteExisting_ShouldReplaceContent()
    {
        // Save lần 1
        _service.SaveRules(new List<AppRule>
        {
            new() { ProcessName = "chrome", Type = RuleType.Block, IsEnabled = true }
        });

        // Save lần 2 - ghi đè
        _service.SaveRules(new List<AppRule>
        {
            new() { ProcessName = "discord", Type = RuleType.Block, IsEnabled = true }
        });

        var loaded = _service.LoadRules();

        loaded.Should().HaveCount(1);
        loaded[0].ProcessName.Should().Be("discord");
    }
}
