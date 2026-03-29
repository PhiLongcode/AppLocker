using AppLocker.Application.Services;
using AppLocker.Domain.Entities;
using AppLocker.Infrastructure.Storage;
using AppLocker.Presentation.Infrastructure;
using AppLocker.Presentation.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace AppLocker.Presentation.ViewModels;

/// <summary>
/// ViewModel cho SettingsWindow: thêm/sửa/xóa/toggle AppRule.
/// </summary>
public class SettingsViewModel : BaseViewModel
{
    private readonly RuleEngineService _ruleEngine;
    private readonly JsonStorageService _storage;
    private readonly ObservableCollection<AppRuleItem> _mainList;

    private string _newProcessName = string.Empty;
    private string _selectedRuleType = "Block";
    private int _timeLimitMinutes = 60;
    private AppRuleItem? _selectedItem;
    private string _statusMessage = string.Empty;

    public ObservableCollection<AppRuleItem> Rules { get; } = new();

    public string NewProcessName
    {
        get => _newProcessName;
        set => SetProperty(ref _newProcessName, value);
    }

    public string SelectedRuleType
    {
        get => _selectedRuleType;
        set
        {
            SetProperty(ref _selectedRuleType, value);
            OnPropertyChanged(nameof(IsTimeLimitVisible));
        }
    }

    public int TimeLimitMinutes
    {
        get => _timeLimitMinutes;
        set => SetProperty(ref _timeLimitMinutes, value);
    }

    public bool IsTimeLimitVisible => SelectedRuleType == "LimitTime";

    public AppRuleItem? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public List<string> RuleTypes { get; } = new() { "Block", "LimitTime" };

    public ICommand AddRuleCommand { get; }
    public ICommand RemoveRuleCommand { get; }
    public ICommand ToggleRuleCommand { get; }
    public ICommand SaveCommand { get; }

    public SettingsViewModel(
        RuleEngineService ruleEngine,
        JsonStorageService storage,
        ObservableCollection<AppRuleItem> mainList)
    {
        _ruleEngine = ruleEngine;
        _storage = storage;
        _mainList = mainList;

        // Load từ engine hiện tại
        foreach (var r in _ruleEngine.Rules)
            Rules.Add(new AppRuleItem
            {
                ProcessName = r.ProcessName,
                RuleType = r.Type.ToString(),
                TimeLimitMinutes = r.TimeLimitMinutes,
                IsEnabled = r.IsEnabled
            });

        AddRuleCommand = new RelayCommand(AddRule, () => !string.IsNullOrWhiteSpace(NewProcessName));
        RemoveRuleCommand = new RelayCommand(RemoveRule, () => SelectedItem is not null);
        ToggleRuleCommand = new RelayCommand(ToggleRule, () => SelectedItem is not null);
        SaveCommand = new RelayCommand(Save);
    }

    private void AddRule()
    {
        var name = NewProcessName.Trim().ToLower().Replace(".exe", "");
        if (Rules.Any(r => r.ProcessName.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            StatusMessage = "⚠ Rule đã tồn tại!";
            return;
        }

        var item = new AppRuleItem
        {
            ProcessName = name,
            RuleType = SelectedRuleType,
            TimeLimitMinutes = SelectedRuleType == "LimitTime" ? TimeLimitMinutes : null,
            IsEnabled = true
        };

        Rules.Add(item);
        NewProcessName = string.Empty;
        StatusMessage = $"✅ Đã thêm: {name}";
    }

    private void RemoveRule()
    {
        if (SelectedItem is null) return;
        Rules.Remove(SelectedItem);
        StatusMessage = $"🗑 Đã xóa: {SelectedItem.ProcessName}";
        SelectedItem = null;
    }

    private void ToggleRule()
    {
        if (SelectedItem is null) return;
        SelectedItem.IsEnabled = !SelectedItem.IsEnabled;
        OnPropertyChanged(nameof(SelectedItem));
        StatusMessage = $"🔄 {SelectedItem.ProcessName}: {(SelectedItem.IsEnabled ? "Active" : "Disabled")}";
    }

    private void Save()
    {
        // Sync lại toàn bộ rule vào engine
        var domainRules = Rules.Select(r => new AppRule
        {
            ProcessName = r.ProcessName,
            Type = Enum.Parse<RuleType>(r.RuleType),
            TimeLimitMinutes = r.TimeLimitMinutes,
            IsEnabled = r.IsEnabled
        }).ToList();

        _storage.SaveRules(domainRules);

        // Refresh rule engine
        foreach (var r in domainRules) _ruleEngine.RemoveRule(r.ProcessName);
        foreach (var r in domainRules) _ruleEngine.AddRule(r);

        StatusMessage = $"💾 Đã lưu {domainRules.Count} rule(s) thành công!";
    }
}
