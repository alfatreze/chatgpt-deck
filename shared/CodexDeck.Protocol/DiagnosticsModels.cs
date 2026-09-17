namespace CodexDeck.Protocol;

public sealed record DiagnosticsSnapshot(
    PermissionState Permission,
    bool CompanionConnected,
    DateTimeOffset UpdatedAt,
    string? Detail = null);

public static class DiagnosticsViewMapper
{
    public static string Map(DiagnosticsSnapshot snapshot, DateTimeOffset now)
    {
        if (now - snapshot.UpdatedAt > TimeSpan.FromMinutes(5)) return "Status unavailable — Test again";
        if (snapshot.Permission != PermissionState.Ready) return PermissionViewMapper.Map(snapshot.Permission).Label;
        return snapshot.CompanionConnected ? "Connected" : "Connection needed";
    }
}
