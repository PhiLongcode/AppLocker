using AppLocker.Domain.Entities;

namespace AppLocker.Application.Services;

/// <summary>
/// Quản lý toàn bộ danh sách AppRule: load, add, remove, toggle.
/// </summary>
public class RuleEngineService
{
    private readonly List<AppRule> _rules = new();

    public IReadOnlyList<AppRule> Rules => _rules.AsReadOnly();

    public void AddRule(AppRule rule) => _rules.Add(rule);

    public void ClearRules() => _rules.Clear();

    public void RemoveRule(string processName) =>
        _rules.RemoveAll(r => r.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));

    public void ToggleRule(string processName, bool isEnabled)
    {
        var rule = _rules.FirstOrDefault(r =>
            r.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));
        if (rule is not null) rule.IsEnabled = isEnabled;
    }

    /// <summary>Trả về danh sách rule đang active</summary>
    public IEnumerable<AppRule> GetActiveRules() => _rules.Where(r => r.IsEnabled);
}
