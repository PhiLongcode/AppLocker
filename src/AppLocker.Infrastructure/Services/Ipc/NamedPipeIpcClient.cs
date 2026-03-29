using System.IO.Pipes;
using System.Text;

namespace AppLocker.Infrastructure.Services.Ipc;

/// <summary>
/// Client gửi lệnh qua NamedPipes tới Windows Service (gọi từ WPF App).
/// </summary>
public class NamedPipeIpcClient : IDisposable
{
    private readonly string _pipeName;

    public NamedPipeIpcClient(string pipeName = "AppLockerIpc")
    {
        _pipeName = pipeName;
    }

    public async Task<string> SendMessageAsync(string message, int timeoutMs = 2000)
    {
        using var pipeClient = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        
        // Kết nối với timeout
        await pipeClient.ConnectAsync(timeoutMs);

        pipeClient.ReadMode = PipeTransmissionMode.Message;

        using var writer = new StreamWriter(pipeClient, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };
        using var reader = new StreamReader(pipeClient, Encoding.UTF8, leaveOpen: true);

        await writer.WriteLineAsync(message);
        var response = await reader.ReadLineAsync();

        return response ?? string.Empty;
    }

    public void Dispose()
    { }
}
