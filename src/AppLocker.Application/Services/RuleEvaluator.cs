using AppLocker.Domain.Entities;
using AppLocker.Domain.Interfaces;
using AppLocker.Domain.Models;

namespace AppLocker.Application.Services;

/// <summary>
/// Đánh giá rule cho một tiến trình dựa trên danh sách AppRule đang được cấu hình.
/// </summary>
public class RuleEvaluator : IRuleEvaluator
{
    private readonly IEnumerable<AppRule> _rules;

    public RuleEvaluator(IEnumerable<AppRule> rules)
    {
        _rules = rules;
    }

    public RuleResult Evaluate(ProcessInfo process)
    {
        var matchedRule = _rules.FirstOrDefault(r =>
            r.IsEnabled &&
            string.Equals(r.ProcessName, process.Name, StringComparison.OrdinalIgnoreCase));

        if (matchedRule is null)
            return new RuleResult { ShouldBlock = false, ProcessName = process.Name };

        if (matchedRule.Type == RuleType.Block)
        {
            return new RuleResult
            {
                ShouldBlock = true,
                Reason = $"Process '{process.Name}' is blocked by rule.",
                ProcessName = process.Name
            };
        }

        // RuleType.LimitTime - sẽ được xử lý bởi UsageTrackingService ở Phase 3
        return new RuleResult { ShouldBlock = false, ProcessName = process.Name };
    }
}
