namespace Loupedeck.CodexDeckPlugin;

internal static class StatusImages
{
    public static BitmapImage For(string status)
    {
        var normalized = string.IsNullOrWhiteSpace(status) ? "idle" : status.Replace('_', '-');
        try { return PluginResources.ReadImage($"status/status-{normalized}.png"); }
        catch (FileNotFoundException) { return PluginResources.ReadImage("status/status-idle.png"); }
    }
}
