# Codex Deck companion (development skeleton)

Build and run:

```text
dotnet run --project companion/CodexDeck.Companion/CodexDeck.Companion.csproj
```

The process binds to `127.0.0.1` on an ephemeral port and prints the selected port. Set `CODEX_DECK_PORT` to choose a development port and set the same value for the plugin diagnostic action. Set `CODEX_DECK_TOKEN` to require a matching `token` property in each JSON envelope; the plugin reads the same environment variable. On macOS, pairing tokens use Keychain by default; set `CODEX_DECK_TOKEN_STORE=file` only for explicit development-only plaintext storage. The current skeleton handles newline-delimited envelopes per connection:

- `hello` → `hello.ack` with shortcut mode
- `action.intent` → `action.receipt` with `accepted`
- unknown or malformed input → error

Example smoke setup:

```text
CODEX_DECK_PORT=47831 CODEX_DECK_TOKEN=dev-token dotnet run --project companion/CodexDeck.Companion/CodexDeck.Companion.csproj
```

In the same environment, launch/reload Loupedeck and assign `Companion Status`. A successful result requires a fresh `hello.ack`; no configured port intentionally reports unavailable.

Token storage is selected with `CODEX_DECK_TOKEN_STORE=file|keychain`. The default `file` mode is for development. `keychain` is macOS-only and writes the pairing token to Keychain; test it only when explicitly reviewing secure-store behavior. `CODEX_DECK_TOKEN` remains the non-persistent test override.

On startup the companion publishes its active port and token to the user-scoped `CodexDeck/connection.json` file so the plugin can discover an ephemeral development port.

This is a development transport seam, not a release daemon. Persistent pairing, reconnect lifecycle, and OS shortcut execution are still pending.

The companion is currently a foreground development process, not an installed background service. It requires no additional macOS authorization beyond the plugin’s separate Accessibility/Automation permissions. Start it from Terminal with the command above; stop it by pressing `Ctrl-C` in that same Terminal window. A future release will provide an installer, launch-at-login option, and an in-app stop/restart control.
