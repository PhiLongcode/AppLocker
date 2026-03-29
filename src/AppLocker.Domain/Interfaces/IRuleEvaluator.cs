using AppLocker.Domain.Models;

namespace AppLocker.Domain.Interfaces;

/// <summary>
/// Hợp đồng đánh giá rule cho một tiến trình.
/// </summary>
public interface IRuleEvaluator
{
    RuleResult Evaluate(ProcessInfo process);
}
