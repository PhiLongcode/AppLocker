using AppLocker.Presentation.Infrastructure;

namespace AppLocker.Presentation.Models;

/// <summary>
/// ViewModel item cho mỗi AppRule hiển thị trên UI list.
/// </summary>
public class AppRuleItem : BaseViewModel
{
    private string _processName = string.Empty;
    private string _ruleType = "Block";
    private int? _timeLimitMinutes;
    private bool _isEnabled = true;

    public string ProcessName
    {
        get => _processName;
        set => SetProperty(ref _processName, value);
    }

    public string RuleType
    {
        get => _ruleType;
        set => SetProperty(ref _ruleType, value);
    }

    public int? TimeLimitMinutes
    {
        get => _timeLimitMinutes;
        set => SetProperty(ref _timeLimitMinutes, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public string StatusText => IsEnabled ? "🟢 Active" : "⚫ Disabled";
    public string RuleDescription => RuleType switch
    {
        "LimitTime" => $"Giới hạn {TimeLimitMinutes} phút/ngày",
        "PasswordLock" => "Khóa bằng mật khẩu (Cần Unlock)",
        _ => "Chặn hoàn toàn"
    };
}
