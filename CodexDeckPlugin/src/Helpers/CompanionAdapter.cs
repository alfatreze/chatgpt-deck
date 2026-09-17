using System;
using System.Threading;
using CodexDeck.Protocol;

namespace Loupedeck.CodexDeckPlugin;

internal sealed class CompanionAdapter : ICodexAdapter
{
    public async Task<CapabilityProbe> ProbeAsync(CancellationToken cancellationToken)
    {
        var endpoint = EndpointStore.Resolve();
        if (endpoint is null) return new CapabilityProbe(OperatingMode.SetupNeeded, new HashSet<string>());
        await using var client = new LoopbackClient();
        var snapshot = await client.ConnectAndReadSnapshotAsync(endpoint.Port, endpoint.Token, 2, cancellationToken);
        return new CapabilityProbe(Enum.TryParse<OperatingMode>(snapshot.Mode, true, out var mode) ? mode : OperatingMode.Degraded, snapshot.Capabilities.ToHashSet(StringComparer.Ordinal));
    }

    public async Task<ActionReceipt> DispatchAsync(ActionIntent intent, CancellationToken cancellationToken)
    {
        var endpoint = EndpointStore.Resolve();
        if (endpoint is null)
            return new ActionReceipt(ProtocolConstants.ActionReceiptType, ProtocolConstants.CurrentVersion, intent.Id, ProtocolConstants.Unavailable, "companion_unavailable");
        try
        {
            await using var client = new LoopbackClient();
            await client.ConnectWithRetryAsync(endpoint.Port, endpoint.Token, 2, cancellationToken);
            return await client.SendIntentAsync(intent, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch { return new ActionReceipt(ProtocolConstants.ActionReceiptType, ProtocolConstants.CurrentVersion, intent.Id, ProtocolConstants.Unavailable, "companion_unavailable"); }
    }
}
