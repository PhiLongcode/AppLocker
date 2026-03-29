using AppLocker.Application.Services;
using AppLocker.Domain.Entities;
using AppLocker.Infrastructure.Services;
using AppLocker.Infrastructure.Storage;
using AppLocker.Presentation.Infrastructure;
using AppLocker.Presentation.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace AppLocker.Presentation.ViewModels;

/// <summary>
/// ViewModel cho MainWindow: quản lý danh sách rule và điều khiển monitor.
/// </summary>
public class MainViewModel : BaseViewModel, IDisposable
{
    private readonly RuleEngineService _ruleEngine;
    private readonly SqliteStorageService _storage;
    private readonly PasswordService _passwordService;

    private MonitorService? _monitorService;

    private bool _isMonitoring;
    private string _statusText = "Chưa giám sát";
    private AppRuleItem? _selectedRule;

    public ObservableCollection<AppRuleItem> Rules { get; } = new();
    public ObservableCollection<InstalledAppItem> InstalledPrograms { get; } = new();

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
    public ICommand LoadInstalledProgramsCommand { get; }

    public MainViewModel(SqliteStorageService storage, PasswordService passwordService)
    {
        _storage = storage;
        _passwordService = passwordService;
        _ruleEngine = new RuleEngineService();

        TryMigrateJsonToSqlite();
        LoadRulesFromStorage();

        ToggleMonitorCommand = new RelayCommand(ToggleMonitor);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        LoadInstalledProgramsCommand = new RelayCommand(LoadInstalledProgramsWithConsent);
    }

    public void Dispose() => _storage.Dispose();

    private void TryMigrateJsonToSqlite()
    {
        if (_storage.LoadRules().Count > 0) return;

        var dataDir = Path.GetDirectoryName(AppDataPaths.GetDatabasePath())!;
        var jsonPath = Path.Combine(dataDir, "applocker.json");
        if (!File.Exists(jsonPath)) return;

        var legacy = new JsonStorageService(jsonPath);
        var rules = legacy.LoadRules();
        if (rules.Count == 0) return;

        _storage.SaveRules(rules);
    }

    private void LoadRulesFromStorage()
    {
        var rules = _storage.LoadRules();
        Rules.Clear();
        _ruleEngine.ClearRules();
        foreach (var r in rules)
        {
            _ruleEngine.AddRule(r);
            Rules.Add(ToItem(r));
        }

        StatusText = Rules.Count == 0
            ? "Chưa có rule — mở Cài đặt để thêm (SQLite + mật khẩu lưu trong DB)"
            : $"Đã tải {Rules.Count} rule(s) từ SQLite";
    }

    /// <summary>
    /// Hỏi quyền đọc Registry (danh sách cài đặt). App đã chạy nâng quyền Administrator thì đọc HKLM đầy đủ.
    /// </summary>
    private void LoadInstalledProgramsWithConsent()
    {
        var ok = System.Windows.MessageBox.Show(
            "AppLocker sẽ đọc danh sách phần mềm đã cài từ Windows (Registry: các khóa Gỡ cài đặt).\n\n" +
            "Chương trình cần chạy với quyền Quản trị viên để xem đầy đủ ứng dụng hệ thống.\n\n" +
            "Bạn có đồng ý tiếp tục?",
            "Xác nhận truy cập cấu hình hệ thống",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        if (ok != MessageBoxResult.Yes)
        {
            StatusText = "Đã hủy: không tải danh sách từ Registry.";
            return;
        }

        try
        {
            var svc = new InstalledProgramsService();
            InstalledPrograms.Clear();
            foreach (var r in svc.EnumerateInstalledPrograms()
                         .OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                InstalledPrograms.Add(new InstalledAppItem
                {
                    DisplayName = r.DisplayName,
                    ProcessName = r.ProcessName,
                    Publisher = r.Publisher,
                    ExecutablePath = r.ExecutablePath
                });
            }

            StatusText = $"Đã tải {InstalledPrograms.Count} ứng dụng từ danh sách cài đặt Windows";
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Không đọc được danh sách cài đặt:\n{ex.Message}",
                "AppLocker",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            StatusText = "Lỗi khi đọc danh sách phần mềm đã cài.";
        }
    }

    private void ToggleMonitor()
    {
        if (IsMonitoring)
            StopMonitoring();
        else
            StartMonitoring();
    }

    /// <summary>Bật giám sát (dùng khi khởi động nền với --background).</summary>
    public void StartMonitoring()
    {
        if (IsMonitoring) return;

        var evaluator = new RuleEvaluator(_ruleEngine.Rules);
        var processService = new ProcessService();
        var enforcement = new EnforcementService();
        var presenter = new WpfPasswordLockPresenter(_passwordService);

        _monitorService = new MonitorService(processService, evaluator, enforcement, presenter);
        _ = _monitorService.StartAsync(intervalMs: 2000);

        IsMonitoring = true;
        StatusText = "🟢 Đang giám sát… (PasswordLock: chặn → nhập mật khẩu để mở lại)";
    }

    public void StopMonitoring()
    {
        if (!IsMonitoring) return;
        _monitorService?.Stop();
        IsMonitoring = false;
        StatusText = "⏹ Đã dừng giám sát";
    }

    private void OpenSettings()
    {
        // Chưa có mật khẩu: bắt buộc qua màn hình tạo mật khẩu trước khi vào Cài đặt.
        // Đã có mật khẩu: đã xác thực khi mở app (App.OnStartup), không hỏi lại trong cùng phiên.
        if (!_passwordService.IsPasswordSet)
        {
            var pwdVm = new PasswordViewModel(_passwordService);
            var pwdWin = new Views.PasswordWindow { DataContext = pwdVm };
            pwdWin.Owner = System.Windows.Application.Current.MainWindow;

            if (pwdWin.ShowDialog() != true)
                return;
        }

        var vm = new SettingsViewModel(_ruleEngine, _storage, Rules);
        var win = new Views.SettingsWindow { DataContext = vm };
        win.Owner = System.Windows.Application.Current.MainWindow;
        win.ShowDialog();

        LoadRulesFromStorage();
        StatusText = $"🔄 Đã đồng bộ — {Rules.Count} rule(s) · SQLite";
    }

    private static AppRuleItem ToItem(AppRule r) => new()
    {
        ProcessName = r.ProcessName,
        RuleType = r.Type.ToString(),
        TimeLimitMinutes = r.TimeLimitMinutes,
        IsEnabled = r.IsEnabled
    };
}
