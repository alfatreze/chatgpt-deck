using System.Net.Sockets;
using System.Text.Json;

namespace CodexDeck.Protocol;

public sealed class LoopbackClient : IAsyncDisposable
{
    private TcpClient _client = new();
    private StreamReader? _reader;
    private StreamWriter? _writer;

    public async Task ConnectAsync(int port, string? token, CancellationToken cancellationToken)
    {
        await _client.ConnectAsync("127.0.0.1", port, cancellationToken);
        var stream = _client.GetStream();
        _reader = new StreamReader(stream);
        _writer = new StreamWriter(stream) { AutoFlush = true };
        var hello = JsonSerializer.Serialize(new { type = "hello", protocolVersion = 1, token }, ProtocolJson.Options);
        await _writer.WriteLineAsync(hello);
    }

    public async Task ConnectWithRetryAsync(int port, string? token, int attempts, CancellationToken cancellationToken)
    {
        if (attempts < 1) throw new ArgumentOutOfRangeException(nameof(attempts));
        Exception? last = null;
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            try
            {
                await ConnectAsync(port, token, cancellationToken);
                return;
            }
            catch (SocketException exception) when (attempt + 1 < attempts)
            {
                last = exception;
                _client.Dispose();
                _client = new TcpClient();
                await Task.Delay(TimeSpan.FromMilliseconds(100 * (attempt + 1)), cancellationToken);
            }
        }
        throw new IOException("Unable to connect to companion after bounded retries.", last);
    }

    public async Task<StateSnapshot> ConnectAndReadSnapshotAsync(int port, string? token, int attempts, CancellationToken cancellationToken)
    {
        await ConnectWithRetryAsync(port, token, attempts, cancellationToken);
        return await ReadHelloSnapshotAsync(cancellationToken);
    }

    public async Task<JsonDocument> ReadAsync(CancellationToken cancellationToken)
    {
        if (_reader is null) throw new InvalidOperationException("Client is not connected.");
        var line = await _reader.ReadLineAsync(cancellationToken) ?? throw new IOException("Companion disconnected.");
        return JsonDocument.Parse(line);
    }

    public async Task<StateSnapshot> ReadHelloSnapshotAsync(CancellationToken cancellationToken)
    {
        using var response = await ReadAsync(cancellationToken);
        if (!response.RootElement.TryGetProperty("snapshot", out var snapshot))
            throw new JsonException("Hello response did not include a snapshot.");
        if (!snapshot.TryGetProperty("protocolVersion", out var version)
            || version.GetInt32() != ProtocolConstants.CurrentVersion)
            throw new JsonException("Snapshot protocol version is unsupported.");
        return ProtocolJson.Deserialize<StateSnapshot>(snapshot.GetRawText());
    }

    public async Task<ActionReceipt> SendIntentAsync(ActionIntent intent, CancellationToken cancellationToken)
    {
        if (_writer is null) throw new InvalidOperationException("Client is not connected.");
        await _writer.WriteLineAsync(JsonSerializer.Serialize(intent, ProtocolJson.Options));
        using var response = await ReadAsync(cancellationToken);
        if (!response.RootElement.TryGetProperty("type", out var type)
            || type.GetString() != ProtocolConstants.ActionReceiptType)
        {
            throw new JsonException("Companion response was not an action receipt.");
        }
        var receipt = ProtocolJson.Deserialize<ActionReceipt>(response.RootElement.GetRawText());
        if (receipt.Id != intent.Id) throw new JsonException("Receipt ID did not match intent ID.");
        return receipt;
    }

    public async ValueTask DisposeAsync()
    {
        _reader?.Dispose();
        if (_writer is not null) await _writer.DisposeAsync();
        _client.Dispose();
    }
}
