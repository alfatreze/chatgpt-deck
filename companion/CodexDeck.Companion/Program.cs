using System.Net;
using System.Net.Sockets;
using CodexDeck.Protocol;

var configuredPort = int.TryParse(Environment.GetEnvironmentVariable("CODEX_DECK_PORT"), out var parsedPort)
    ? parsedPort
    : 0;
if (configuredPort is < 0 or > 65535) throw new ArgumentOutOfRangeException("CODEX_DECK_PORT");
var listener = new TcpListener(IPAddress.Loopback, configuredPort);
listener.Start();
var tokenPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CodexDeck", "pairing.token");
var expectedToken = Environment.GetEnvironmentVariable("CODEX_DECK_TOKEN")
    ?? CodexDeck.Companion.PairingTokenStoreFactory.Create().GetOrCreate(tokenPath);
var activePort = ((IPEndPoint)listener.LocalEndpoint).Port;
CodexDeck.Companion.EndpointPublication.Write(activePort, expectedToken);
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    CodexDeck.Companion.EndpointPublication.Clear();
    listener.Stop();
};
Console.WriteLine($"Codex Deck companion listening on {activePort}");
var adapter = OperatingSystem.IsMacOS() ? new CodexDeck.Companion.MacCodexAdapter() : null;

try
{
while (true)
{
    var client = await listener.AcceptTcpClientAsync();
    _ = Task.Run(() => HandleClientAsync(client));
}

async Task HandleClientAsync(TcpClient client)
{
    using (client)
    {
    await using var stream = client.GetStream();
    using var reader = new StreamReader(stream);
    await using var writer = new StreamWriter(stream) { AutoFlush = true };
    while (true)
    {
        using var readTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        string? line;
        try { line = await reader.ReadLineAsync(readTimeout.Token); }
        catch (OperationCanceledException) { break; }
        if (line is null) break;
        try
        {
            var envelope = System.Text.Json.JsonDocument.Parse(line);
            if (!string.IsNullOrEmpty(expectedToken)
                && (!envelope.RootElement.TryGetProperty("token", out var token)
                    || token.GetString() != expectedToken))
            {
                await writer.WriteLineAsync("{\"type\":\"error\",\"protocolVersion\":1,\"reason\":\"unauthorized\"}");
                continue;
            }
            var messageType = envelope.RootElement.GetProperty("type").GetString();
            if (messageType == "hello")
            {
                var probe = adapter is null
                    ? new CapabilityProbe(OperatingMode.SetupNeeded, new HashSet<string>())
                    : await adapter.ProbeAsync(CancellationToken.None);
                var snapshot = new StateSnapshot(
                    ProtocolConstants.SnapshotType, 1, "shortcut",
                    probe.Capabilities.ToArray(), DateTimeOffset.UtcNow,
                    probe.Capabilities.ToDictionary(action => action, _ => new ActionState(true, "idle")));
                var response = new HelloAck("hello.ack", 1, snapshot);
                await writer.WriteLineAsync(System.Text.Json.JsonSerializer.Serialize(response, ProtocolJson.Options));
                continue;
            }
            if (messageType == "action.intent")
            {
                if (!envelope.RootElement.TryGetProperty("id", out var id)
                    || !envelope.RootElement.TryGetProperty("action", out var action)
                    || id.ValueKind != System.Text.Json.JsonValueKind.String
                    || action.ValueKind != System.Text.Json.JsonValueKind.String)
                {
                    await writer.WriteLineAsync("{\"type\":\"error\",\"protocolVersion\":1,\"reason\":\"invalid_message\"}");
                    continue;
                }
                var intent = new ActionIntent(ProtocolConstants.ActionIntentType, ProtocolConstants.CurrentVersion,
                    Guid.Parse(id.GetString()!), action.GetString()!);
                var receipt = adapter is null
                    ? new ActionReceipt(ProtocolConstants.ActionReceiptType, ProtocolConstants.CurrentVersion, intent.Id, ProtocolConstants.Unavailable, ProtocolConstants.NotSupported)
                    : await adapter.DispatchAsync(intent, CancellationToken.None);
                await writer.WriteLineAsync(System.Text.Json.JsonSerializer.Serialize(receipt, ProtocolJson.Options));
                continue;
            }
            await writer.WriteLineAsync("{\"type\":\"error\",\"protocolVersion\":1,\"reason\":\"unsupported\"}");
        }
        catch (Exception)
        {
            await writer.WriteLineAsync("{\"type\":\"error\",\"protocolVersion\":1,\"reason\":\"invalid_message\"}");
        }
    }
}
}
}
catch (SocketException exception) when (exception.SocketErrorCode is SocketError.OperationAborted or SocketError.Interrupted)
{
    // Expected when Ctrl-C stops the listener.
}
