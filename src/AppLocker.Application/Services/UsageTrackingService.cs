using AppLocker.Domain.Entities;

namespace AppLocker.Application.Services;

/// <summary>
/// Theo dõi thời gian sử dụng ứng dụng trong ngày (in-memory).
/// Phase 3: sẽ kết hợp với SQLite để persist data.
/// </summary>
public class UsageTrackingService
{
    // key: processName (lowercase), value: time used today
    private readonly Dictionary<string, TimeSpan> _usageMap = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Ghi nhận thêm thời gian sử dụng cho một process.</summary>
    public void RecordUsage(string processName, TimeSpan duration)
    {
        if (_usageMap.ContainsKey(processName))
            _usageMap[processName] += duration;
        else
            _usageMap[processName] = duration;
    }

    /// <summary>Lấy tổng thời gian đã dùng hôm nay cho một process.</summary>
    public TimeSpan GetUsageToday(string processName)
    {
        return _usageMap.TryGetValue(processName, out var usage) ? usage : TimeSpan.Zero;
    }

    /// <summary>Kiểm tra xem process đã vượt giới hạn thời gian chưa.</summary>
    public bool IsOverLimit(string processName, AppRule rule)
    {
        if (rule.Type != RuleType.LimitTime || rule.TimeLimitMinutes is null)
            return false;

        var usage = GetUsageToday(processName);
        return usage.TotalMinutes > rule.TimeLimitMinutes.Value;
    }

    /// <summary>Reset thời gian tracking của một process (dùng khi sang ngày mới).</summary>
    public void ResetUsage(string processName)
    {
        _usageMap.Remove(processName);
    }

    /// <summary>Reset toàn bộ tracking (sang ngày mới).</summary>
    public void ResetAll() => _usageMap.Clear();
}
