# Architecture

## 1. Component model

```text
┌─────────────────────────────────────┐
│ Logitech Actions plugin               │
│ controls, displays, configuration    │
└───────────────┬─────────────────────┘
                │ localhost TCP / newline JSON v1
┌───────────────▼─────────────────────┐
│ Local companion                      │
│ bridge server · config · diagnostics │
│ action executor · state reducer      │
└───────┬───────────────────┬─────────┘
        │                   │
┌───────▼────────┐  ┌───────▼──────────┐
│ ShortcutAdapter │  │ LiveCodexAdapter │
│ OS key dispatch │  │ only if supported│
└────────────────┘  └──────────────────┘
        │                   │
        └─────────┬─────────┘
                  ▼
             Codex Desktop
```

## 2. Hardware runtime strategy

Loupedeck ended product sales in March 2025 and states that future plug-ins can serve Loupedeck owners through the backend shared with Logitech MX Creative Console. The plugin therefore targets the current Logitech Actions SDK/runtime abstraction rather than a device-private implementation. Phase 0 must verify actual packaging, control semantics, and supported devices, then record the minimum supported matrix. A Loupedeck-only release is permitted only with an explicit ADR that explains why it does not strand the product on a retired sales platform.

The initial implementation language is **C#**. This is not a generic-language preference: the project’s reusable findings show that the tested C# Actions SDK path exposes the action-editor and dynamic-action behavior needed for a multi-device control surface. Phase 0 may retain a tiny Node.js probe for API exploration, but no production dependency may be chosen from old documentation alone.

Before feature work, record the host application version, Logi Plugin Service path, CPU architecture, operating-system version, runtime, SDK assembly/package version, selected device models, and plugin packaging/linking method. Keep an SDK-inspection utility with the source; when documentation and the installed `PluginApi` disagree, verify by reflection and document the observed API.

## 3. Why a companion exists

The plugin must remain portable across Loupedeck SDK constraints and must not embed OS automation or Codex-specific discovery. The local companion owns those responsibilities, exposes a small versioned contract, and permits an independently testable mock.

## 4. Operating modes

| Mode | Required adapter | Available behavior |
| --- | --- | --- |
| `setup_needed` | none | diagnostics and configuration only |
| `shortcut` | `ShortcutAdapter` | P0 controls, with no task-aware guarantees |
| `live` | `LiveCodexAdapter` | verified status, task slots, exact reasoning display |
| `degraded` | partial adapter | only individually verified actions remain enabled |

The companion chooses mode from capability probe results; the plugin never chooses it optimistically.

## 5. Implementation boundary and configuration

The companion follows the same separation proven in `plugin-development-findings.md`:

| Component | Owns | Must not own |
| --- | --- | --- |
| Codex adapter | capability probe, action dispatch, verified state subscription | Loupedeck rendering or editor controls |
| State controller | state reduction, polling/subscription lifecycle, retry/backoff, dial coalescing | direct SDK event handlers |
| Settings registry | versioned local preferences, profile IDs, mappings, templates, migration/backup | live state or physical rendering |
| Plugin actions | translate SDK input, read snapshots, render feedback | automation timing, state polling, config migration |

This project controls Codex Desktop, not LAN devices. Therefore the reusable findings about mDNS discovery, mutable IP addresses, and a default network device are **not** requirements here. The analogous requirement that does apply is stable identity: task/profile/action mappings use opaque IDs, never display labels as identifiers.

## 6. Interfaces

### Platform adapter layout

Keep the protocol and action model platform-neutral, then select an OS-specific implementation inside the companion:

```text
companion/
  CodexAdapter.cs              # shared contract and capability model
  Mac/MacCodexAdapter.cs       # osascript, Accessibility, macOS activation
  Windows/WindowsCodexAdapter.cs # SendInput/UI Automation, Windows activation
```

Use `OperatingSystem.IsMacOS()` / `OperatingSystem.IsWindows()` for selection. Unsupported platforms return `unavailable`; they must not fall back to another OS’s automation.

```csharp
public interface ICodexAdapter
{
    Task<CapabilityProbe> ProbeAsync(CancellationToken cancellationToken);
    Task<ActionReceipt> DispatchAsync(ActionIntent intent, CancellationToken cancellationToken);
    IDisposable? Subscribe(Action<CodexEvent> onEvent);
}

public sealed record CapabilityProbe(
    OperatingMode Mode,
    IReadOnlySet<Capability> Capabilities);
```

The live adapter is a boundary, not a promise. Its implementation may only use an officially supported local interface or documented integration path. Until then it remains absent and `shortcut` is a complete P0 product mode.

## 7. Data ownership

| Data | Owner | Persistence |
| --- | --- | --- |
| Mappings, template metadata, color/accessibility preferences | companion | local user config |
| Shortcut availability and adapter capabilities | companion | in-memory + local diagnostic cache |
| Current device render state | plugin | in-memory |
| Prompt/code/task content | Codex | never persisted or relayed by this product |

## 8. Security boundaries

- Companion listens only on `127.0.0.1`/`::1` and authenticates its plugin connection with an installation-scoped secret.
- Configuration permissions are explicit: accessibility/automation approval on the host is required only for shortcut dispatch.
- Logs redact action payload details other than action kind, result category, and duration.
- Upgrade the protocol compatibly: plugin and companion negotiate versions before exchange.

## 9. Suggested package boundaries

```text
plugin/           C# Actions SDK bindings, rendering, action-editor controls, bridge client
companion/        C# localhost server, config, shared adapter contracts, controller, diagnostics
companion/Mac/    macOS automation and permission probes
companion/Windows/ Windows automation and permission probes
shared/           JSON Schema, C# contracts, reducer, fixtures, SDK-inspection utility
tests/            unit, contract, integration, physical-device test records
```
