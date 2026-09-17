using System.Diagnostics;
using System.Security.Cryptography;

namespace CodexDeck.Companion;

/// macOS release-store candidate. Not selected by the development path yet.
public sealed class MacKeychainTokenStore : IPairingTokenStore
{
    private const string Service = "com.codexdeck.companion";
    private const string Account = "pairing-token";

    public string GetOrCreate(string path)
    {
        if (!OperatingSystem.IsMacOS()) throw new PlatformNotSupportedException("macOS Keychain is only available on macOS.");
        var existing = RunSecurity("find-generic-password", "-s", Service, "-a", Account, "-w");
        if (existing.ExitCode == 0 && !string.IsNullOrWhiteSpace(existing.Output)) return existing.Output.Trim();

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var created = RunSecurity("add-generic-password", "-U", "-s", Service, "-a", Account, "-w", token);
        if (created.ExitCode != 0) throw new InvalidOperationException($"Unable to store pairing token in Keychain: {created.Error.Trim()}");
        return token;
    }

    private static (int ExitCode, string Output, string Error) RunSecurity(params string[] arguments)
    {
        var info = new ProcessStartInfo { FileName = "/usr/bin/security", UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        using var process = Process.Start(info) ?? throw new InvalidOperationException("Unable to launch macOS security tool.");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output, error);
    }
}
