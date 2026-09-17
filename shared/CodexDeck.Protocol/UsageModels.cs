namespace CodexDeck.Protocol;

public sealed record UsageWindow(string Id, double? UsedPercent, DateTimeOffset? ResetsAt, bool Supported, DateTimeOffset UpdatedAt);

public sealed record CreditsSnapshot(decimal? Balance, string? Unit, decimal? LastCost, DateTimeOffset? LastCostAt, bool Supported, DateTimeOffset UpdatedAt);

public sealed record UsageSnapshot(IReadOnlyList<UsageWindow> Windows, CreditsSnapshot Credits, string Plan, DateTimeOffset UpdatedAt);

public enum UsageAlertLevel { Unknown, Healthy, Warning, Exhausted }

public static class UsageAlertMapper
{
    public static UsageAlertLevel Map(double? remainingPercent, bool supported, DateTimeOffset updatedAt, DateTimeOffset now, double warningThreshold = 20)
    {
        if (warningThreshold is < 1 or > 99) throw new ArgumentOutOfRangeException(nameof(warningThreshold));
        if (!supported || remainingPercent is null || now - updatedAt > TimeSpan.FromMinutes(5)) return UsageAlertLevel.Unknown;
        if (remainingPercent <= 0) return UsageAlertLevel.Exhausted;
        return remainingPercent <= warningThreshold ? UsageAlertLevel.Warning : UsageAlertLevel.Healthy;
    }
}

public static class UsageLayoutValidator
{
    public static bool IsValid(string? layout) => layout is "combined" or "separate" or "combined_with_credits";
}
