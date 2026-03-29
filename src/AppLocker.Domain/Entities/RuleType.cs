namespace AppLocker.Domain.Entities;

/// <summary>
/// Loại rule áp dụng cho ứng dụng.
/// </summary>
public enum RuleType
{
    /// <summary>Chặn hoàn toàn - kill process ngay khi phát hiện</summary>
    Block,

    /// <summary>Giới hạn thời gian sử dụng theo phút</summary>
    LimitTime
}
