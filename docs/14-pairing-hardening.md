# Pairing hardening checklist

## Implemented development path

- Companion binds only to loopback.
- Authenticated hello and action envelopes use a per-installation token.
- Companion publishes its active endpoint atomically to a user-scoped file.
- Plugin validates port range, protocol version, and snapshot freshness.
- Environment variables remain explicit test overrides.
- Token access is now behind the shared `IPairingTokenStore` interface; the file-backed `PairingTokenStore` remains the development implementation while platform-secure stores are added.
- `CODEX_DECK_TOKEN_STORE=file|keychain` selects the implementation explicitly; unset defaults to the development file store, while `keychain` is supported only on macOS.

## Required before release

1. Move the token from plaintext JSON into macOS Keychain (and the equivalent protected store on Windows).
2. Store only endpoint metadata in `connection.json`; never store secrets there.
3. Add pairing/reset UI with confirmation and a visible permission explanation.
4. Add reconnect backoff and a stale-file repair path after crash/forced termination.
5. Test companion replacement, token rotation, malformed config, and concurrent startup.
6. Ensure diagnostics report source/state without exposing tokens or prompt/task content.

Secure-store selection must be runtime-specific: macOS Keychain on macOS, Windows Credential Manager/DPAPI on Windows, and the file store only for explicit development/test mode. Secure-store failures must produce a clear pairing error and must not silently downgrade a release build to plaintext storage.

## macOS permission UX

The plugin exposes `Permissions & Connection` and `Test Permissions` diagnostic actions under `Diagnostics`. The first opens the Accessibility settings pane when needed; the second performs a harmless recheck. Protected action failures should direct users to the setup action with a readable `Permission needed — Open Settings` message.

The Actions SDK documentation describes action/editor controls, images, and plugin capabilities, but does not document a generic runtime confirmation dialog API. Therefore, pre-action explanation belongs in the assigned action label/feedback and the dedicated diagnostics action; the macOS consent dialog remains system-owned.

The plugin cannot silently grant Accessibility permission. The supported flow is:

1. Detect a failed System Events probe.
2. Show a concise explanation: “Codex Deck needs Accessibility permission to send the New Task shortcut to Codex. It does not read prompt or code content.”
3. Offer **Open Accessibility Settings**, launching `x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility`.
4. Instruct the user to enable Loupedeck/Logi Plugin Service, then return and press **Test again**.

On first use, macOS may display a native consent prompt for **LogiPluginService** controlling **System Events**. The prompt is expected; selecting **Allow** is what enables keyboard automation. The plugin should explain this immediately before the first probe.

Automation consent may also appear under **Privacy & Security → Automation**. Never claim permission is granted until a real probe succeeds.

## Adaptive onboarding button

The initial profile should include one `Permissions & Connection` action. Its label/background/feedback changes from `Permission needed` → `Allow Accessibility` → `Allow Automation` → `Test connection` → `Connected`. Each press performs only the next harmless probe or opens the relevant settings pane. Once all required permissions are verified, the action remains available as `Companion Status`; users may remove it from their layout at that point.

Protected actions continue to run their own preflight checks. If permission later disappears, they fail closed with `Permission needed`, and the onboarding action returns to the appropriate step. The plugin must not repeatedly trigger prompts in a loop or remove the button automatically.

The current implementation is intentionally suitable for local development and controlled testing, not unattended distribution.
