namespace CodexDeck.Protocol;

public enum OperatingMode
{
    SetupNeeded,
    Shortcut,
    Live,
    Degraded,
}

public sealed record CapabilityProbe(
    OperatingMode Mode,
    IReadOnlySet<string> Capabilities);

public interface ICodexAdapter
{
    Task<CapabilityProbe> ProbeAsync(CancellationToken cancellationToken);
    Task<ActionReceipt> DispatchAsync(ActionIntent intent, CancellationToken cancellationToken);
}
