using AppLocker.Application.Services;
using AppLocker.Domain.Entities;
using AppLocker.Infrastructure.Services;
using AppLocker.Infrastructure.Storage;
using AppLocker.Presentation.Infrastructure;
using AppLocker.Presentation.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace AppLocker.Presentation.ViewModels;

/// <summary>
/// ViewModel cho MainWindow: quản lý danh sách rule và điều khiển monitor.
/// </summary>
public class MainViewModel : BaseViewModel
{
    private readonly RuleEngineService _ruleEngine;
    private readonly JsonStorageService _storage;
    private MonitorService? _monitorService;
    private Task? _monitorTask;

    private bool _isMonitoring;
    private string _statusText = "Chưa giám sát";
    private AppRuleItem? _selectedRule;

    public ObservableCollection<AppRuleItem> Rules { get; } = new();

    public bool IsMonitoring
    {
        get => _isMonitoring;
        set
        {
            SetProperty(ref _isMonitoring, value);
            OnPropertyChanged(nameof(MonitorButtonText));
        }
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string MonitorButtonText => IsMonitoring ? "⏹ Dừng giám sát" : "▶ Bắt đầu giám sát";

    public AppRuleItem? SelectedRule
    {
        get => _selectedRule;
        set => SetProperty(ref _selectedRule, value);
    }

    public ICommand ToggleMonitorCommand { get; }
    public ICommand OpenSettingsCommand { get; }

    public MainViewModel()
    {
        _ruleEngine = new RuleEngineService();
        _storage = new JsonStorageService(GetConfigPath());

        LoadRulesFromFile();

        ToggleMonitorCommand = new RelayCommand(ToggleMonitor);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
    }

    private static string GetConfigPath()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(baseDir, "..", "..", "..", "..", "..", "config", "applocker.json");
    }

    private void LoadRulesFromFile()
    {
        var rules = _storage.LoadRules();
        Rules.Clear();
        foreach (var r in rules)
        {
            _ruleEngine.AddRule(r);
            Rules.Add(ToItem(r));
        }
        StatusText = $"Đã tải {Rules.Count} rule(s)";
    }

    private void ToggleMonitor()
    {
        if (IsMonitoring)
        {
            _monitorService?.Stop();
            IsMonitoring = false;
            StatusText = "⏹ Đã dừng giám sát";
        }
        else
        {
            // Rebuild evaluator với danh sách rule hiện tại
            var evaluator = new RuleEvaluator(_ruleEngine.Rules);
            var processService = new ProcessService();
            var enforcement = new EnforcementService();

            _monitorService = new MonitorService(processService, evaluator, enforcement);
            _monitorTask = _monitorService.StartAsync(intervalMs: 2000);

            IsMonitoring = true;
            StatusText = "🟢 Đang giám sát...";
        }
    }

    private void OpenSettings()
    {
        var vm = new SettingsViewModel(_ruleEngine, _storage, Rules);
        var win = new Views.SettingsWindow { DataContext = vm };
        win.Owner = Application.Current.MainWindow;
        if (win.ShowDialog() == true)
        {
            // Reload sau khi settings thay đổi
            LoadRulesFromFile();
            StatusText = $"🔄 Đã cập nhật - {Rules.Count} rule(s)";
        }
    }

    private static AppRuleItem ToItem(AppRule r) => new()
    {
        ProcessName = r.ProcessName,
        RuleType = r.Type.ToString(),
        TimeLimitMinutes = r.TimeLimitMinutes,
        IsEnabled = r.IsEnabled
    };
}
