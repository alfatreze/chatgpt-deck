namespace CodexDeck.Companion;

public static class PairingTokenStoreFactory
{
    public static IPairingTokenStore Create()
    {
        var mode = Environment.GetEnvironmentVariable("CODEX_DECK_TOKEN_STORE")?.Trim().ToLowerInvariant();
        return mode switch
        {
            "keychain" when OperatingSystem.IsMacOS() => new MacKeychainTokenStore(),
            "file" or null or "" => new PairingTokenStore(),
            "keychain" => throw new PlatformNotSupportedException("Keychain token storage is only available on macOS."),
            _ => throw new InvalidOperationException("CODEX_DECK_TOKEN_STORE must be 'file' or 'keychain'.")
        };
    }
}
