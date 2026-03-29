using FluentAssertions;
using AppLocker.Infrastructure.Services.Ipc;

namespace AppLocker.Tests.Infrastructure;

/// <summary>
/// TDD Red: Tests cho IPC (NamedPipes) Server/Client TRƯỚC KHI implement.
/// Trong kiến trúc mới: 
/// - Server chạy trên Windows Service.
/// - Client chạy trên UI (WPF App) gửi lệnh (Ping, ReloadConfig, GetStatus).
/// </summary>
public class NamedPipeIpcTests : IDisposable
{
    private readonly string _pipeName;
    private readonly NamedPipeIpcServer _server;
    private readonly NamedPipeIpcClient _client;

    public NamedPipeIpcTests()
    {
        // Dùng tên pipe unique cho mỗi test để tránh conflict do chạy song song
        _pipeName = $"AppLockerTestPipe_{Guid.NewGuid():N}";
        _server = new NamedPipeIpcServer(_pipeName);
        _client = new NamedPipeIpcClient(_pipeName);
    }

    public void Dispose()
    {
        _server.Stop();
        _client.Dispose();
    }

    [Fact]
    public async Task SendMessage_WhenServerIsRunning_ShouldReceiveResponse()
    {
        // Arrange
        var requestText = "PING";
        var expectedResponse = "PONG";

        _server.OnMessageReceived = (msg) => 
        {
            if (msg == requestText) return expectedResponse;
            return "UNKNOWN";
        };

        _server.Start();

        // Act
        var response = await _client.SendMessageAsync(requestText);

        // Assert
        response.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task SendMessage_WhenServerIsDown_ShouldThrowException()
    {
        // KHÔNG start server. Client Connect sẽ ném lỗi.
        var act = async () => await _client.SendMessageAsync("PING", timeoutMs: 500);

        await act.Should().ThrowAsync<TimeoutException>();
    }

    [Fact]
    public async Task SendMessage_MultipleRequests_ShouldHandleConcurrently()
    {
        _server.OnMessageReceived = (msg) => $"ECHO:{msg}";
        _server.Start();

        var t1 = _client.SendMessageAsync("MSG1");
        var t2 = _client.SendMessageAsync("MSG2");

        var results = await Task.WhenAll(t1, t2);

        results.Should().Contain("ECHO:MSG1");
        results.Should().Contain("ECHO:MSG2");
    }
}
