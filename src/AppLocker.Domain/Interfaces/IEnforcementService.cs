namespace AppLocker.Domain.Interfaces;

/// <summary>
/// Hợp đồng thực thi hành động với tiến trình vi phạm.
/// </summary>
public interface IEnforcementService
{
    void Kill(string processName);
}
