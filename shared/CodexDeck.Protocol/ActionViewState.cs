namespace CodexDeck.Protocol;

public sealed record ActionViewState(string Label, string Status, bool Enabled, string Feedback);

public static class ActionViewStateMapper
{
    public static ActionViewState Map(string label, string status) => status switch
    {
        ProtocolConstants.Accepted => new(label, status, true, "Accepted"),
        ProtocolConstants.Failed => new(label, status, true, "Failed"),
        ProtocolConstants.Unavailable => new(label, status, false, "Unavailable"),
        "sending" => new(label, status, false, "Sending"),
        "running" => new(label, status, false, "Running"),
        "succeeded" => new(label, status, true, "Complete"),
        "cancelled" => new(label, status, true, "Cancelled"),
        "idle" => new(label, status, true, "Ready"),
        _ => new(label, "unavailable", false, "Unavailable"),
    };
}
