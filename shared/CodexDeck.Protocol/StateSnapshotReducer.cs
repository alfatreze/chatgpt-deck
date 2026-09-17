namespace CodexDeck.Protocol;

public sealed class StateSnapshotReducer
{
    public StateSnapshot? Current { get; private set; }

    public bool Apply(StateSnapshot snapshot)
    {
        if (snapshot.ProtocolVersion != ProtocolConstants.CurrentVersion) return false;
        if (snapshot.Actions.Values.Any(state => state.Status is not ("idle" or "sending" or "accepted" or "running" or "succeeded" or "failed" or "unavailable" or "cancelled"))) return false;
        if (snapshot.Actions.Any(pair => pair.Value.Enabled && !snapshot.Capabilities.Contains(pair.Key, StringComparer.Ordinal))) return false;
        if (Current is not null && snapshot.UpdatedAt <= Current.UpdatedAt) return false;
        Current = snapshot;
        return true;
    }

    public bool IsStale(DateTimeOffset now, TimeSpan maxAge) =>
        Current is null || now - Current.UpdatedAt > maxAge;

    public void Clear() => Current = null;

    public string GetFreshness(DateTimeOffset now) => Current is null
        ? "disconnected"
        : now - Current.UpdatedAt > ProtocolConstants.SnapshotUnknownAfter
            ? "unknown"
            : now - Current.UpdatedAt > ProtocolConstants.SnapshotStaleAfter
                ? "stale"
                : "fresh";
}
