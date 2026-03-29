using AppLocker.Domain.Models;

namespace AppLocker.Domain.Interfaces;

/// <summary>
/// Interface theo dõi sự kiện liên quan đến process (khởi tạo, dừng).
/// </summary>
public interface IProcessEventWatcher : IDisposable
{
    /// <summary>Sự kiện được kích hoạt khi có một tiến trình mới được khởi chạy.</summary>
    event EventHandler<ProcessInfo>? ProcessStarted;

    /// <summary>Bắt đầu lắng nghe sự kiện từ hệ điều hành.</summary>
    void StartListening();

    /// <summary>Dừng lắng nghe sự kiện.</summary>
    void StopListening();
}
