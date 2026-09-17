namespace Loupedeck.CodexDeckPlugin;

internal static class StatusImages
{
    public static BitmapImage For(string status, PluginImageSize imageSize, StatusFrameStyle style = StatusFrameStyle.FullBleed)
    {
        var normalized = string.IsNullOrWhiteSpace(status) ? "idle" : status.Replace('_', '-');
        using var builder = new BitmapBuilder(imageSize);
        var statusColor = ColorFor(normalized);
        switch (style)
        {
            case StatusFrameStyle.InsetRail:
                builder.Clear(Background);
                builder.FillRectangle(0, 0, Math.Max(8, builder.Width / 8), builder.Height, statusColor);
                break;
            case StatusFrameStyle.RoundedFrame:
                builder.Clear(Background);
                FillRoundedRectangle(builder, 5, 5, builder.Width - 10, builder.Height - 10, 10, statusColor);
                FillRoundedRectangle(builder, 12, 12, builder.Width - 24, builder.Height - 24, 5, Background);
                break;
            default:
                builder.Clear(statusColor);
                break;
        }
        return builder.ToImage();
    }

    private static readonly BitmapColor Background = new(26, 32, 44);

    private static void FillRoundedRectangle(BitmapBuilder builder, int x, int y, int width, int height, int radius, BitmapColor color)
    {
        var safeRadius = Math.Min(radius, Math.Min(width, height) / 2);
        builder.FillRectangle(x + safeRadius, y, width - 2 * safeRadius, height, color);
        builder.FillRectangle(x, y + safeRadius, width, height - 2 * safeRadius, color);
        builder.FillCircle(x + safeRadius, y + safeRadius, safeRadius, color);
        builder.FillCircle(x + width - safeRadius, y + safeRadius, safeRadius, color);
        builder.FillCircle(x + safeRadius, y + height - safeRadius, safeRadius, color);
        builder.FillCircle(x + width - safeRadius, y + height - safeRadius, safeRadius, color);
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

internal enum StatusFrameStyle { FullBleed, InsetRail, RoundedFrame }
