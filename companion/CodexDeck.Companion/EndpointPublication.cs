using System.Text.Json;

namespace CodexDeck.Companion;

public static class EndpointPublication
{
    private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CodexDeck", "connection.json");

    public static void Write(int port, string token)
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CodexDeck");
        Directory.CreateDirectory(directory);
        var path = FilePath;
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(new { port, token }));
        File.Move(temporary, path, true);
        if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    public static void Clear() { try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch { } }
}
