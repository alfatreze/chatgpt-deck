#nullable enable

namespace Loupedeck.CodexDeckPlugin
{
    using System;
    using System.Threading;
    using CodexDeck.Protocol;

    public sealed class ConnectionStatusCommand : PluginDynamicCommand
    {
        private string _status = "unavailable";

        public ConnectionStatusCommand() : base("Companion Status", "Check the local Codex Deck companion", "Diagnostics") { }

        protected override void RunCommand(string actionParameter)
        {
            PluginLog.Info("Connection Status action pressed");
            _status = "sending";
            ActionImageChanged();
            LoopbackClient? client = null;
            try
            {
                var endpoint = EndpointStore.Resolve();
                if (endpoint is null) { _status = "unavailable"; PluginLog.Info("Connection status unavailable: no valid endpoint"); return; }
                PluginLog.Info($"Connection status probing {EndpointStore.Source()} endpoint on port {endpoint.Port}");
                client = new LoopbackClient();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                var snapshot = client.ConnectAndReadSnapshotAsync(endpoint.Port, endpoint.Token, 2, timeout.Token).GetAwaiter().GetResult();
                _status = DateTimeOffset.UtcNow - snapshot.UpdatedAt > ProtocolConstants.SnapshotStaleAfter ? "unavailable" : "succeeded";
            }
            catch (Exception exception)
            {
                _status = "unavailable";
                PluginLog.Info($"Connection status unavailable: {exception.Message}");
            }
            finally
            {
                client?.DisposeAsync().GetAwaiter().GetResult();
                ActionImageChanged();
            }
        }

        protected override string GetCommandDisplayName(string actionParameter, PluginImageSize imageSize)
        {
            return _status switch
            {
                "sending" => "Companion Status\nChecking companion",
                "succeeded" => "Companion Status\nCompanion connected",
                _ => "Companion Status\nCompanion unavailable"
            };
        }
    }
}
