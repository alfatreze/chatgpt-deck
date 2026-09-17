namespace Loupedeck.CodexDeckPlugin;

internal static class StatusImages
{
    public static BitmapImage For(string status, PluginImageSize imageSize)
    {
        var normalized = string.IsNullOrWhiteSpace(status) ? "idle" : status.Replace('_', '-');
        PluginLog.Info($"Status image requested: {normalized}");
        using var builder = new BitmapBuilder(imageSize);
        builder.Clear(ColorFor(normalized));
        return builder.ToImage();
    }

    private static BitmapColor ColorFor(string status) => status switch
    {
        "accepted" or "checking" or "running" => new BitmapColor(0, 122, 255),
        "succeeded" => new BitmapColor(52, 168, 83),
        "failed" or "attention" or "companion-disconnected" => new BitmapColor(196, 46, 46),
        "unavailable" or "permission-needed" or "setup-needed" or "stale" => new BitmapColor(245, 158, 11),
        "not-supported" or "cancelled" => new BitmapColor(97, 97, 97),
        _ => new BitmapColor(54, 54, 54),
    };
}
