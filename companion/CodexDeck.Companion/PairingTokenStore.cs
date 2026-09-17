using System.Security.Cryptography;

namespace CodexDeck.Companion;

public sealed class PairingTokenStore : IPairingTokenStore
{
    public string GetOrCreate(string path)
    {
        if (File.Exists(path)) return File.ReadAllText(path).Trim();
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, token);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(temporary, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        File.Move(temporary, path);
        return token;
    }
}
