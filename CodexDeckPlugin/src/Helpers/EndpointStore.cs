#nullable enable

namespace Loupedeck.CodexDeckPlugin
{
    using System;
    using System.IO;
    using System.Text.Json;

    internal sealed record CompanionEndpoint(int Port, string? Token);

    internal static class EndpointStore
    {
        private static readonly string Path = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CodexDeck", "connection.json");

        public static CompanionEndpoint? Load()
        {
            try
            {
                if (!File.Exists(Path)) return null;
                var endpoint = JsonSerializer.Deserialize<CompanionEndpoint>(File.ReadAllText(Path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return endpoint is { Port: > 0 and <= 65535 } ? endpoint : null;
            }
            catch { return null; }
        }

        public static CompanionEndpoint? Resolve()
        {
            if (int.TryParse(Environment.GetEnvironmentVariable("CODEX_DECK_PORT"), out var port) && port > 0)
                return new CompanionEndpoint(port, Environment.GetEnvironmentVariable("CODEX_DECK_TOKEN"));
            return Load();
        }

        public static string Source() => int.TryParse(Environment.GetEnvironmentVariable("CODEX_DECK_PORT"), out var port) && port > 0 ? "environment" : "user-scoped file";
    }
}
