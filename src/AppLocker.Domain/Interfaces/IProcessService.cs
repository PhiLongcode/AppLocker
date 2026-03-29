using AppLocker.Domain.Models;

namespace AppLocker.Domain.Interfaces;

/// <summary>
/// Hợp đồng lấy danh sách tiến trình đang chạy.
/// </summary>
public interface IProcessService
{
    IEnumerable<ProcessInfo> GetRunningProcesses();
}
