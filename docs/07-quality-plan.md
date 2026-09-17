# Quality, privacy, and release plan

## Test pyramid

## Current reproducible regression gate

Run all three before changing the bridge or host-facing actions:

```text
/usr/local/share/dotnet/dotnet build CodexDeckPlugin/CodexDeckPlugin.sln --no-restore
/usr/local/share/dotnet/dotnet build companion/CodexDeck.Companion/CodexDeck.Companion.csproj --no-restore
/usr/local/share/dotnet/dotnet run --project shared/CodexDeck.Protocol.Tests/CodexDeck.Protocol.Tests.csproj
python3 shared/validate_fixtures.py
python3 shared/test_protocol.py
```

The host smoke portion additionally requires a Loupedeck reload and action-browser inspection; a clean build alone is insufficient.

The same checks can be run with `scripts/regression_gate.sh`.

| Layer | What to test | Required examples |
| --- | --- | --- |
| Unit | Reducers, mapping validation, shortcuts, config migration | stale task, unavailable action, debounce, corrupt config |
| Contract | Plugin/companion JSON messages | malformed message, invalid token, version mismatch, reconnect snapshot |
| Integration | mock companion + plugin surface | press → receipt → render; disconnect/reconnect; rate limits |
| SDK/host smoke | development link + Plugin Service | clean build, load, one action press/log, reload, SDK inspection record |
| Manual hardware | reference Loupedeck, MX Creative Console, and reference OS | setup, all P0 controls, display truncation, LEDs, dial, sleep/wake, profile change, packaged image/label rendering |
| Accessibility | visual/motor/cognitive feedback | color-free recognition, reduced motion, discoverable disabled reasons |

## Key scenarios

1. Codex is not focused and Accept is pressed: clear `Focus Codex first` result, no repeated uncontrolled input.
2. Shortcut is missing: button is disabled and setup route is discoverable.
3. Companion dies while an action is sending: plugin reports disconnect/failure, then accepts a complete fresh snapshot on restart.
4. The user rotates reasoning rapidly: bridge receives coalesced steps, rendering remains responsive.
5. A live state heartbeat stops: status turns stale, then unknown; no “running” ghost remains.
6. Color vision or reduced-motion setting is enabled: all states stay recognizable.
7. A profile import has invalid bindings: preview identifies invalid fields and leaves existing profile intact.
8. Codex is not verified as foreground: Accept/Reject do not dispatch; surface offers Focus Codex recovery.
9. A device wakes or an application profile changes: a complete snapshot restores render state and no stale press is replayed.
10. Fast Mode/Continue in New Task are unavailable: they are disabled with setup copy, and no context is copied to the clipboard.
11. A dynamic action/editor choice refers to a removed profile: it renders a readable recovery state and does not silently target the default profile.
12. A static layout asset conflicts with dynamic feedback: physical-device test detects the stale/blank render before release.
13. Rapid dial input is coalesced at 75 ms, clamped, serialized against the current state, and discarded after an explicit Set action.
14. On macOS, New Task first activates Codex, waits briefly, sends Cmd+N through System Events, and reports the actual automation exit status.

## Performance budgets

| Operation | Budget |
| --- | --- |
| Hardware input → local render acknowledgement | ≤150 ms p95 |
| Plugin → companion receipt | ≤500 ms p95 |
| Snapshot render (10 task entries maximum) | ≤100 ms p95 |
| Reconnect → full usable snapshot | ≤2 s p95 |
| Companion memory at idle | target ≤100 MB |

## Privacy and telemetry

Telemetry is off by default. If enabled, aggregate locally before sending any future product analytics and obtain an explicit separate consent. Permitted fields: anonymous install cohort, app/plugin version, action category, result category, and duration bucket. Prohibited fields include prompt text, code, task names, repository paths, shortcut values, hardware serials, IP addresses, and raw event logs.

## Release checklist

- [ ] P0 acceptance criteria pass on the reference device.
- [ ] Minimal C# plugin load, press log, and reload are verified against the installed SDK/host—not inferred from a successful build.
- [ ] SDK/host baseline and API-inspection output are recorded and reproducible.
- [ ] Shortcut-only mode has been tested with missing permissions and missing bindings.
- [ ] macOS first-run System Events consent, Accessibility recovery, Test Permissions, and permission revocation/recovery have been manually tested.
- [ ] Each protected macOS shortcut has separate evidence for AppleScript exit/stderr, frontmost bundle detection, authorization, app activation, key dispatch, and wrong-app focus; failures are not collapsed into one generic `unavailable` result.
- [ ] Focus guard has blocked unsafe approval dispatch in an automated and manual test.
- [ ] Sleep/wake and profile change have been tested on every release surface.
- [ ] Long-press is not a required P0 activation path.
- [ ] Every button has a disabled reason and outcome treatment.
- [ ] Contract compatibility checks cover current and previous protocol version.
- [ ] Config migration and rollback/backup are tested.
- [ ] Reduced motion and non-color cues pass manual review.
- [ ] No secrets or prohibited data appear in logs; package audit passes.
- [ ] Action-editor Save validation, dynamic-list refresh, removed target recovery, and physical assets have been tested.
- [ ] Installation, setup, and uninstall instructions are tested from a clean user account.
- [ ] Known limitations list accurately distinguishes shortcut mode from live mode.
