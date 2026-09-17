namespace Loupedeck.CodexDeckPlugin;

internal static class StatusImages
{
    public static BitmapImage For(string status, PluginImageSize imageSize)
    {
        var normalized = string.IsNullOrWhiteSpace(status) ? "idle" : status.Replace('_', '-');
        PluginLog.Info($"Status image requested: {normalized}");
        BitmapImage background;
        try { background = PluginResources.ReadImage($"status-runtime/status-{normalized}.png"); }
        catch (FileNotFoundException) { background = PluginResources.ReadImage("status-runtime/status-idle.png"); }
        using var builder = new BitmapBuilder(imageSize);
        builder.SetBackgroundImage(background);
        return builder.ToImage();
    }
}
