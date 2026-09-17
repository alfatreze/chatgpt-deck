namespace CodexDeck.Protocol;

public enum PermissionState
{
    Unknown,
    AccessibilityNeeded,
    AutomationNeeded,
    ConnectionNeeded,
    Ready,
}

public sealed record PermissionView(string Label, string Feedback, bool Enabled);

public static class PermissionViewMapper
{
    public static PermissionView Map(PermissionState state) => state switch
    {
        PermissionState.AccessibilityNeeded => new("Allow Accessibility", "Open Settings", true),
        PermissionState.AutomationNeeded => new("Allow Automation", "Open Settings", true),
        PermissionState.ConnectionNeeded => new("Connection Needed", "Check Companion", true),
        PermissionState.Ready => new("Connection Status", "Connected", true),
        _ => new("Permissions & Connection", "Setup needed", true),
    };
}
