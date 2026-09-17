# Core profile and action-editor design

## Scope

**Status:** Design captured; implementation blocked on host discovery/registration verification. A compile-only prototype was removed from the shipping project after the host exposed only the existing dynamic actions.

P1-05 will expose a small, stable Core profile rather than a general-purpose command builder. Each assignment stores a stable action ID plus validated parameters; the companion remains the source of truth for capability and enabled state.

## Initial controls

- **Command:** Open Codex, Interrupt Codex, Accept, Reject, New Task.
- **Profile:** Core (default), Review, Debug, Refactor (templates arrive in Phase 2).
- **Feedback:** text feedback on by default; reduced motion and high contrast are shared preferences.

## Save and refresh rules

1. Validate action ID and parameters before save.
2. Reject unknown IDs, malformed values, and unsupported capabilities without mutating the existing assignment.
3. Persist only the validated document through `ShortcutBindingStore`.
4. Refresh labels/options after profile or capability snapshots change.
5. Unknown or stale bridge state renders unavailable; it never silently falls back to a guessed status.

The implementation should use the SDK `ActionEditorCommand` family only after the installed assembly signatures are reflected and captured in tests. Until then, the current dynamic commands remain the safe fallback.

Verified host signatures (macOS ARM64): `ActionEditorCommand(DeviceType)`, `ActionEditorAction(Boolean, Boolean, DeviceType)`, `ActionEditorTextbox(name, label, description)`, `ActionEditorListbox(name, label, description)`, and `ActionEditor.AddControl<T>(control)`. These are API evidence only; registration and persistence still require a physical host test.

Inherited inspection also confirms `ActionEditorCommand.HasActionEditor`, `ActionEditor` and `SetDescription`/`SetSupportedDevices` members. A prototype must still establish the SDK’s registration lifecycle and host callback behavior before shipping.

## Workflow template editor contract

- Stable IDs (`review`, `debug`, `refactor`) are never regenerated from labels.
- Labels are limited to 40 characters and Save rejects invalid or empty sets.
- The local store writes a versioned document, preserves a `.bak`, and recovers defaults from malformed files.
- Only metadata (ID, label, action, enabled) is persisted by default; prompt or repository content is out of scope.
- A template action renders unavailable until the companion advertises its `launch_*` capability.

## Pairing configuration note

`CODEX_DECK_PORT` and `CODEX_DECK_TOKEN` are development-only overrides. Loupedeck launches the plugin as a host child process, so shell environment inheritance is not a reliable production configuration channel. P1 pairing must persist an endpoint/token through the companion/plugin config store and expose a diagnostics/reset path.

Reference: [Logi Actions Action Editor Actions](https://logitech.github.io/actions-sdk-docs/csharp/plugin-features/action-editor-actions/).
