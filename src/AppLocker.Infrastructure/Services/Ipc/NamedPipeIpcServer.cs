using System.IO.Pipes;
using System.Text;

namespace AppLocker.Infrastructure.Services.Ipc;

/// <summary>
/// Server lắng nghe kết nối IPC thông qua NamedPipes (chạy ngầm trong Windows Service).
/// </summary>
public class NamedPipeIpcServer : IDisposable
{
    private readonly string _pipeName;
    private CancellationTokenSource? _cts;
    private bool _isRunning;

    /// <summary>Func xử lý request nhận được và trả về response text.</summary>
    public Func<string, string>? OnMessageReceived { get; set; }

    public NamedPipeIpcServer(string pipeName = "AppLockerIpc")
    {
        _pipeName = pipeName;
    }

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        _cts = new CancellationTokenSource();

        _ = Task.Run(ListenLoop, _cts.Token);
    }

    private async Task ListenLoop()
    {
        while (_isRunning && _cts is not null && !_cts.Token.IsCancellationRequested)
        {
            try
            {
                using var pipeServer = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous);

                await pipeServer.WaitForConnectionAsync(_cts.Token);

                // Khi có client connect, xử lý request đó trên luồng/Task riêng để không block pipe
                _ = ProcessClientRequestAsync(pipeServer);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Lỗi pipe, tiếp tục loop
            }
        }
    }

    private async Task ProcessClientRequestAsync(NamedPipeServerStream stream)
    {
        try
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };

            var request = await reader.ReadLineAsync();
            if (request is not null)
            {
                var response = OnMessageReceived?.Invoke(request) ?? "ERROR: Handler not set";
                await writer.WriteLineAsync(response);
            }
        }
        catch
        {
            // Ignored
        }
        finally
        {
            if (stream.IsConnected) stream.Disconnect();
            stream.Dispose();
        }
    }

    public void Stop()
    {
        _isRunning = false;
        _cts?.Cancel();
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
    }
}
