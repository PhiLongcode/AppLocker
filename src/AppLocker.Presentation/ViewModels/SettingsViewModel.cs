using AppLocker.Application.Services;
using AppLocker.Domain.Entities;
using AppLocker.Infrastructure.Services;
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
    private readonly SqliteStorageService _storage;
    private readonly ObservableCollection<AppRuleItem> _mainList;

    private string _newProcessName = string.Empty;
    private string _selectedRuleType = "Block";
    private int _timeLimitMinutes = 60;
    private AppRuleItem? _selectedItem;
    private string _statusMessage = string.Empty;
    private InstalledAppItem? _selectedPreset;

    public ObservableCollection<AppRuleItem> Rules { get; } = new();

    /// <summary>Ứng dụng đã cài (Registry) — chọn để điền tên process.</summary>
    public ObservableCollection<InstalledAppItem> PresetApps { get; } = new();

    public string NewProcessName
    {
        get => _newProcessName;
        set => SetProperty(ref _newProcessName, value);
    }

    public InstalledAppItem? SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            if (!SetProperty(ref _selectedPreset, value)) return;
            if (value is not null && !string.IsNullOrWhiteSpace(value.ProcessName))
                NewProcessName = value.ProcessName;
        }
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

    public List<string> RuleTypes { get; } = new() { "Block", "LimitTime", "PasswordLock" };

    public ICommand AddRuleCommand { get; }
    public ICommand RemoveRuleCommand { get; }
    public ICommand ToggleRuleCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand ReloadPresetAppsCommand { get; }

    public SettingsViewModel(
        RuleEngineService ruleEngine,
        SqliteStorageService storage,
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

        LoadPresetApps();

        AddRuleCommand = new RelayCommand(AddRule, () => !string.IsNullOrWhiteSpace(NewProcessName));
        RemoveRuleCommand = new RelayCommand(RemoveRule, () => SelectedItem is not null);
        ToggleRuleCommand = new RelayCommand(ToggleRule, () => SelectedItem is not null);
        SaveCommand = new RelayCommand(Save);
        ReloadPresetAppsCommand = new RelayCommand(LoadPresetApps);
    }

    private void LoadPresetApps()
    {
        PresetApps.Clear();
        SelectedPreset = null;
        try
        {
            var svc = new InstalledProgramsService();
            foreach (var r in svc.EnumerateInstalledPrograms()
                         .OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                PresetApps.Add(new InstalledAppItem
                {
                    DisplayName = r.DisplayName,
                    ProcessName = r.ProcessName,
                    Publisher = r.Publisher,
                    ExecutablePath = r.ExecutablePath
                });
            }
        }
        catch
        {
            // Không có quyền Registry hoặc lỗi — vẫn cho nhập tay
        }
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
        SelectedPreset = null;
        StatusMessage = $"✅ Đã thêm: {name}";
    }

    private void RemoveRule()
    {
        var item = SelectedItem;
        if (item is null) return;

        // Capture trước khi Remove: ListView two-way binding có thể set SelectedItem = null
        // ngay trong CollectionChanged → dùng SelectedItem sau Remove gây NullReferenceException.
        var name = item.ProcessName;
        SelectedItem = null;
        Rules.Remove(item);
        StatusMessage = $"🗑 Đã xóa: {name}";
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

        _ruleEngine.ClearRules();
        foreach (var r in domainRules) _ruleEngine.AddRule(r);

        StatusMessage = $"💾 Đã lưu {domainRules.Count} rule(s) thành công!";
    }
}
