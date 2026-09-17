namespace CodexDeck.Protocol;

public sealed record ActionIntent(
    string Type,
    int ProtocolVersion,
    Guid Id,
    string Action,
    string Source = "hardware",
    string? TaskId = null);

public sealed record ActionReceipt(
    string Type,
    int ProtocolVersion,
    Guid Id,
    string Status,
    string? Reason = null);

public sealed record ActionState(bool Enabled, string Status);

public sealed record StateSnapshot(
    string Type,
    int ProtocolVersion,
    string Mode,
    IReadOnlyList<string> Capabilities,
    DateTimeOffset UpdatedAt,
    IReadOnlyDictionary<string, ActionState> Actions);

public sealed record HelloAck(string Type, int ProtocolVersion, StateSnapshot Snapshot);

public static class ProtocolConstants
{
    public const int CurrentVersion = 1;
    public const string ActionIntentType = "action.intent";
    public const string ActionReceiptType = "action.receipt";
    public const string SnapshotType = "state.snapshot";
    public const string Accepted = "accepted";
    public const string Unavailable = "unavailable";
    public const string Failed = "failed";
    public const string NotFocused = "not_focused";
    public const string NotSupported = "not_supported";
    public const string ExecutionFailed = "execution_failed";
    public static readonly TimeSpan SnapshotStaleAfter = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan SnapshotUnknownAfter = TimeSpan.FromSeconds(30);
}
