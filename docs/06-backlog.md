# Delegable backlog

Each ticket is intentionally bounded. An agent should take one ticket, update its evidence field, and avoid unrelated refactors.

| ID | Phase | Ticket | Dependencies | Done when | Evidence |
| --- | --- | --- | --- | --- | --- |
| P0-00 | 0 | Establish repository hygiene and reproducible C# development baseline | none | `.gitignore`, license decision, clean-checkout build/reload instructions, local SDK/cache exclusions, and no generated artifacts tracked | In progress — Git initialized; ignores/editor/build procedure added; license decision pending |
| P0-01 | 0 | Select Loupedeck + MX Creative Console reference/runtime and document capability matrix | hardware access | host/Service path, architecture, SDK/runtime versions, inputs, output surfaces, SDK lifecycle, packaging, and cross-surface test route recorded | In progress — host, SDK, Plugin API path, and versions recorded; reference hardware/cross-surface matrix pending |
| P0-01a | 0 | Retarget to `net10.0` and build minimal C# load-and-log plugin | P0-01 | compatible target builds; development link loads; one action logs a press; reflection record resolves uncertain SDK API; reload is documented | Done — clean .NET 10 build, `.link`, reload, plugin load, dynamic-action log, hardware assignment, and three physical presses verified; sample action replaced by local `Open Codex` dispatch |
| P0-02 | 0 | Create `shared` protocol schema, fixtures, and mock bridge | none | valid/error handshake and snapshot fixtures pass contract tests | Done — versioned schema, typed C# contracts, receipt/hello fixtures, dependency-free validator, authenticated mock responses, endpoint publication, and loopback integration tests pass |
| P0-03 | 0 | Research official Codex local integration capability | none | decision recorded with source, constraints, and fallback | Done — official OpenAI developer search found API/CLI and use-case guidance but no supported Codex Desktop local control/status surface; retain shortcut mode and keep `LiveCodexAdapter` blocked |
| P1-01 | 1 | Companion pairing and loopback transport | P0-02 | unauthenticated/non-loopback connections rejected; reconnect sends snapshot | In progress — net10 companion plus typed `LoopbackClient` bind/use loopback, authenticated envelopes, correlated typed receipts, fresh shortcut snapshots, generated token, published endpoint file, shared `IPairingTokenStore` boundary, and macOS Keychain smoke test are verified; migration/rotation/denial tests, Windows secure store, pairing UX, reconnect policy, and forced-shutdown recovery remain |
| P1-02 | 1 | Config schema and migration-safe local store | P0-02 | opaque stable IDs, defaults, Save-time validation, backup/export, and corrupted-config recovery tested | In progress — versioned document format (schema 1) now saves while legacy list files still load; validated atomic save, automatic `.bak` backup, export, default recovery, and malformed JSON recovery pass; future migrations remain |
| P1-03 | 1 | `ShortcutAdapter`, focus guard, and host permission diagnostics | P0-03 | every P0 shortcut can be configured/tested; approval is blocked when focus is unknown; outcomes truthful | In progress — deterministic adapter/binding tests pass; Open/Focus/New Task and both interrupt paths are user-tested on macOS; macOS consent flow, permission probe, settings action, and Test Permissions action are implemented. Accept/Reject remain deferred without confirmed shortcuts or an official action interface. |
| P1-04 | 1 | Action reducer and receipt UI model | P0-02 | shortcut receipt cannot fabricate succeeded state | In progress — receipt reducer gates protocol version/correlation and `ActionViewStateMapper` covers the full valid lifecycle with fail-closed unknown handling; full Loupedeck rendering integration remains |
| P1-05 | 1 | Core-profile Loupedeck controls and action editor | P1-01, P1-04, P0-01a | controls render enabled/disabled/feedback states; dynamic labels/lists refresh after profile change; failed Save persists nothing | In progress — Action Editor API is now loadable and signatures are captured; registration/persistence and physical host behavior remain |
| P1-06 | 1 | Keyboard accessibility and reduced-motion implementation | P1-05 | each state has non-color cue and motion can be removed | In progress — shared `AccessibilityPreferences`, permission/usage text semantics, non-color state mappings, and Interrupt red attention frame are implemented and physically verified; broader host rendering and reduced-motion review remain |
| P1-06a | 1 | Verify runtime bitmap recoloring for dynamic state feedback | P1-05 | public `BitmapImage` construction/recolor API is identified, finite state frames render correctly on a physical surface, and reduced-motion behavior is covered | Pending — installed `PluginApi.dll` contains `CreateImage`/`ReplaceImageColor` symbols, but public signatures and hardware rendering are unverified |
| R1-01 | remediation | Enforce companion-only automation boundary | P1 scaffold | plugin sends intents only; companion adapter owns focus/activation/key dispatch; receipts are truthful | In progress — macOS companion adapter now validates and dispatches the verified shortcut set; plugin actions still need migration |
| R1-02 | remediation | Reconcile and harden bridge transport | R1-01 | deployed transport, schema, and documentation agree; authenticated server validates actions and serves bounded/concurrent clients | Done — newline-delimited TCP is documented, clients are concurrent, reads are bounded, and live integration smoke passes |
| R1-03 | remediation | Harden pairing defaults and endpoint publication | R1-02 | macOS defaults to Keychain, plaintext is explicit dev-only, endpoint publish has no permissive secret window | Done — secure default/explicit file override and live owner-only endpoint permission check pass |
| R1-04 | remediation | Add real companion integration tests | R1-01, R1-02 | tests exercise live server auth, malformed input, unsupported actions, dispatch states, and reconnect | Done — live smoke covers hello/auth/unauthorized/unsupported/malformed-id/reconnect and deterministic failed receipt mode |
| P2-01 | 2 | Workflow template launchers | P1-03, P1-05 | review/debug/refactor are editable and safely dispatched | In progress — stable validated metadata store, capability-gated UI actions, and editor contract are present; host editor integration and verified launch paths remain |
| P2-02 | 2 | Fast Mode and Continue-in-New-Task controls | P1-03, P1-05 | commands are visibly configured and never reconstruct context | Pending |
| P2-03 | 2 | Attention-slot policies and contextual dial modes | P1-05, P3-01 for live policies | P1 mode selection is visible; live ordering is deterministic | Pending |
| P2-04 | 2 | Review Changes/Open in Editor and voice/composer controls | P1-03, P1-05 | commands are capability-gated; voice survives/disconnects safely | Pending |
| P2-05 | 2 | Profile import/export preview | P1-02 | diff preview, backup, and malformed-file recovery work | Pending |
| P2-06 | 2 | Setup wizard and diagnostics screen | P1-03, P1-05 | user completes configuration without editing files | Pending |
| P3-01 | 3 | Implement supported `LiveCodexAdapter`, if viable | P0-03 | capability probe and provenance-tested events exist | Blocked by evidence |
| P3-02 | 3 | Live task status slots and freshness reducer | P3-01 | stale/ordered/disconnect behavior passes automated tests | Blocked by P3-01 |
| P4-01 | 4 | Multi-agent slot assignment and focus | P3-02 | five-task scenario meets five-second attention-find target | Blocked by P3-02 |
| P6-01 | 6 | Evaluate usage/queue awareness | official capability | privacy-reviewed prototype/rejection and user evidence recorded | Blocked by official capability |
| P6-02 | 6 | Evaluate handoff/evidence card | P3-01 | metadata-only prototype/rejection and agent-user usability evidence recorded | Blocked by P3-01 |
| P6-03 | 6 | Evaluate mutation-capable Git/PR controls | P2-04 | threat model and activation-safety review complete before implementation | Pending research |
| P6-04 | 6 | Evaluate remote/mobile companion | none | threat model and local-first decision complete | Pending research |
| P6-05 | 6 | Evaluate Codex Micro HID/API interoperability | P0-03 | read-only capture, bundle/API inventory, legal/terms review, compatibility risk assessment, and explicit go/no-go before any write path | Research only — feasible via owned-device HID capture and local bundle inspection; not a production dependency |
| P5-01 | 5 | Evaluate ChatGPT Desktop target | R1-01 | official capability evidence, opt-in target configuration, target-specific privacy/permission model, and no copied Codex automation assumptions | Pending — future goal; detail after bridge remediation |

## Suggested agent prompts

**Protocol agent**

> Implement P0-02 only. Create versioned JSON Schema, matching validated C# models, fixtures, mock bridge, and contract tests from `docs/04-protocol-and-state.md`. Do not wire hardware or automation. Report changed files and test output in the ticket evidence.

**Companion agent**

> Implement P1-03 only after P0-03 is complete. Follow `CodexAdapter` in `docs/03-architecture.md`; no UI scraping and no claim beyond confirmed key dispatch. Add unit tests for unavailable, permission denied, and dispatch failure.

**Plugin agent**

> Implement P1-05 only. Render the Core profile based exclusively on bridge snapshots. Include disabled states and no-color-only feedback. Do not add Codex automation or a status scraper to the plugin.
