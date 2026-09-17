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
        if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
            File.SetUnixFileMode(temporary, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        File.Move(temporary, path, true);
    }

    public static void Clear() { try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch { } }
}
