using AppLocker.Application.Services;
using AppLocker.Domain.Entities;
using AppLocker.Infrastructure.Storage;
using AppLocker.Presentation.Models;
using AppLocker.Presentation.ViewModels;
using FluentAssertions;
using System.Collections.ObjectModel;

namespace AppLocker.Tests.Presentation;

/// <summary>
/// Unit Tests cho SettingsViewModel (sprint-0.5).
/// ViewModels không phụ thuộc WPF dispatcher nên có thể test trực tiếp.
/// </summary>
public class SettingsViewModelTests : IDisposable
{
    private readonly string _tempFile;
    private readonly RuleEngineService _engine;
    private readonly JsonStorageService _storage;
    private readonly ObservableCollection<AppRuleItem> _mainList;

    public SettingsViewModelTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"vm_test_{Guid.NewGuid()}.json");
        _engine = new RuleEngineService();
        _storage = new JsonStorageService(_tempFile);
        _mainList = new ObservableCollection<AppRuleItem>();
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile)) File.Delete(_tempFile);
    }

    private SettingsViewModel CreateVm() =>
        new(_engine, _storage, _mainList);

    // ── AddRule ───────────────────────────────────────────────────────────────

    [Fact]
    public void AddRule_WithValidName_ShouldAddToRulesList()
    {
        var vm = CreateVm();
        vm.NewProcessName = "chrome";
        vm.SelectedRuleType = "Block";

        vm.AddRuleCommand.Execute(null);

        vm.Rules.Should().HaveCount(1);
        vm.Rules[0].ProcessName.Should().Be("chrome");
    }

    [Fact]
    public void AddRule_ShouldRemoveExeExtension()
    {
        var vm = CreateVm();
        vm.NewProcessName = "chrome.exe";
        vm.SelectedRuleType = "Block";

        vm.AddRuleCommand.Execute(null);

        vm.Rules[0].ProcessName.Should().Be("chrome");
    }

    [Fact]
    public void AddRule_WhenDuplicate_ShouldNotAddAndShowWarning()
    {
        var vm = CreateVm();
        vm.NewProcessName = "chrome";
        vm.SelectedRuleType = "Block";
        vm.AddRuleCommand.Execute(null);

        // Thêm lần 2 cùng tên
        vm.NewProcessName = "chrome";
        vm.AddRuleCommand.Execute(null);

        vm.Rules.Should().HaveCount(1);
        vm.StatusMessage.Should().Contain("⚠");
    }

    [Fact]
    public void AddRule_AfterAdding_ShouldClearNewProcessName()
    {
        var vm = CreateVm();
        vm.NewProcessName = "notepad";
        vm.SelectedRuleType = "Block";

        vm.AddRuleCommand.Execute(null);

        vm.NewProcessName.Should().BeEmpty();
    }

    [Fact]
    public void AddRuleCommand_CanExecute_WhenNameEmpty_ShouldReturnFalse()
    {
        var vm = CreateVm();
        vm.NewProcessName = "";

        vm.AddRuleCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void AddRuleCommand_CanExecute_WhenNameProvided_ShouldReturnTrue()
    {
        var vm = CreateVm();
        vm.NewProcessName = "chrome";

        vm.AddRuleCommand.CanExecute(null).Should().BeTrue();
    }

    // ── RemoveRule ────────────────────────────────────────────────────────────

    [Fact]
    public void RemoveRule_WhenItemSelected_ShouldRemoveFromList()
    {
        var vm = CreateVm();
        vm.NewProcessName = "chrome";
        vm.SelectedRuleType = "Block";
        vm.AddRuleCommand.Execute(null);

        vm.SelectedItem = vm.Rules[0];
        vm.RemoveRuleCommand.Execute(null);

        vm.Rules.Should().BeEmpty();
    }

    [Fact]
    public void RemoveRuleCommand_CanExecute_WhenNoSelection_ShouldReturnFalse()
    {
        var vm = CreateVm();
        vm.SelectedItem = null;

        vm.RemoveRuleCommand.CanExecute(null).Should().BeFalse();
    }

    // ── ToggleRule ────────────────────────────────────────────────────────────

    [Fact]
    public void ToggleRule_WhenEnabled_ShouldDisable()
    {
        var vm = CreateVm();
        vm.NewProcessName = "chrome";
        vm.SelectedRuleType = "Block";
        vm.AddRuleCommand.Execute(null);

        vm.SelectedItem = vm.Rules[0];
        vm.SelectedItem.IsEnabled = true;
        vm.ToggleRuleCommand.Execute(null);

        vm.SelectedItem.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void ToggleRule_WhenDisabled_ShouldEnable()
    {
        var vm = CreateVm();
        vm.NewProcessName = "chrome";
        vm.SelectedRuleType = "Block";
        vm.AddRuleCommand.Execute(null);

        vm.SelectedItem = vm.Rules[0];
        vm.SelectedItem.IsEnabled = false;
        vm.ToggleRuleCommand.Execute(null);

        vm.SelectedItem.IsEnabled.Should().BeTrue();
    }

    // ── IsTimeLimitVisible ────────────────────────────────────────────────────

    [Fact]
    public void IsTimeLimitVisible_WhenLimitTime_ShouldBeTrue()
    {
        var vm = CreateVm();
        vm.SelectedRuleType = "LimitTime";

        vm.IsTimeLimitVisible.Should().BeTrue();
    }

    [Fact]
    public void IsTimeLimitVisible_WhenBlock_ShouldBeFalse()
    {
        var vm = CreateVm();
        vm.SelectedRuleType = "Block";

        vm.IsTimeLimitVisible.Should().BeFalse();
    }

    // ── Save ─────────────────────────────────────────────────────────────────

    [Fact]
    public void SaveCommand_ShouldPersistRulesToFile()
    {
        var vm = CreateVm();
        vm.NewProcessName = "chrome";
        vm.SelectedRuleType = "Block";
        vm.AddRuleCommand.Execute(null);

        vm.SaveCommand.Execute(null);

        var loaded = _storage.LoadRules();
        loaded.Should().HaveCount(1);
        loaded[0].ProcessName.Should().Be("chrome");
        vm.StatusMessage.Should().Contain("💾");
    }
}

/// <summary>
/// Unit Tests cho AppRuleItem ViewModel model.
/// </summary>
public class AppRuleItemTests
{
    [Fact]
    public void StatusText_WhenEnabled_ShouldContainActive()
    {
        var item = new AppRuleItem { IsEnabled = true };
        item.StatusText.Should().Contain("Active");
    }

    [Fact]
    public void StatusText_WhenDisabled_ShouldContainDisabled()
    {
        var item = new AppRuleItem { IsEnabled = false };
        item.StatusText.Should().Contain("Disabled");
    }

    [Fact]
    public void RuleDescription_WhenBlock_ShouldReturnBlockText()
    {
        var item = new AppRuleItem { RuleType = "Block" };
        item.RuleDescription.Should().Contain("Chặn");
    }

    [Fact]
    public void RuleDescription_WhenLimitTime_ShouldIncludeMinutes()
    {
        var item = new AppRuleItem { RuleType = "LimitTime", TimeLimitMinutes = 90 };
        item.RuleDescription.Should().Contain("90");
    }

    [Fact]
    public void PropertyChanged_WhenIsEnabledSet_ShouldFire()
    {
        var item = new AppRuleItem();
        var changed = false;
        item.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(AppRuleItem.IsEnabled)) changed = true; };

        item.IsEnabled = false;

        changed.Should().BeTrue();
    }
}
