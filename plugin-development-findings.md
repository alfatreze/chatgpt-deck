# Plugin Development Findings and Reusable Considerations

> **Curation policy:** This is a durable knowledge base, not an activity log. Add only findings that prevent future plugin mistakes, with evidence and applicability. Routine build/reload milestones belong in git history or the roadmap.

This document records practical findings from developing the Elgato Keylight plugin for macOS and the Loupedeck/Logi Actions SDK. It is intended as a reference for future plugins, not as a substitute for the current SDK documentation.

## 1. Validate the host and SDK before designing the plugin

- Confirm the installed Loupedeck version, Logi Plugin Service location, CPU architecture, macOS version, and available .NET/Node runtimes.
- A package can build successfully and still fail to load. The first feasibility milestone should be a minimal plugin that loads, registers one action, and writes a recognizable log entry when pressed.
- Keep the host-specific paths and build assumptions documented. On this project the active C# plugin is loaded through a development `.link` file under the Logi Plugin Service plug-in directory.
- Treat SDK documentation and installed assemblies as complementary sources. Reflection against the installed `PluginApi.dll` was useful when documentation did not expose exact event/property names.
- Keep a small API-inspection utility in the repository during development. It makes SDK upgrades and version differences easier to diagnose.

## 2. Choose the implementation language from tested constraints

- The Node.js/TypeScript route is quick for a feasibility spike and API prototyping.
- The C# SDK was the better long-term route here because it exposed the action-editor and dynamic-action behavior needed for a richer multi-device plugin.
- Do not commit to a language solely from an old SDK page. Verify that the installed Loupedeck release supports the intended platform and runtime.
- Keep the device-control layer independent from the Loupedeck action layer. That made it possible to move from the Node.js experiment to C# without redesigning the Elgato HTTP protocol implementation.

## 3. Keep device integration separate from UI actions

Use separate responsibilities:

- **HTTP service/client:** reads and updates the device API; validates response shapes and status codes.
- **Controller:** serializes requests, stores the latest state, publishes state changes, handles polling, retries, and adjustment coalescing.
- **Discovery:** finds devices on the local network and resolves names to addresses.
- **Settings registry:** persists stable device IDs, friendly aliases, addresses, and the default-device ID.
- **Actions:** translate Loupedeck events into controller calls and render feedback.

This separation prevents action code from owning network timing or configuration migration and makes the hardware layer testable without a Loupedeck device.

## 4. Treat configuration as a registry, not a single address

- Store a stable internal device ID. Do not use a friendly name or mutable IP address as the identity.
- Store at least: `Id`, `Name`, `Address`, and `DefaultDeviceId`.
- Upsert by normalized address when a device is configured again, while retaining the stable ID.
- Make the default device explicit. General actions target it; targeted actions retain their selected ID.
- Configuration must distinguish “save this device” from “make this device the default.” In this plugin the Configure Device editor uses a save-time choice:
  - **Keep current default**
  - **Make this light default**
- Default selection should apply only after successful validation and Save. Cancel or failed validation must not change the registry.
- A first successfully configured device can become the default automatically; later edits should not silently change the default.
- When a device is renamed, targeted actions should update their display text without changing their identity.

## 5. Discovery needs an explicit fallback and multi-device policy

- Use local mDNS/Bonjour discovery for convenience, but always offer manual IP/hostname entry.
- Discovery should validate the candidate by calling the device API before saving it.
- If discovery returns zero devices, show an actionable error and allow manual configuration.
- If discovery returns multiple devices, do not silently pick one for every new action. Require an explicit address/device choice or provide a device picker.
- A discovered friendly name is useful as a default alias, but the user must be able to replace it.
- Network discovery is affected by power state, subnet, Wi-Fi isolation, macOS Local Network permissions, and buffering behavior of command-line discovery tools.

## 6. Action Editor controls have persistence semantics

- `ActionEditorCommand` is the right primitive for button actions with user-configured values.
- Use sliders for numeric values such as brightness, temperature, and preset values; store the values in the action parameters, not global settings.
- Use dynamic list boxes for choices that depend on the current registry. Populate items from the saved device list and use stable IDs as item values.
- A checkbox can be misleading when it represents an operation (“make default”) rather than state (“is default”). Prefer labels that describe the save-time operation.
- The editor's built-in action **Name** is a useful place for a user-facing alias when the SDK already provides it. Avoid adding a second name field unless there is a real need.
- Do not assume a control's visual state is authoritative after another action changes global settings. Dynamic action labels or a clearly marked `(Default)` value are better for communicating current state.
- Validate on Save and keep failed edits out of the persisted registry.

## 7. Dynamic actions and folders

- `PluginDynamicCommand` supports runtime parameters through `AddParameter`, `RemoveAllParameters`, and `ParametersChanged`.
- Use the stable device ID as the dynamic action parameter. The display name can safely include the current alias and status.
- Subscribe dynamic actions to a registry-change event so the Loupedeck action list refreshes after a device is added, renamed, or removed.
- Separate general actions from targeted actions:
  - General actions operate on the current default device.
  - “By Light” actions retain one device ID and must not follow default changes.
- Mark the current default in targeted action labels, for example `Desk Light (Default): Toggle`, so users can understand the relationship without opening settings.
- Expect a device to disappear after an action was assigned. Handle removed IDs with a readable fallback and a recoverable error.

## 8. Dial controls need state-aware coalescing

- A dial can produce events faster than the light can accept HTTP requests. Sending every event creates latency and causes apparent jumps or missed movement.
- Accumulate the intended target, clamp it to the valid range, and flush after a short quiet period. This project uses a 75 ms coalescing window.
- Brightness has a safety floor of 3% so the brightness dial does not unexpectedly switch the light off.
- Temperature direction must be defined in user terms. Here, counterclockwise cools and clockwise warms, matching Control Center.
- Serialize polling and control calls with one per-device gate. A control request should start from the latest known state.
- Clear queued dial work when an explicit Set or Preset action is executed.

## 9. Polling, offline state, and feedback

- Poll periodically so changes made outside Loupedeck appear on the device.
- On failures, mark the device unavailable, publish a state change, and use bounded exponential backoff. Resume normal polling after a successful read.
- Every network action should have a visible offline/error path; never leave the user guessing whether a button press was received.
- Keep the last known state for useful display, but do not present it as current while the device is unavailable.
- Log concise, privacy-conscious diagnostics: operation, device alias/address where appropriate, and the exception reason. Avoid credentials and unnecessary payloads.

## 10. Image and label rendering pitfalls

- Test SVG and PNG assets on the actual Loupedeck device/editor. An asset that previews correctly in a browser may appear as a white square, tiny glyph, or blank tile in Loupedeck.
- Keep source assets and package assets separate and verify the exact resource names embedded by the C# project.
- Dynamic image rendering and static layout overrides can conflict. A static template may replace the output of `GetCommandImage`, which makes a dynamic image appear frozen or blank.
- Built-in layout choices such as icon-above-text, text-above-icon, and centered layouts are controlled by the host. The plugin should not assume it can fully override them.
- Use short, fixed labels for explicit actions. For stateful actions, dynamic labels are useful, but only if the host refreshes the action image/label reliably.
- Treat icon polish as a separate validation task after behavior works; visual iteration can otherwise obscure networking and state bugs.

## 11. Build, reload, and test workflow

Recommended loop:

1. Make a small source change.
2. Build with the same runtime and CLI-home used by the project.
3. Let the post-build target update the development link and request a plugin reload.
4. Read the plugin log immediately.
5. Test one physical action and one failure path.
6. Commit only source/documentation changes; keep `bin`, `obj`, local SDKs, caches, and packaged build output ignored.

The first clean checkout may need a normal restore before `--no-restore` builds work. Keep generated SDK/runtime folders out of Git and document how to recreate them.

## 12. Testing strategy

Use three layers:

- **Pure unit tests:** API parsing, clamping, temperature conversion, registry migration, and request coalescing.
- **Service/integration tests:** HTTP status handling, malformed responses, timeouts, and retries using a local fake server.
- **Physical-device tests:** discovery, read/update round trips, power transitions, sleep/wake recovery, multiple lights, and Loupedeck rendering.

For each action, test success, unreachable device, invalid configuration, device removal, and repeated/rapid input. Hardware tests should record the starting state and restore it when practical.

## 13. Packaging and repository hygiene

- Keep the package manifest's display name, version, license, minimum host version, supported devices, and resource paths explicit.
- Include the license and a README that distinguishes implemented behavior from planned work.
- Use a repository `.gitignore` for Node modules, .NET `bin/`/`obj/`, local SDKs, CLI homes, tool caches, generated packages, and development archives.
- Publish source and reproducible build instructions; publish release packages separately rather than committing every local build artifact.
- Initialize Git before the project becomes large, and verify the remote branch after publishing.

## 14. Reusable checklist

- [ ] Minimal plugin loads and logs a button press.
- [ ] Installed SDK/API version and host paths are recorded.
- [ ] Device client is separated from action code.
- [ ] Configuration uses stable IDs and an explicit default.
- [ ] Discovery has manual fallback and a multi-device policy.
- [ ] Action-editor values are validated and persisted correctly.
- [ ] Dynamic actions refresh after registry changes.
- [ ] Polling, retries, offline feedback, and request serialization are implemented.
- [ ] Dial input is clamped and coalesced.
- [ ] Images and labels are tested on the physical device.
- [ ] Unit, integration, and hardware tests cover failure paths.
- [ ] Build artifacts and local credentials are excluded from Git.
- [ ] README, license, package metadata, and release steps are complete.

## 15. Codex Deck addendum: current host/toolchain evidence (2026-09-16)

This section records findings confirmed while bootstrapping Codex Deck. It supersedes generic assumptions above when the installed host provides more specific evidence.

### Host inventory

- macOS `26.6.2`, Apple Silicon `arm64`.
- Loupedeck host: `/Applications/Loupedeck.app`, version `6.4.1.364`.
- Logi Plugin Service: `/Applications/Utilities/LogiPluginService.app`, version `6.4.1.3246`.
- The installed API assembly is `/Applications/Utilities/LogiPluginService.app/Contents/MonoBundle/PluginApi.dll`.
- The official `LogiPluginTool` global tool installed successfully at version `6.1.4.22672`.

### Framework compatibility finding

- The official generator currently emits a C# project targeting `net8.0`.
- The installed `PluginApi.dll` references `System.Runtime, Version=10.0.0.0`.
- Building the untouched generated project with .NET SDK `8.0.425` fails with `CS1705` because the reference assembly is newer than `net8.0`.
- This project therefore targets `net10.0`, matching the installed host API and the project owner’s previously successful .NET 10 plugin baseline. Do not downgrade the target merely to match the generator template.
- The target change is now proven through a clean build, development-link creation, host load, and plugin log entry; physical action press verification remains pending.

### Confirmed bootstrap sequence

```zsh
dotnet tool install --global LogiPluginTool
logiplugintool generate CodexDeck
# Retarget the generated project to net10.0 after inspecting the installed PluginApi.dll.
dotnet build CodexDeckPlugin/CodexDeckPlugin.sln
```

The generated project writes a development `.link` under:

```text
~/Library/Application Support/Logi/LogiPluginService/Plugins/
```

Plugin-specific logs are expected under:

```text
~/Library/Application Support/Logi/LogiPluginService/Logs/plugin_logs/
```

### Ongoing rule

Record host version, SDK/tool version, target framework, API assembly path, build result, link path, reload method, and physical verification result whenever the development environment changes. A successful compile alone is insufficient evidence that a plugin is loadable.

### Codex Deck result

- .NET SDK `10.0.401` successfully builds the retargeted `net10.0` project with 0 warnings and 0 errors.
- Logi Plugin Service loads the plugin from the generated development link and records `CodexDeck` version `1.0` plus two generated dynamic actions in `CodexDeck.log`.
- The remaining validation is assigning one generated action to a physical device and confirming both the press behavior and action-specific log entry.

## 16. Maintenance rule for future plugins

This file is a reusable engineering knowledge base, not a one-off Codex Deck log. Before closing any plugin-development task, ask whether the work discovered something that could save another plugin effort or prevent a repeat failure.

When the answer is yes, add a short dated entry containing:

1. **Environment:** host app, Plugin Service, OS, architecture, SDK/tool, and relevant device versions.
2. **Observation:** what actually happened, including failures and surprising behavior.
3. **Evidence:** command output, API inspection, log path, physical test, or authoritative documentation link.
4. **Applicability:** which SDK versions, host families, devices, or plugin patterns it affects; mark unknowns explicitly.
5. **Reusable recommendation:** the guardrail, test, code boundary, or setup step a future plugin should adopt.

Keep Codex-specific product decisions, feature priorities, and protocol choices in `docs/`. Keep cross-plugin SDK, host, packaging, rendering, input, lifecycle, and testing lessons here. If a later observation contradicts an older entry, preserve the older record and add the new evidence rather than rewriting history without explanation.

## 17. Plugin visibility and action discovery (2026-09-16)

- **Environment:** macOS, Loupedeck app with a connected Loupedeck Live, Logi Plugin Service 6.4.1.3246, generated CodexDeck plugin targeting `net10.0`.
- **Observation:** A generated plugin can be installed and loaded successfully yet remain hidden in the action browser until enabled through **Hide and show plugins**. Once enabled, a universal plugin (`HasNoApplication => true`) appears as its own plugin rail entry.
- **Evidence:** After enabling `CodexDeck`, the UI exposed `Adjustments` and `Commands`; expanding `Commands` showed the generated `Press Counter` action. The plugin log reported two dynamic actions loaded and the plugin loaded successfully.
- **Applicability:** Plugin visibility is a separate validation step from build/link/load. A missing action in the browser is not necessarily a registration failure.
- **Reusable recommendation:** Include “enable plugin in Hide and show plugins, select its rail entry, expand its action group” in first-run and troubleshooting checklists. Keep physical assignment/press verification as a distinct hardware test.

## 18. First physical action verification (2026-09-16)

- **Environment:** Same host and Loupedeck Live setup as entry 17; CodexDeck development link active.
- **Observation:** The generated `Press Counter` action executed successfully three times from a physical device assignment.
- **Evidence:** `CodexDeck.log` recorded `Counter value is 1`, `Counter value is 2`, and `Counter value is 3` at 23:28:02–23:28:03.
- **Applicability:** Confirms the end-to-end loop: generated package → development link → action browser → hardware assignment → plugin callback → plugin logging.
- **Reusable recommendation:** Keep one minimal counter/logging action in every new plugin until physical input is proven; use the log as the first hardware acceptance artifact before implementing external integrations.

## 19. First real local action: open/focus Codex (2026-09-16)

- **Environment:** Same macOS/Loupedeck Live host; .NET SDK 10.0.401; target `net10.0`.
- **Observation:** Replacing the sample counter with a local `Open Codex` command compiled and loaded successfully. The command dispatches macOS `open -a Codex` and logs success/failure without claiming a Codex task result.
- **Evidence:** Build completed with 0 warnings and 0 errors; `CodexDeck.log` recorded `OpenCodexCommand` registration and `CodexDeck` reload.
- **Applicability:** This is a low-risk first integration boundary while official Codex local control capabilities remain under research.
- **Reusable recommendation:** Start external-app actions with an explicit local OS dispatch and truthful receipt logging; do not infer task creation, completion, or live status from a successful process launch.

## 20. Official Codex integration boundary (2026-09-16)

- **Environment:** Official OpenAI developer documentation reviewed during implementation planning.
- **Observation:** The public developer material documents Codex use cases, CLI/API integration patterns, and building tools Codex can use, but does not establish a supported local control or live-status API for the Codex Desktop app.
- **Evidence:** Official search and page review: [Codex use cases](https://developers.openai.com/codex/use-cases) and [OpenAI CLI reference](https://developers.openai.com/api/reference/cli). Neither documents a desktop-app command/status protocol suitable for a third-party Loupedeck plugin.
- **Applicability:** Do not build `LiveCodexAdapter`, task scraping, or task mutation around undocumented app internals. Shortcut mode remains the complete supported baseline.
- **Reusable recommendation:** Require an official, permissioned local interface before claiming live task state or task-aware actions; otherwise expose only clearly labeled OS-level actions and truthful dispatch receipts.

## 21. Icon source rule (2026-09-16)

- **Environment:** Project-wide asset policy.
- **Observation:** Custom action imagery may be needed for fast hardware scanning, but the plugin should avoid inventing a bespoke icon language.
- **Evidence:** The official [Tabler Icons repository](https://github.com/tabler/tabler-icons) provides a large SVG set under the MIT license; icons use a consistent 24×24, 2px-stroke base.
- **Applicability:** Use Tabler only when an icon materially improves recognition or state communication. Text labels and native/default SDK imagery remain preferable when sufficient.
- **Reusable recommendation:** Prefer official Tabler SVG/PNG assets, preserve the license notice when assets are redistributed, document icon name/source/version, and provide non-color/text feedback for accessibility.

## 22. Codex Micro capability distinction (2026-09-16)

- **Environment:** Public Work Louder Codex Micro product documentation, compared with the third-party Loupedeck plugin boundary.
- **Observation:** Codex Micro is directly integrated into Codex and advertises live agent-state keys plus native commands such as accept/reject, push-to-talk, new chats, and custom actions. That integration is not evidence of a public API available to external plugins.
- **Evidence:** [Work Louder Codex Micro](https://worklouder.cc/codex-micro) explicitly describes dynamically state-reflecting Agent Keys and Codex-native Command Keys.
- **Applicability:** “Blocked” applies to our independently developed Loupedeck plugin’s access to those capabilities, not to the official Codex Micro product.
- **Reusable recommendation:** Treat first-party hardware integrations as capability references and UX targets; do not assume their private bridge/protocol is supported for third-party hardware. Revisit only when an official external integration surface is documented.

## 23. Community adaptation boundary (2026-09-16)

- **Environment:** Community implementations reviewed: Microbridge, Codex Micro Phone, Codex Micro Light, WorkLouderCTL, and BetterTouchTool Work Louder support.
- **Observation:** Reusable value is concentrated in capability gating, privacy-safe state projection, focus ownership, event-driven watchers/backoff, and shortcut compatibility diagnostics. HID framing, `node-hid` shims, and Work Louder LED commands are device-specific.
- **Evidence:** Microbridge separates integrations from the device renderer and advertises only supported actions; the phone project validates shortcut compatibility and projects generic slot state; BetterTouchTool confirms Codex Micro device exclusivity; Micro Light explicitly labels its HID path experimental.
- **Applicability:** The architectural patterns can improve the Loupedeck companion; the device-control code cannot be transplanted to Logitech Actions SDK.
- **Reusable recommendation:** Port concepts through interfaces and tests, never by copying undocumented HID/process-injection code into a production plugin. Keep device-specific reverse engineering isolated, opt-in, and reversible.

## 24. Dependency-free contract fixtures (2026-09-16)

- **Environment:** Repository shared-contract layer; Python 3 standard library.
- **Observation:** Early protocol validation can run without adding a package manager, network dependency, or companion runtime.
- **Evidence:** `shared/validate_fixtures.py` validates accepted and not-focused receipt fixtures successfully with `python3`.
- **Applicability:** Useful for initial schema/fixture work and CI smoke checks; it does not replace JSON Schema validation or transport tests.
- **Reusable recommendation:** Start contract work with deterministic standard-library fixture checks, then add stronger schema and transport tooling only when the protocol surface warrants it.

## 25. Loopback mock bridge smoke test (2026-09-16)

- **Environment:** Repository shared-contract layer; Python 3 standard library.
- **Observation:** A small newline-delimited JSON loopback server can exercise hello, shortcut snapshot, accepted intent, and unsupported-message paths without involving Loupedeck or Codex.
- **Evidence:** `shared/mock_bridge.py` passed syntax checks and direct response assertions for hello, action intent, and unsupported messages.
- **Applicability:** Suitable for deterministic contract and plugin-client development before a real companion exists; not a production transport.
- **Reusable recommendation:** Keep a local mock bridge as the first integration target so UI/action code can be tested without external app state or hardware.

## 26. Token-gated mock handshake (2026-09-16)

- **Environment:** Repository shared-contract layer; Python 3 standard library.
- **Observation:** The mock bridge can require an installation-scoped token before returning hello/snapshot or action receipts, while remaining easy to exercise in tests.
- **Evidence:** `shared/test_protocol.py` covers accepted and rejected token paths plus snapshot shape; four smoke tests pass.
- **Applicability:** This is a test seam, not production authentication. A real companion must use secure token provisioning and loopback-only binding.
- **Reusable recommendation:** Test unauthorized behavior before wiring plugin actions to any bridge; never make an unauthenticated local endpoint the default production contract.

## 27. Typed protocol contracts build cleanly on net10 (2026-09-16)

- **Environment:** .NET SDK 10.0.401, target `net10.0`.
- **Observation:** The shared protocol can be represented by small nullable C# records and shared `System.Text.Json` options without adding external packages.
- **Evidence:** `shared/CodexDeck.Protocol/CodexDeck.Protocol.csproj` builds with 0 warnings and 0 errors.
- **Applicability:** Provides a stable contract layer for the future companion and plugin; it is independent of the Loupedeck SDK.
- **Reusable recommendation:** Keep protocol records in a standalone target-compatible project and serialize through one shared options instance to avoid drift between transport clients.

## 28. Shortcut adapter safety boundary (2026-09-17)

- **Environment:** .NET SDK 10.0.401, target `net10.0`, shared protocol project.
- **Observation:** Shortcut dispatch can be isolated behind `IShortcutDispatcher` and `IFocusGuard`; unsupported actions, failed dispatch, and unknown Codex focus become explicit receipt states.
- **Evidence:** `ShortcutAdapter` builds cleanly and probes only dispatcher-advertised actions. `accept`, `reject`, and `interrupt` return `not_focused` before dispatch when the guard is false.
- **Applicability:** This boundary is portable across macOS/Windows dispatch implementations and avoids embedding OS automation in the Loupedeck plugin.
- **Reusable recommendation:** Keep focus-sensitive actions behind a guard and return truthful receipts; never equate a generated key event with Codex completion.

## 29. Deterministic adapter tests without external packages (2026-09-17)

- **Environment:** .NET SDK 10.0.401, target `net10.0`.
- **Observation:** The adapter’s safety behavior can be tested with a tiny executable test project and fakes, avoiding a package restore or host/UI dependency.
- **Evidence:** `shared/CodexDeck.Protocol.Tests` passes focus-blocking, accepted-dispatch, and unsupported-action assertions.
- **Applicability:** Appropriate for protocol/adapter unit coverage; not a substitute for OS permission and physical-device tests.
- **Reusable recommendation:** Keep fake dispatcher/focus tests alongside the contract project so safety regressions are caught before platform automation is introduced.

## 30. Plugin icon asset handling (2026-09-17)

- **Environment:** Generated Logi Actions package on macOS; package metadata contains `Icon256x256.png`; project assets include OpenAI PNG/SVG artwork.
- **Observation:** The package metadata path expects a 256×256 PNG for the plugin identity icon, while the SVG is a better source asset for recolorable/action imagery.
- **Evidence:** Generated package contains `src/package/metadata/Icon256x256.png`; supplied `assets/openai.png` is 256×256 RGBA; `assets/openai.svg` now uses `currentColor` instead of a fixed black stroke.
- **Applicability:** Use the PNG for package identity where the host requires it, and retain the recolorable SVG as the source for future action images if the SDK/resource path supports SVG.
- **Reusable recommendation:** Keep source SVG and host-required raster derivatives together; never edit a raster derivative without updating the source asset and recording the conversion.

## 31. OpenAI package icon verified in host (2026-09-17)

- **Environment:** Loupedeck app with connected Loupedeck Live; CodexDeck development link rebuilt after replacing package metadata icon.
- **Observation:** The supplied OpenAI icon appears in the CodexDeck plugin rail and the plugin installation completes successfully. The action browser shows `Open Codex` under Commands.
- **Evidence:** UI displayed the OpenAI knot icon for the selected CodexDeck rail entry and reported “Plugin installation completed”; build completed with 0 warnings and 0 errors.
- **Applicability:** Confirms the 256×256 PNG derivative is accepted by the current host while the recolorable SVG remains available as source imagery.
- **Reusable recommendation:** Verify package identity icons in the host UI after installation; file validity alone does not prove the asset is used by the plugin browser.

## 32. Plugin references shared protocol assembly (2026-09-17)

- **Environment:** .NET SDK 10.0.401; CodexDeck plugin and shared protocol both target `net10.0`; Logi Plugin Service 6.4.1.3246.
- **Observation:** Adding a project reference lets the plugin build against the typed protocol contracts without duplicating models; the protocol DLL is copied beside the plugin DLL in the development output.
- **Evidence:** Plugin solution build completed with 0 warnings and 0 errors; output contains `CodexDeck.Protocol.dll`; development link/reload completed.
- **Applicability:** Establishes the intended plugin/companion contract boundary; runtime use should be added incrementally and tested against the host.
- **Reusable recommendation:** Reference one shared contract assembly rather than copying protocol records into plugin and companion projects; verify dependency copy/load before wiring actions.

## 33. First action wired through shared receipt contract (2026-09-17)

- **Environment:** .NET SDK 10.0.401; Logi Plugin Service 6.4.1.3246; macOS development link.
- **Observation:** `Open Codex` now uses `ShortcutAdapter` and `ActionIntent`/`ActionReceipt` instead of launching the process directly from the action callback.
- **Evidence:** Plugin solution rebuild and reload completed with 0 warnings and 0 errors.
- **Applicability:** Establishes the intended thin-plugin pattern: SDK action translates input, shared adapter dispatches, and the log records a truthful receipt.
- **Reusable recommendation:** Route every production action through the shared adapter boundary before adding more commands; keep OS-specific dispatchers replaceable and testable.

## 34. Codex bundle identity and guarded interrupt (2026-09-17)

- **Environment:** Installed `/Applications/ChatGPT.app`; macOS bundle metadata; Logi plugin target `net10.0`.
- **Observation:** The installed Codex desktop app identifies itself as bundle `com.openai.codex`. A guarded `Interrupt Codex` action can check the frontmost process before sending Escape via System Events.
- **Evidence:** `Info.plist` reports `CFBundleIdentifier = com.openai.codex`; plugin rebuild/reload completed with 0 warnings and 0 errors.
- **Applicability:** macOS-only implementation; `osascript` requires Accessibility permission and must fail closed when focus cannot be verified.
- **Reusable recommendation:** Resolve and record the actual app bundle identifier before implementing focus-sensitive shortcuts; return `not_focused` or permission failure instead of dispatching blindly.

## 35. Accessibility denial must fail closed (2026-09-17)

- **Environment:** macOS System Events; current desktop session without granted Accessibility control for the test process.
- **Observation:** Frontmost-process inspection through `osascript` failed with macOS error `-10827` when permission was unavailable.
- **Evidence:** `osascript -e 'tell application "System Events" to get bundle identifier of first process whose frontmost is true'` exited 1 with `-10827`; hardened plugin guard now catches this and returns false.
- **Applicability:** Any macOS action using System Events/focus inspection; permission behavior varies by signed host/process identity.
- **Reusable recommendation:** Treat Accessibility denial, timeout, and script failure as unknown focus and fail closed; never let an automation exception escape the Loupedeck action callback.

## 36. Guarded Interrupt action surfaced in host (2026-09-17)

- **Environment:** Loupedeck app with connected Loupedeck Live; CodexDeck development link reloaded.
- **Observation:** The refreshed action browser exposes both `Interrupt Codex` and `Open Codex` under Commands, alongside the OpenAI plugin icon.
- **Evidence:** Host UI displayed a successful plugin-installation notice and listed both command rows after reload.
- **Applicability:** Confirms action registration and packaging; does not yet prove Accessibility-approved dispatch on hardware.
- **Reusable recommendation:** Treat action discovery as a separate acceptance step from physical execution and permission validation.

## 37. Nullable shortcut binding defaults (2026-09-17)

- **Environment:** Shared protocol project targeting `net10.0`.
- **Observation:** Actions can be represented with stable IDs and nullable keystrokes, allowing setup to distinguish “not configured” from an assumed shortcut.
- **Evidence:** `ShortcutBindings.cs` defines stable action IDs, disabled/unconfigured defaults, and bounded validation; protocol build and adapter tests pass.
- **Applicability:** Prevents undocumented Codex shortcut assumptions across OSes and app versions.
- **Reusable recommendation:** Persist stable action IDs, validate user-provided keystrokes on Save, and keep unconfigured actions unavailable rather than guessing defaults.

## 38. Validated atomic binding store (2026-09-17)

- **Environment:** .NET SDK 10.0.401; shared protocol project targeting `net10.0`.
- **Observation:** A small local store can reject invalid/empty binding sets, write through a temporary file, and recover to defaults on missing, malformed, or unreadable configuration.
- **Evidence:** `ShortcutBindingStore` round-trip and malformed-JSON recovery assertions pass in the dependency-free C# test executable.
- **Applicability:** General local plugin/companion configuration; backup/export and schema migration are still required for release.
- **Reusable recommendation:** Validate before Save, use atomic replacement, and fail safe to defaults without silently persisting corrupted state.

## 39. Automatic binding backup/export (2026-09-17)

- **Environment:** .NET SDK 10.0.401; shared protocol project targeting `net10.0`.
- **Observation:** The binding store now preserves the previous file as `.bak` before replacement and can export a validated current configuration to a separate destination.
- **Evidence:** C# smoke tests verify backup creation and export round-trip.
- **Applicability:** Useful for user-editable plugin profiles and recovery workflows; retention policy and schema migrations remain open.
- **Reusable recommendation:** Make backup creation part of the normal Save path and keep export explicit so users can recover without editing internal files.

## 40. Binding validation edge cases (2026-09-17)

- **Environment:** .NET SDK 10.0.401; shared protocol test executable.
- **Observation:** Explicit tests now reject action IDs containing spaces and keystrokes longer than the configured bound while accepting unconfigured stable defaults.
- **Evidence:** C# smoke test passes valid-default, invalid-ID, and oversized-keystroke assertions.
- **Applicability:** Applies to any user-editable action mapping; exact keystroke grammar remains platform-specific and should be validated by the OS dispatcher later.
- **Reusable recommendation:** Separate platform-neutral structural validation from platform-specific shortcut parsing; reject malformed input before persistence.

## 41. Versioned config with legacy load (2026-09-17)

- **Environment:** .NET SDK 10.0.401; shared protocol project targeting `net10.0`.
- **Observation:** The binding store now writes a schema-versioned document while continuing to read the earlier bare-list format.
- **Evidence:** Existing round-trip, export, malformed-config, and validation tests pass after the format change.
- **Applicability:** Establishes a migration seam before user profiles exist; future schema versions still require explicit migration code.
- **Reusable recommendation:** Version persisted configuration from the first release and retain a narrow legacy reader so format evolution does not strand early adopters.

## 42. Future config versions fail safe (2026-09-17)

- **Environment:** .NET SDK 10.0.401; shared protocol test executable.
- **Observation:** Binding files with an unknown schema version are rejected and replaced in memory with defaults rather than partially interpreted.
- **Evidence:** Smoke tests cover schema version 99 recovery and continue to pass.
- **Applicability:** Any persisted plugin/companion configuration that may outlive the current binary.
- **Reusable recommendation:** Gate deserialization on an explicitly supported schema version and require a deliberate migration path for newer files.

## 43. Backup-first configuration recovery (2026-09-17)

- **Environment:** .NET SDK 10.0.401; shared protocol test executable.
- **Observation:** A corrupted primary binding file now falls back to its validated `.bak` before using defaults.
- **Evidence:** Smoke test corrupts the primary after two saves and recovers the prior valid binding from backup.
- **Applicability:** Local user configuration where a failed write or partial edit should be recoverable without data loss.
- **Reusable recommendation:** Prefer validated backup recovery over silently discarding user configuration; expose defaults only when both primary and backup are unusable.

## 44. Explicit legacy-format detection (2026-09-17)

- **Environment:** .NET SDK 10.0.401; shared protocol test executable.
- **Observation:** Versioned-object deserialization can throw on the older bare-array format; inspecting the JSON envelope before deserialization provides a reliable compatibility branch.
- **Evidence:** Legacy-list test initially failed, then passed after `ShortcutBindingStore` added explicit array detection.
- **Applicability:** Any migration from a collection root to a versioned document root.
- **Reusable recommendation:** Detect legacy envelope shape before deserializing into the new document type; do not rely on a failed deserialize as the migration mechanism.

## 45. Loopback companion skeleton (2026-09-17)

- **Environment:** .NET SDK 10.0.401; companion and protocol target `net10.0`.
- **Observation:** A minimal companion can bind explicitly to `IPAddress.Loopback`, answer a hello envelope, and reject unsupported messages without exposing a network interface.
- **Evidence:** `companion/CodexDeck.Companion` builds with 0 warnings and 0 errors.
- **Applicability:** Initial transport seam only; authentication, reconnect behavior, and production lifecycle management are not complete.
- **Reusable recommendation:** Bind development companions to loopback from the first line of code; add authentication and reconnect tests before allowing any non-test client.

## 46. Companion token gate (2026-09-17)

- **Environment:** .NET SDK 10.0.401; loopback companion targeting `net10.0`.
- **Observation:** The companion now supports an installation-scoped token gate through `CODEX_DECK_TOKEN`; mismatched or missing tokens receive an `unauthorized` response.
- **Evidence:** Companion rebuild succeeds with 0 warnings and 0 errors.
- **Applicability:** Development security seam; environment-variable provisioning is temporary and not the final pairing UX.
- **Reusable recommendation:** Make authentication a required production configuration, while keeping token provisioning replaceable by a pairing flow rather than embedding secrets in plugin code.

## 47. Companion action receipt loop (2026-09-17)

- **Environment:** .NET SDK 10.0.401; loopback companion targeting `net10.0`.
- **Observation:** Authenticated `action.intent` envelopes now receive an `action.receipt` response with the original request ID and `accepted` status.
- **Evidence:** Companion rebuild succeeds with 0 warnings and 0 errors.
- **Applicability:** Basic transport seam only; the skeleton does not yet execute OS shortcuts or verify completion.
- **Reusable recommendation:** Preserve request IDs end-to-end and keep receipt acceptance distinct from underlying Codex completion.

## 48. Explicit development transport boundary (2026-09-17)

- **Environment:** .NET 10 companion skeleton on macOS.
- **Observation:** A concise README documenting loopback binding, token provisioning, envelope behavior, and non-production status prevents developers from mistaking the skeleton for a persistent daemon.
- **Evidence:** `companion/CodexDeck.Companion/README.md` now records the exact run command and pending lifecycle work.
- **Applicability:** Any early local bridge before pairing, reconnect, and service management are complete.
- **Reusable recommendation:** Document development-only transport limitations next to the executable project and state which production guarantees are not yet present.

## 49. Persistent envelope loop (2026-09-17)

- **Environment:** .NET SDK 10.0.401; loopback companion targeting `net10.0`.
- **Observation:** The companion now reads multiple newline-delimited envelopes on one connection instead of closing after the first request.
- **Evidence:** Companion rebuild succeeds with 0 warnings and 0 errors.
- **Applicability:** Required for realistic hello → snapshot → action exchanges and reconnect behavior; idle timeouts and backpressure remain.
- **Reusable recommendation:** Keep the transport connection alive for a request sequence, but bound lifecycle with explicit cancellation/idle policies before production use.

## 50. Typed loopback client boundary (2026-09-17)

- **Environment:** .NET SDK 10.0.401; protocol project targeting `net10.0`.
- **Observation:** A small `LoopbackClient` centralizes 127.0.0.1 connection, token-bearing hello, newline framing, cancellation, and disconnect errors.
- **Evidence:** Protocol project builds with 0 warnings and 0 errors.
- **Applicability:** Shared by plugin/companion integration tests; production pairing and reconnect policy still need explicit implementation.
- **Reusable recommendation:** Keep socket setup and framing in one typed client so plugin actions never manage raw streams or silently ignore disconnects.

## 51. Typed intent send/receipt parsing (2026-09-17)

- **Environment:** .NET SDK 10.0.401; protocol project targeting `net10.0`.
- **Observation:** `LoopbackClient.SendIntentAsync` now serializes an `ActionIntent`, waits for one response, and parses an `ActionReceipt` through shared JSON options.
- **Evidence:** Protocol project builds with 0 warnings and 0 errors.
- **Applicability:** Basic request/receipt integration; correlation, timeouts, and receipt-type validation remain for production.
- **Reusable recommendation:** Keep intent serialization and receipt parsing in the transport client, with explicit cancellation and future correlation checks.

## 52. Receipt correlation validation (2026-09-17)

- **Environment:** .NET SDK 10.0.401; protocol project targeting `net10.0`.
- **Observation:** The loopback client now rejects responses that are not `action.receipt` or whose ID does not match the sent intent.
- **Evidence:** Protocol project builds with 0 warnings and 0 errors.
- **Applicability:** Prevents stale or cross-request responses from being presented as current action results.
- **Reusable recommendation:** Validate response type and request correlation at the transport boundary before updating UI or state reducers.

## 53. Loopback integration requires socket permission (2026-09-17)

- **Environment:** macOS sandboxed development runner; .NET 10 loopback test.
- **Observation:** In-process TCP integration tests are valid but may require elevated execution permission when the environment denies local socket bind.
- **Evidence:** Initial test run failed with `SocketException (13): Permission denied` at `TcpListener.Start`; the same test passed with narrowly scoped elevated permission.
- **Applicability:** Local CI/dev environments with network sandboxing; production loopback behavior is unchanged.
- **Reusable recommendation:** Keep transport tests in-process and loopback-only, and document the minimal socket permission needed to run them rather than weakening the test or binding externally.

## 54. Generated local pairing token (2026-09-17)

- **Environment:** .NET 10 companion on macOS.
- **Observation:** The companion now generates a cryptographically random 32-byte token and persists it under the user application-support directory when `CODEX_DECK_TOKEN` is not supplied.
- **Evidence:** `PairingTokenStore` and companion build pass with 0 warnings and 0 errors.
- **Applicability:** Development pairing seam; secure keychain storage, rotation, and user-visible pairing UX remain.
- **Reusable recommendation:** Generate secrets locally, keep environment override for testing, and replace plaintext storage with OS keychain before release.

## 55. Pairing token file permissions (2026-09-17)

- **Environment:** .NET SDK 10.0.401; macOS companion targeting `net10.0`.
- **Observation:** Newly generated token files are restricted to user read/write permissions on Unix hosts before being moved into place.
- **Evidence:** `PairingTokenStore` builds cleanly with `File.SetUnixFileMode` applied to the temporary token file.
- **Applicability:** Unix/macOS development and release hosts; Windows ACL handling remains platform-specific.
- **Reusable recommendation:** Apply restrictive permissions before atomically moving local secrets into their final path; use the OS keychain for production credentials.

## 56. Bounded reconnect backoff (2026-09-17)

- **Environment:** .NET SDK 10.0.401; shared protocol project targeting `net10.0`.
- **Observation:** `LoopbackClient` now exposes bounded reconnect attempts with cancellation and increasing short delays.
- **Evidence:** Protocol project builds with 0 warnings and 0 errors.
- **Applicability:** Companion startup/reconnect paths; production should recreate the socket client after a failed connection and add jitter/telemetry.
- **Reusable recommendation:** Bound retries and honor cancellation; never spin indefinitely or hide a disconnected companion from the user.

## 57. Fresh socket per reconnect attempt (2026-09-17)

- **Environment:** .NET SDK 10.0.401; shared loopback client targeting `net10.0`.
- **Observation:** A failed `TcpClient.ConnectAsync` should not be retried on the same socket instance; reconnect now disposes and recreates the client before the next attempt.
- **Evidence:** Protocol project builds with 0 warnings and 0 errors.
- **Applicability:** Any reconnecting TCP client; exact socket behavior can vary by runtime/platform.
- **Reusable recommendation:** Recreate failed connection objects between attempts to avoid carrying a poisoned socket state into retry logic.

## 58. Complete hello snapshot (2026-09-17)

- **Environment:** .NET SDK 10.0.401; companion targeting `net10.0`.
- **Observation:** Companion hello acknowledgement now includes a complete shortcut-mode state snapshot with capabilities, timestamp, and action states rather than only a mode marker.
- **Evidence:** Companion rebuild succeeds with 0 warnings and 0 errors.
- **Applicability:** Enables deterministic plugin initialization and reconnect rendering; snapshot validation against JSON Schema remains.
- **Reusable recommendation:** Send a complete snapshot on every successful hello/reconnect before incremental events so clients never initialize from partial state.

## 59. Configurable loopback port (2026-09-17)

- **Environment:** .NET 10 companion on macOS.
- **Observation:** The development companion now accepts `CODEX_DECK_PORT` with range validation while defaulting to an ephemeral loopback port.
- **Evidence:** Companion build succeeds with 0 warnings and 0 errors.
- **Applicability:** Development discovery and integration tests; production should reserve/configure a port through pairing rather than rely on an environment variable.
- **Reusable recommendation:** Keep ephemeral ports as the safe default and make fixed-port overrides explicit, validated, and loopback-only.

## 60. Typed hello snapshot helper (2026-09-17)

- **Environment:** .NET SDK 10.0.401; shared protocol project targeting `net10.0`.
- **Observation:** `LoopbackClient.ReadHelloSnapshotAsync` validates that the hello response contains a snapshot and deserializes it into `StateSnapshot`.
- **Evidence:** Protocol project builds with 0 warnings and 0 errors.
- **Applicability:** Plugin/companion initialization and reconnect paths; malformed snapshot and version checks remain transport-test work.
- **Reusable recommendation:** Keep envelope parsing in the client and expose typed initialization methods so UI code never reaches into raw JSON.

## 61. Snapshot protocol-version gate (2026-09-17)

- **Environment:** .NET SDK 10.0.401; shared protocol project targeting `net10.0`.
- **Observation:** Typed snapshot initialization now rejects missing or unsupported `protocolVersion` values before deserialization.
- **Evidence:** Protocol project builds with 0 warnings and 0 errors.
- **Applicability:** Any plugin/companion handshake where schema evolution must fail explicitly.
- **Reusable recommendation:** Validate envelope/version compatibility before accepting state; do not silently deserialize newer snapshots into older models.

## 62. End-to-end typed snapshot test (2026-09-17)

- **Environment:** .NET SDK 10.0.401; in-process loopback integration test.
- **Observation:** The integration test now consumes the hello response through `ReadHelloSnapshotAsync` and asserts shortcut-mode state before sending an intent.
- **Evidence:** Elevated loopback test passes: `Validated ShortcutAdapter, binding store, and loopback client integration`.
- **Applicability:** Verifies the intended initialization sequence without Loupedeck or Codex runtime dependencies.
- **Reusable recommendation:** Exercise typed handshake helpers in an in-process transport test before wiring real UI state reducers.

## 63. Companion intent shape validation (2026-09-17)

- **Environment:** .NET SDK 10.0.401; loopback companion targeting `net10.0`.
- **Observation:** The companion now requires string `id` and `action` fields before returning an accepted action receipt.
- **Evidence:** Companion rebuild succeeds with 0 warnings and 0 errors.
- **Applicability:** Prevents malformed or incomplete client messages from being represented as successful dispatches.
- **Reusable recommendation:** Validate required intent fields at the transport boundary and return an explicit invalid-message error before adapter dispatch.

## 64. Centralized protocol status constants (2026-09-17)

- **Environment:** .NET SDK 10.0.401; shared protocol project targeting `net10.0`.
- **Observation:** Receipt statuses and common reasons are now centralized in `ProtocolConstants`, reducing string drift between adapter and transport code.
- **Evidence:** Adapter/integration tests pass; protocol build remains clean.
- **Applicability:** All C# protocol consumers; wire values remain stable strings for JSON compatibility.
- **Reusable recommendation:** Centralize discriminant/status values while preserving their serialized wire representation.

## 65. Typed companion snapshot serialization (2026-09-17)

- **Environment:** .NET SDK 10.0.401; companion/protocol target `net10.0`.
- **Observation:** The companion now constructs `StateSnapshot` and `HelloAck` records and serializes them with shared JSON options instead of hand-built snapshot strings.
- **Evidence:** Companion build succeeds with 0 warnings and 0 errors.
- **Applicability:** Reduces schema drift between companion and client; transport schema validation remains required.
- **Reusable recommendation:** Serialize typed protocol records at the producer boundary; avoid interpolated JSON for state-bearing messages.

## 66. Reconnect restores snapshot atomically (2026-09-17)

- **Environment:** .NET SDK 10.0.401; shared loopback client targeting `net10.0`.
- **Observation:** `ConnectAndReadSnapshotAsync` combines bounded connect/retry with typed hello-snapshot restoration, giving callers one initialization operation.
- **Evidence:** Protocol project builds with 0 warnings and 0 errors.
- **Applicability:** Plugin reconnect and startup state restoration; caller still owns stale-state clearing and UI rendering.
- **Reusable recommendation:** Treat reconnect as incomplete until a fresh snapshot is received; expose one helper that makes this invariant hard to skip.

## 67. Monotonic snapshot reducer (2026-09-17)

- **Environment:** .NET SDK 10.0.401; shared protocol project targeting `net10.0`.
- **Observation:** `StateSnapshotReducer` accepts only current-version snapshots newer than the current timestamp and rejects older state.
- **Evidence:** Protocol integration test applies a current snapshot and rejects an older snapshot.
- **Applicability:** Reconnect and event-driven UI state; equal-timestamp tie-breaking and sequence numbers remain future work.
- **Reusable recommendation:** Enforce monotonic state updates at the reducer boundary so transport reordering cannot regress rendered status.

## 68. Equal timestamps are not an ordering signal (2026-09-17)

- **Environment:** .NET SDK 10.0.401; shared snapshot reducer.
- **Observation:** Snapshots with equal timestamps are now rejected rather than allowed to overwrite state nondeterministically.
- **Evidence:** Reducer builds cleanly after changing the comparison to `<=`; existing tests pass.
- **Applicability:** Timestamp-only ordering until transport sequence numbers are implemented.
- **Reusable recommendation:** Treat equal timestamps as ambiguous and require a stronger sequence/tie-breaker before replacing state.

## 69. Explicit snapshot freshness check (2026-09-17)

- **Environment:** .NET SDK 10.0.401; shared snapshot reducer.
- **Observation:** Reducer exposes `IsStale(now, maxAge)` and treats missing state as stale.
- **Evidence:** Integration tests cover fresh and over-threshold snapshots.
- **Applicability:** UI status rendering and reconnect diagnostics; production thresholds should follow the protocol policy.
- **Reusable recommendation:** Make freshness a named reducer query and render stale state explicitly instead of treating absent updates as success.

## 70. Disconnect clears rendered state (2026-09-17)

- **Environment:** .NET SDK 10.0.401; shared snapshot reducer.
- **Observation:** `StateSnapshotReducer.Clear()` removes the current snapshot so disconnect handling cannot leave stale state looking live.
- **Evidence:** Integration tests assert `Current` is null and `IsStale` is true after clear.
- **Applicability:** Companion disconnect/reconnect and plugin rendering lifecycle.
- **Reusable recommendation:** Clear state explicitly on disconnect, then require a fresh snapshot before re-enabling live indicators or task-aware controls.

## 71. Centralized freshness policy (2026-09-17)

- **Environment:** .NET SDK 10.0.401; shared protocol project targeting `net10.0`.
- **Observation:** Snapshot stale (10s) and unknown (30s) thresholds are now named protocol constants.
- **Evidence:** Protocol project builds with 0 warnings and 0 errors.
- **Applicability:** Shared reducer/rendering policy; UI copy and heartbeat implementation remain pending.
- **Reusable recommendation:** Centralize freshness thresholds to prevent companion and plugin from disagreeing about status age.

## 72. Named freshness states (2026-09-17)

- **Environment:** .NET SDK 10.0.401; shared snapshot reducer.
- **Observation:** Reducer now exposes `fresh`, `stale`, `unknown`, and `disconnected` states based on shared thresholds.
- **Evidence:** Integration tests cover all four outcomes.
- **Applicability:** Rendering and diagnostics state selection; event sequence ordering remains future work.
- **Reusable recommendation:** Expose named freshness states from one reducer rather than duplicating age comparisons in each control.

## 73. Snapshot action-status validation (2026-09-17)

- **Environment:** .NET SDK 10.0.401; shared snapshot reducer.
- **Observation:** Reducer rejects snapshots containing action statuses outside the protocol lifecycle.
- **Evidence:** Integration tests cover rejection of a `bogus` action status.
- **Applicability:** Protects plugin rendering and capability state from malformed companion payloads.
- **Reusable recommendation:** Validate enumerated state values at the reducer boundary before exposing them to hardware controls.

## 74. Capability/state consistency (2026-09-17)

- **Environment:** .NET SDK 10.0.401; shared snapshot reducer.
- **Observation:** Enabled actions must appear in the snapshot capability list; otherwise the snapshot is rejected.
- **Evidence:** Integration tests cover an enabled but unadvertised action and reject it.
- **Applicability:** Prevents UI from offering controls the companion cannot dispatch.
- **Reusable recommendation:** Require capability advertisement for every enabled action and render unadvertised actions as unavailable.

## 75. Correlated receipt reducer (2026-09-17)

- **Environment:** .NET SDK 10.0.401; shared protocol project targeting `net10.0`.
- **Observation:** `ActionReceiptReducer` tracks intents in `sending`, accepts only known receipt statuses for matching IDs, and rejects unknown receipts.
- **Evidence:** Integration tests cover matching and unknown receipt IDs.
- **Applicability:** Hardware action feedback and UI state; persistence/history are intentionally out of scope.
- **Reusable recommendation:** Track pending intent IDs explicitly and never let an unsolicited or mismatched receipt update visible action state.

## 76. Receipt protocol-version gate (2026-09-17)

- **Environment:** .NET SDK 10.0.401; shared receipt reducer.
- **Observation:** Receipt reduction now rejects statuses from unsupported protocol versions before correlation/state updates.
- **Evidence:** Integration tests cover version 99 rejection.
- **Applicability:** Mixed-version plugin/companion upgrades and stale queued responses.
- **Reusable recommendation:** Enforce protocol compatibility at both transport and reducer boundaries.

## 77. Textual action feedback mapping (2026-09-17)

- **Environment:** .NET SDK 10.0.401; shared protocol project.
- **Observation:** `ActionViewStateMapper` maps accepted/failed/unavailable/sending states to enabled state and text feedback, keeping state communication independent of color.
- **Evidence:** Integration tests cover unavailable disabling and accepted feedback.
- **Applicability:** Loupedeck controls and accessibility rendering; final icon/animation treatment remains host-specific.
- **Reusable recommendation:** Derive UI state from protocol status through one mapper and include a non-color feedback string for every state.

## 78. Shared accessibility preferences (2026-09-17)

- **Environment:** .NET SDK 10.0.401; shared protocol project.
- **Observation:** Reduced motion, high contrast, and text feedback are represented as shared preferences with text feedback enabled by default.
- **Evidence:** Integration tests assert reduced-motion/high-contrast values do not disable text feedback.
- **Applicability:** Plugin and companion settings; platform-specific rendering and persistence remain.
- **Reusable recommendation:** Keep accessibility preferences in the shared model and preserve at least one non-visual/text cue regardless of visual theme.

## 79. Unknown UI status fails closed (2026-09-17)

- **Environment:** .NET SDK 10.0.401; shared action view-state mapper.
- **Observation:** Unrecognized action statuses now map to disabled/unavailable rather than idle/ready.
- **Evidence:** Integration tests assert an `unknown` status is disabled.
- **Applicability:** Protects controls from malformed or newer status values during upgrades.
- **Reusable recommendation:** Unknown state should remove activation affordance and use explicit unavailable feedback until capability is understood.

## 80. Complete lifecycle UI mapping (2026-09-17)

- **Environment:** .NET SDK 10.0.401; shared action view-state mapper.
- **Observation:** Valid `running`, `succeeded`, and `cancelled` statuses now receive explicit feedback and activation semantics; only unknown values fail closed.
- **Evidence:** Integration tests cover succeeded mapping and existing lifecycle paths.
- **Applicability:** Receipt/state rendering; exact hardware animation remains host-specific.
- **Reusable recommendation:** Enumerate every valid lifecycle state in the UI mapper and reserve the fail-closed branch for genuinely unknown values.

## 81. Receipt feedback rendered on hardware action (2026-09-17)

- **Environment:** Loupedeck plugin, Logi Plugin Service 6.4.1.3246, .NET 10.
- **Observation:** `Open Codex` now stores its latest receipt status, refreshes its action image, and displays text feedback (`Ready`, `Accepted`, etc.) on the action surface.
- **Evidence:** Plugin solution rebuild/reload completed with 0 warnings and 0 errors.
- **Applicability:** Demonstrates thin-plugin rendering from shared state; richer bridge snapshots still need client wiring.
- **Reusable recommendation:** Refresh dynamic action imagery/display only from reducer/view-state outcomes and include a text cue for every status.

## 82. Consistent feedback across actions (2026-09-17)

- **Environment:** Loupedeck plugin, Logi Plugin Service 6.4.1.3246, .NET 10.
- **Observation:** `Interrupt Codex` now uses the same shared view-state mapping and dynamic display refresh as `Open Codex`.
- **Evidence:** Plugin solution rebuild/reload completed with 0 warnings and 0 errors.
- **Applicability:** All future actions that expose receipts or lifecycle state.
- **Reusable recommendation:** Standardize action feedback through one mapper and refresh mechanism rather than action-specific status formatting.

## 83. Host action discovery after reload (2026-09-17)

- **Environment:** Loupedeck app with connected Loupedeck Live; CodexDeck development link.
- **Observation:** After reload, the host action browser lists `Interrupt Codex` and `Open Codex` under Commands and shows the plugin installation-completed notice.
- **Evidence:** Accessibility tree and UI screenshot show both rows under CodexDeck → Commands.
- **Applicability:** Confirms registration/discovery only; physical execution and Accessibility permission remain separate tests.
- **Reusable recommendation:** Capture host action-browser evidence after each reload when adding or renaming actions.

## 84. Interrupt unavailable when Codex is not frontmost (2026-09-17)

- **Environment:** macOS Loupedeck host with CodexDeck loaded; current Logi Plugin Service log.
- **Observation:** `Interrupt Codex` changed from `Ready` to `Unavailable` after a hardware press because the focus guard reported Codex was not frontmost.
- **Evidence:** `CodexDeck.log` recorded `Interrupt Codex receipt: unavailable (not_focused)` at 01:22:20.
- **Applicability:** Expected safety behavior whenever Loupedeck, another app, or a permission-denied System Events query is frontmost.
- **Reusable recommendation:** Explain the focus requirement in setup/help text; do not auto-activate Codex or send Escape to an unknown foreground app.

## 85. Transient action feedback (2026-09-17)

- **Environment:** Loupedeck plugin, Logi Plugin Service 6.4.1.3246, .NET 10.
- **Observation:** `Interrupt Codex` now resets its displayed receipt state to `Ready` after three seconds, preventing an unavailable result from becoming a permanent-looking action state.
- **Evidence:** Plugin rebuild/reload completed with 0 warnings and 0 errors after adding a one-shot timer.
- **Applicability:** Short-lived hardware receipts; durable task state should remain reducer-driven when a live adapter exists.
- **Reusable recommendation:** Use transient feedback for one-shot dispatch outcomes and reserve persistent labels for verified state.

## 86. Consistent transient feedback across actions (2026-09-17)

- **Environment:** Loupedeck plugin, Logi Plugin Service 6.4.1.3246, .NET 10.
- **Observation:** `Open Codex` now resets its receipt display to `Ready` after three seconds, matching `Interrupt Codex`.
- **Evidence:** Plugin rebuild/reload completed with 0 warnings and 0 errors.
- **Applicability:** One-shot local actions; future bridge-backed actions should use reducer state instead.
- **Reusable recommendation:** Apply the same transient feedback policy to equivalent dispatch actions to avoid inconsistent operator expectations.

## 87. Actions SDK icon surfaces (2026-09-17)

- **Environment:** Logi Actions SDK documentation and current CodexDeck package.
- **Observation:** The SDK distinguishes package identity icons, action-picker symbols, and runtime button images. Package identity uses metadata raster assets; action symbols are small SVGs; runtime images are supplied through `GetCommandImage` and refreshed with `ActionImageChanged`.
- **Evidence:** [Action SDK plugin structure](https://logitech.github.io/actions-sdk-docs/csharp/tutorial/plugin-structure/) documents action symbols; [Change a Button Image](https://logitech.github.io/actions-sdk-docs/csharp/tutorial/change-a-button-image/) documents runtime image rendering.
- **Applicability:** Current Loupedeck/Logi host; exact resource naming should be verified when adding action symbols.
- **Reusable recommendation:** Keep the OpenAI PNG for package identity, use recolorable SVGs for action-picker symbols, and use `GetCommandImage` only when dynamic button imagery materially improves recognition.

## 88. Embedded runtime action image (2026-09-17)

- **Environment:** Loupedeck plugin, Logi Plugin Service 6.4.1.3246, .NET 10.
- **Observation:** The supplied OpenAI PNG is embedded from the shared assets folder and returned through `GetCommandImage` for `Open Codex`.
- **Evidence:** Plugin solution rebuild/reload completed with 0 warnings and 0 errors.
- **Applicability:** Current host/resource conventions; exact visual rendering still benefits from physical hardware review.
- **Reusable recommendation:** Link source assets into the project as embedded resources and resolve them by filename through `PluginResources`, then verify the packaged image on the host.

## 89. Phase 0 physical validation closes the scaffold loop (2026-09-17)

- **Environment:** macOS Loupedeck host, Logi Plugin Service 6.4.1.3246, .NET 10, reference Loupedeck device.
- **Observation:** The generated sample action was assigned and pressed three times; sequential action-log entries confirmed the complete build → link → host-load → hardware-input path.
- **Evidence:** `CodexDeck.log` recorded counter values 1, 2, and 3 after the physical presses; the development `.link` remained active.
- **Applicability:** Baseline validation for future plugins before replacing scaffold actions with product behavior.
- **Reusable recommendation:** Treat one clean build, host discovery, assignment, and repeated physical activation as the minimum scaffold exit criterion; record unresolved visual/device-specific issues separately.

## 90. Runtime action image is present in the action browser (2026-09-17)

- **Environment:** Loupedeck Live, Loupedeck 6.4.1.364, CodexDeck development link.
- **Observation:** The action browser lists both `Open Codex` and `Interrupt Codex` under `CodexDeck → Commands` after reload; the plugin rail shows the supplied OpenAI knot icon.
- **Evidence:** Accessibility tree after reload exposed both action rows and the `CodexDeck` plugin section.
- **Applicability:** Confirms registration and discoverability of the current P1 command surface; it does not replace a physical image-rendering check.
- **Reusable recommendation:** Validate action-browser discoverability separately from package identity and device-button rendering; record each surface independently.

## 91. Action Editor API requires signature verification before adoption (2026-09-17)

- **Environment:** Official Logi Actions SDK documentation and installed `PluginApi.dll` 6.4.1.3246.
- **Observation:** The SDK documents `ActionEditorCommand` and controls such as `ActionEditorCheckbox`, but the installed host assembly is the compatibility authority for exact constructors and parameter APIs.
- **Evidence:** Official Action Editor documentation plus local assembly symbol inspection.
- **Applicability:** Prevents a compile-time or runtime mismatch while moving from dynamic commands to configurable Core-profile actions.
- **Reusable recommendation:** Reflect and test the installed API surface before introducing editor controls; keep validated dynamic commands as a fallback until signatures are proven.

## 92. Action Editor compile success does not prove host discovery (2026-09-17)

- **Environment:** Logi Actions SDK 6.4.1.3246, .NET 10, macOS development link.
- **Observation:** A documented `ActionEditorCommand` implementation compiled and the plugin reloaded, but the host log still reported only three dynamic actions; the new editor action was not exposed in the action browser.
- **Evidence:** `CodexDeck.log` reported `3 dynamic actions loaded`; Loupedeck action browser showed only the existing commands/adjustments.
- **Applicability:** The installed host may require a separate registration path or may not expose this SDK surface through the current plugin type discovery.
- **Reusable recommendation:** Require host discovery evidence before claiming Action Editor support. Keep the implementation out of the shipping plugin until registration is understood; retain the validated dynamic-command fallback.

## 93. Core command discovery scales through dynamic actions (2026-09-17)

- **Environment:** Loupedeck 6.4.1.364, Logi Plugin Service 6.4.1.3246, .NET 10.
- **Observation:** `Focus Codex` and guarded `New Task` were added as ordinary dynamic commands and discovered immediately after reload.
- **Evidence:** `CodexDeck.log` reported five dynamic actions loaded, including both new command types; build completed with 0 warnings and 0 errors.
- **Applicability:** Current host path while Action Editor registration remains unresolved.
- **Reusable recommendation:** Prefer discoverable dynamic commands for the first Core release; add editor configuration only behind a separately verified host capability.

## 94. Protocol foundation remains build- and integration-clean (2026-09-17)

- **Environment:** .NET SDK 10.0.401, shared protocol tests, local companion project.
- **Observation:** Adapter, binding-store, reducer, receipt-correlation, and loopback socket integration tests pass; the companion also builds cleanly.
- **Evidence:** Test output: `Validated ShortcutAdapter, binding store, and loopback client integration`; companion build: 0 warnings, 0 errors.
- **Applicability:** Regression gate for P1 bridge work before host-facing state rendering.
- **Reusable recommendation:** Run protocol tests and companion build together after any transport, schema, persistence, or capability change.

## 95. Connection status action fails closed without a configured companion (2026-09-17)

- **Environment:** Loupedeck host with CodexDeck plugin; companion port unset.
- **Observation:** A new `Connection Status` diagnostic action checks the configured loopback port and reports unavailable when no companion is configured or the snapshot is stale.
- **Evidence:** Plugin builds/reloads cleanly with the new action; no default port is assumed.
- **Applicability:** Safe first host-facing bridge indicator before persistent connection subscriptions are implemented.
- **Reusable recommendation:** Never label the bridge ready based on process presence alone; require a fresh, protocol-version-validated snapshot.

## 96. Companion/plugin smoke configuration uses explicit shared environment values (2026-09-17)

- **Environment:** Local development companion and Loupedeck plugin.
- **Observation:** Both sides can use explicit `CODEX_DECK_PORT` and `CODEX_DECK_TOKEN` values; the plugin does not guess an ephemeral port.
- **Evidence:** Companion README now documents a fixed-port/token smoke command and matching plugin diagnostic behavior.
- **Applicability:** Repeatable local integration testing before persistent pairing UI exists.
- **Reusable recommendation:** Keep development transport configuration explicit and local; never silently scan ports or weaken token checks.

## 97. Authenticated companion hello round-trip verified (2026-09-17)

- **Environment:** macOS local companion on `127.0.0.1:47831`, token `dev-token`.
- **Observation:** A correctly newline-terminated hello envelope returned `hello.ack` with protocol version 1, shortcut mode, capabilities, and a fresh snapshot.
- **Evidence:** End-to-end socket request returned a typed `state.snapshot` containing `open_codex` and `interrupt` actions.
- **Applicability:** Confirms the transport seam used by the plugin connection diagnostic.
- **Reusable recommendation:** Keep an authenticated hello/ack smoke test as the first integration check before testing action intents or live state.

## 98. Shell environment is not a production pairing channel (2026-09-17)

- **Environment:** Loupedeck-launched plugin versus terminal-launched companion on macOS.
- **Observation:** The diagnostic currently reads `CODEX_DECK_PORT`/`CODEX_DECK_TOKEN`, which is convenient for development but depends on process environment inheritance that the GUI host does not guarantee.
- **Evidence:** Plugin is launched by Logi Plugin Service, while the documented smoke command sets variables only in the terminal process.
- **Applicability:** Production pairing and reconnect behavior.
- **Reusable recommendation:** Treat environment variables as test overrides only; persist endpoint/token through a user-scoped config store and provide reset/diagnostics UI before release.

## 99. Endpoint resolution now supports user-scoped configuration (2026-09-17)

- **Environment:** macOS Loupedeck plugin, .NET 10.
- **Observation:** `Connection Status` first honors explicit development environment overrides, then reads `%ApplicationData%/CodexDeck/connection.json` with port validation.
- **Evidence:** Plugin rebuild/reload completed with 0 warnings and 0 errors after adding `EndpointStore`.
- **Applicability:** Establishes the production direction for pairing without relying on GUI process environment inheritance.
- **Reusable recommendation:** Keep endpoint resolution deterministic (explicit override → validated user-scoped file → unavailable) and add encrypted/permissioned token handling before release.

## 100. Companion publishes its active endpoint for plugin discovery (2026-09-17)

- **Environment:** .NET 10 companion on macOS.
- **Observation:** On startup, the companion now atomically writes `ApplicationData/CodexDeck/connection.json` with its actual bound port and token, including user-only file permissions on Unix.
- **Evidence:** Companion build completes with 0 warnings and 0 errors.
- **Applicability:** Enables plugin discovery when the companion uses an ephemeral development port or is launched outside the Loupedeck process.
- **Reusable recommendation:** Publish only localhost endpoint metadata; replace the plaintext development token with a secure platform credential store before release.

## 101. Endpoint publication verified on macOS (2026-09-17)

- **Environment:** macOS companion on fixed port `47832` with development token.
- **Observation:** Startup created `~/Library/Application Support/CodexDeck/connection.json` containing the active port and token.
- **Evidence:** File read returned `{"port":47832,"token":"dev-token"}` while the companion was running.
- **Applicability:** Confirms the plugin can resolve a companion launched independently of Loupedeck.
- **Reusable recommendation:** Add endpoint-file existence and stale-process recovery checks to pairing diagnostics; never treat the file alone as proof of a live companion.

## 102. Endpoint source is now visible in plugin diagnostics (2026-09-17)

- **Environment:** Loupedeck plugin with environment and user-scoped endpoint resolution.
- **Observation:** `Connection Status` logs whether it is probing an explicit environment override or the persisted user-scoped file, including the port.
- **Evidence:** Plugin rebuild/reload completed with 0 warnings and 0 errors.
- **Applicability:** Support troubleshooting when multiple development companions or stale pairing files exist.
- **Reusable recommendation:** Log configuration provenance, not tokens; keep diagnostics useful without exposing secrets.

## 103. Graceful companion shutdown clears published endpoint (2026-09-17)

- **Environment:** .NET 10 companion on macOS.
- **Observation:** Ctrl-C handling now removes the user-scoped connection file and stops the listener.
- **Evidence:** Companion rebuild completed with 0 warnings and 0 errors.
- **Applicability:** Prevents a normal development shutdown from leaving a misleading endpoint behind.
- **Reusable recommendation:** Pair endpoint publication with cleanup; still retain live hello probing because crashes and forced termination can leave stale files.

## 104. Shutdown cleanup verified despite listener cancellation (2026-09-17)

- **Environment:** macOS companion on port `47833`.
- **Observation:** Endpoint file was present while running and cleared after Ctrl-C; the listener’s pending accept reported expected cancellation during process exit.
- **Evidence:** Before shutdown: `present`; after shutdown: `cleared`.
- **Applicability:** Confirms stale endpoint cleanup behavior in development.
- **Reusable recommendation:** Treat cancellation during deliberate listener shutdown as expected and suppress it in the release host loop for a clean operator experience.

## 105. Listener cancellation is now handled cleanly (2026-09-17)

- **Environment:** .NET 10 companion on macOS.
- **Observation:** The accept loop catches expected `OperationAborted`/`Interrupted` socket errors after Ctrl-C and exits without an unhandled exception.
- **Evidence:** Companion build completed with 0 warnings and 0 errors.
- **Applicability:** Development and release operator experience.
- **Reusable recommendation:** Handle deliberate listener cancellation explicitly, while allowing unexpected socket failures to remain visible.

## 106. Project README now reflects implementation reality (2026-09-17)

- **Environment:** Codex Deck repository after Phase 0 and early P1 implementation.
- **Observation:** README previously described a specification-only repository and an in-progress Phase 0; it now reflects the loaded five-action plugin, companion transport, endpoint discovery, and remaining P1 gaps.
- **Evidence:** Current plugin/companion builds and host logs support the updated status.
- **Applicability:** Agent handoffs and future contributor orientation.
- **Reusable recommendation:** Keep top-level project status synchronized with tested implementation milestones so new agents do not repeat completed bootstrap work.

## 107. Action Editor became discoverable after a subsequent reload (2026-09-17)

- **Environment:** Loupedeck 6.4.1.364, Logi Plugin Service 6.4.1.3246, CodexDeck development link.
- **Observation:** `Open Codex (Profile)` is now visible in the action browser and its editor shows the Profile list with `Core` selected, plus Cancel/Save controls.
- **Evidence:** Current accessibility tree exposes the `Open Codex (Profile)` row and editor container.
- **Applicability:** Confirms the documented Action Editor path is supported by the current host, but persistence and execution still need validation.
- **Reusable recommendation:** Recheck after a clean reload before declaring editor support blocked; host discovery can lag one reload cycle.

## 108. Clean reload reconciles stale Action Editor UI (2026-09-17)

- **Environment:** Loupedeck 6.4.1.364 with the current source after removing the editor prototype.
- **Observation:** A clean build/reload removed the stale `Open Codex (Profile)` editor entry and exposed the current five dynamic actions grouped under `Core` and `Diagnostics`.
- **Evidence:** Accessibility tree shows `Focus Codex`, `New Task`, `Interrupt Codex`, `Open Codex`, and `Connection Status`; no editor prototype remains.
- **Applicability:** Development hot-reload and plugin iteration.
- **Reusable recommendation:** Always perform a full reload and inspect the action browser after source removal; cached UI can misrepresent the active assembly.

## 109. Control map now distinguishes planned versus verified actions (2026-09-17)

- **Environment:** Codex Deck control map and current Loupedeck host.
- **Observation:** The control map now lists the five verified host actions separately from planned approval, reasoning, navigation, template, and voice controls.
- **Evidence:** Accessibility-tree verification and current plugin log show the exact five-action surface.
- **Applicability:** Agent handoffs, QA, and user-facing documentation.
- **Reusable recommendation:** Mark every control as verified, planned, or blocked; avoid letting aspirational layouts imply shipped behavior.

## 110. Protocol ticket exit criteria are met (2026-09-17)

- **Environment:** Shared protocol, mock bridge, companion, and integration tests.
- **Observation:** Versioned envelopes, authenticated hello/action flows, endpoint publication, fixtures, and loopback tests are all present and passing.
- **Evidence:** Protocol smoke tests pass; companion and plugin builds are clean.
- **Applicability:** P0-02 backlog closure and future schema changes.
- **Reusable recommendation:** Close protocol foundation only when both fixture validation and a real localhost round-trip pass; keep pairing UX as a separate P1 concern.

## 111. Full regression gate remains clean after endpoint changes (2026-09-17)

- **Environment:** macOS arm64, .NET SDK 10.0.401, Logi host development link.
- **Observation:** Plugin build/reload, companion build, and shared protocol integration tests all pass together.
- **Evidence:** Both builds completed with 0 warnings and 0 errors; test output confirmed adapter, binding-store, and loopback integration.
- **Applicability:** Safe checkpoint before adding pairing UX or additional commands.
- **Reusable recommendation:** Run this three-part gate after transport or host-facing changes to catch cross-project regressions early.

## 112. Pairing hardening is explicitly separated from development transport (2026-09-17)

- **Environment:** Current companion/plugin implementation.
- **Observation:** The development path is authenticated and local, but still publishes a plaintext token for deterministic testing.
- **Evidence:** New pairing-hardening checklist records Keychain storage, reset UX, reconnect backoff, rotation, and crash recovery as release requirements.
- **Applicability:** Security review and distribution readiness.
- **Reusable recommendation:** Do not promote a localhost development seam to release status without protected secret storage and explicit recovery/rotation behavior.

## 113. Connection diagnostic uses bounded retry (2026-09-17)

- **Environment:** Loupedeck plugin with shared `LoopbackClient`.
- **Observation:** `Connection Status` now allows two bounded connection attempts, covering short companion startup races without hanging the action.
- **Evidence:** Plugin rebuild/reload completed with 0 warnings and 0 errors.
- **Applicability:** Local companion startup/restart transitions.
- **Reusable recommendation:** Use small finite retries for user-triggered diagnostics; reserve longer backoff and background reconnect for the companion lifecycle service.

## 114. Connection diagnostic has a strict two-second deadline (2026-09-17)

- **Environment:** Loupedeck plugin using `LoopbackClient`.
- **Observation:** User-triggered connection checks now cancel connect/read operations after two seconds.
- **Evidence:** Plugin rebuild/reload completed with 0 warnings and 0 errors.
- **Applicability:** Hardware feedback actions must not block on a dead or wedged companion.
- **Reusable recommendation:** Bound all synchronous UI-triggered bridge checks; report unavailable on timeout and reserve persistent reconnect for background lifecycle code.

## 115. Connection diagnostic disposes clients on all outcomes (2026-09-17)

- **Environment:** Loupedeck plugin, .NET 10.
- **Observation:** Loopback clients are now disposed in a `finally` block after success, timeout, authentication failure, or malformed snapshot.
- **Evidence:** Plugin rebuild/reload completed with 0 warnings and 0 errors.
- **Applicability:** Repeated hardware diagnostics and companion restarts.
- **Reusable recommendation:** Treat every bridge probe as a finite resource scope; dispose transport objects even when the probe fails closed.

## 116. Five-action surface remains stable after reload and regression tests (2026-09-17)

- **Environment:** Loupedeck Live, Logi Plugin Service 6.4.1.3246, current CodexDeck build.
- **Observation:** After reload, the action browser consistently exposes the five intended actions grouped under Core and Diagnostics.
- **Evidence:** Accessibility tree shows Open Codex, Interrupt Codex, Focus Codex, New Task, and Connection Status; shared protocol tests pass.
- **Applicability:** P1 regression checkpoint and manual QA baseline.
- **Reusable recommendation:** Combine host-tree verification with protocol tests after each plugin reload to detect stale or missing registrations.

## 117. Documentation audit found no stale implementation claims (2026-09-17)

- **Environment:** Repository-wide documentation audit after the P1 reload checkpoint.
- **Observation:** Historical notes remain explicitly labeled, while active README, roadmap, backlog, and environment status reflect the current five-action implementation.
- **Evidence:** `git diff --check` is clean; targeted stale-claim search found no active “Phase 0 underway” or protocol-ticket-in-progress claims.
- **Applicability:** Agent handoff reliability.
- **Reusable recommendation:** Run a targeted stale-claim search after each milestone so documentation does not send future agents back to completed work.

## 118. Agent brief now advances by highest-priority unchecked ticket (2026-09-17)

- **Environment:** Repository README and ordered backlog.
- **Observation:** The handoff brief previously instructed agents to search only for unchecked P0 tickets even after P0-02 closed.
- **Evidence:** README now directs agents to the next unchecked highest-priority ticket while preserving protocol and no-scraping constraints.
- **Applicability:** Continuous multi-agent development.
- **Reusable recommendation:** Keep handoff prompts priority-aware so completed phases do not stall future implementation.

## 119. Capability snapshots must match shipped command surface (2026-09-17)

- **Environment:** .NET 10 companion and current CodexDeck dynamic commands.
- **Observation:** The companion snapshot now advertises `new_task` alongside `open_codex` and `interrupt`, matching the implemented guarded command path.
- **Evidence:** Companion build and shared protocol tests pass.
- **Applicability:** Capability-gated UI and future live adapters.
- **Reusable recommendation:** Treat capability lists as contracts; update them in the same change as command support so clients never enable an unimplemented action or hide a supported one.

## 120. Mock and real capability snapshots are synchronized (2026-09-17)

- **Environment:** Python mock bridge and .NET companion.
- **Observation:** Both snapshots now advertise `open_codex`, `interrupt`, and `new_task` with matching idle action states.
- **Evidence:** Fixture validation and four mock-bridge smoke tests pass.
- **Applicability:** Contract tests and agent development without the host running.
- **Reusable recommendation:** Keep mock capabilities aligned with the real companion to prevent false-green integration tests.

## 121. Capability coverage is asserted in the mock smoke suite (2026-09-17)

- **Environment:** Dependency-free Python protocol tests.
- **Observation:** The hello test now asserts that all currently supported Core capabilities and action states are present.
- **Evidence:** `shared/test_protocol.py` passes and reports Core capability coverage.
- **Applicability:** Prevents accidental capability drift in the mock contract.
- **Reusable recommendation:** Assert both capability names and corresponding action-state keys, not just that the fields are lists/maps.

## 122. GitHub publication is a Phase 1 handoff gate (2026-09-17)

- **Environment:** Project roadmap and repository workflow.
- **Observation:** Initial GitHub repository creation/upload is explicitly scheduled only after Phase 1 acceptance criteria pass.
- **Evidence:** Phase 1 roadmap exit gate now includes preserving findings, decisions, build instructions, and recording remote metadata.
- **Applicability:** Prevents premature publication of unstable or undocumented plugin code.
- **Reusable recommendation:** Publish the first shared repository at a tested phase boundary, with license, visibility, remote URL, and reproducibility documentation recorded.

## 123. GitHub preflight excludes secrets and host artifacts (2026-09-17)

- **Environment:** Planned Phase 1 repository publication workflow.
- **Observation:** The handoff checklist explicitly excludes pairing tokens, endpoint files, host logs, personal paths, binaries, and unreviewed experiments.
- **Evidence:** `docs/16-github-handoff.md` defines preflight, publish, and clean-clone verification steps.
- **Applicability:** Any local plugin project being shared with agents or collaborators.
- **Reusable recommendation:** Treat repository publication as a security/reproducibility review, not only a `git push` operation.

## 124. Quality plan now contains the executable regression gate (2026-09-17)

- **Environment:** Current repository build/test toolchain.
- **Observation:** The quality plan now lists the exact plugin, companion, C# protocol, fixture, and Python smoke commands plus the required host action-browser check.
- **Evidence:** Commands have passed in the current workspace; host reload and tree inspection are documented separately.
- **Applicability:** Repeatable pre-change and pre-publication verification.
- **Reusable recommendation:** Keep executable commands next to acceptance criteria, and explicitly state which checks require the physical host.

## 125. Regression gate is packaged as a one-command script (2026-09-17)

- **Environment:** macOS arm64 development workspace.
- **Observation:** `scripts/regression_gate.sh` runs plugin/companion builds, C# integration tests, fixture validation, and Python smoke tests in one fail-fast command.
- **Evidence:** Script completed with `Regression gate passed`.
- **Applicability:** Agent handoffs and Phase 1 GitHub preflight.
- **Reusable recommendation:** Prefer one documented fail-fast entry point for multi-project verification, while retaining the individual commands for diagnosis.

## 126. GitHub preflight detects secrets and host artifacts (2026-09-17)

- **Environment:** Repository publication tooling.
- **Observation:** `scripts/github_preflight.sh` checks whitespace errors, forbidden local artifacts, and likely development tokens before staging.
- **Evidence:** Script completed with `GitHub preflight passed` on the current workspace.
- **Applicability:** Initial publication and later release pushes.
- **Reusable recommendation:** Automate publication hygiene and fail before staging; do not rely on manual inspection alone.

## 127. Verification entry points are visible at repository root (2026-09-17)

- **Environment:** Repository README and scripts directory.
- **Observation:** The README now names the regression and GitHub-preflight scripts alongside the current build status.
- **Evidence:** Both scripts execute successfully in the current workspace.
- **Applicability:** New-agent onboarding and handoff speed.
- **Reusable recommendation:** Put the canonical verification commands where agents first look; avoid burying critical workflow in deep docs.

## 128. Approval/rejection shortcuts must not be inferred (2026-09-17)

- **Environment:** Codex Desktop shortcut-mode design.
- **Observation:** Open/Focus/New Task can use documented OS behavior, but Accept/Reject semantics are task-sensitive and no supported local Codex action API is available.
- **Evidence:** Community/research findings and current official integration boundary contain no reliable shortcut contract for these actions.
- **Applicability:** Safety-critical controls.
- **Reusable recommendation:** Require confirmed user-configured shortcuts or a supported action interface before implementing approval/rejection dispatch; keep them visibly planned rather than silently guessing.

## 129. New Task must activate Codex before sending Cmd+N (2026-09-17)

- **Environment:** macOS CodexDeck plugin and Codex Desktop.
- **Observation:** Sending Cmd+N without first activating Codex can target the wrong app or do nothing. The dispatcher now activates Codex, waits 250 ms, sends Cmd+N, waits for process exit, and reports failure on a non-zero result.
- **Evidence:** Plugin rebuild/reload completed with 0 warnings and 0 errors after the dispatcher change.
- **Applicability:** Foreground-sensitive desktop shortcuts.
- **Reusable recommendation:** For app-targeted shortcuts, explicitly activate the target app, add a short deterministic settling delay, and inspect automation exit status.

## 130. Cmd+N is confirmed; launcher Accessibility remains the likely variable (2026-09-17)

- **Environment:** macOS Codex Desktop and Loupedeck plugin.
- **Observation:** User confirmed Cmd+N works manually in Codex, while the plugin receipt is `execution_failed`; direct `osascript` succeeds from the terminal.
- **Evidence:** Plugin log shows `New Task receipt: failed (execution_failed)`; direct macOS automation exited 0.
- **Applicability:** System Events automation launched by Logi Plugin Service rather than an interactive terminal.
- **Reusable recommendation:** Test shortcuts manually first, then grant Accessibility to the actual launcher process (Loupedeck/Logi Plugin Service) and inspect its exit status; do not treat terminal success as proof of plugin permission.

## 131. macOS permission UX must guide, not silently grant, access (2026-09-17)

- **Environment:** macOS Accessibility and Automation privacy controls.
- **Observation:** A plugin can detect failed System Events automation and open the relevant Privacy & Security pane, but the user must explicitly enable the launcher process.
- **Evidence:** Apple documents explicit user approval for Accessibility and Automation access; current New Task testing succeeds from Terminal but fails from the plugin launcher.
- **Applicability:** Any macOS plugin that sends keyboard events or controls another app.
- **Reusable recommendation:** Explain the narrow purpose, open the correct settings pane, identify the actual launcher, provide Test again, and only report ready after a real probe succeeds.

## 136. Logi SDK has no documented generic runtime confirmation dialog (2026-09-17)

- **Environment:** Current Logi Actions SDK documentation and macOS permission flow.
- **Observation:** The SDK documents action-editor controls and button feedback, but no generic runtime modal/confirmation API for explaining a permission request before dispatch.
- **Evidence:** Official SDK capability/action-editor documentation; the visible consent dialog is generated by macOS for Logi Plugin Service controlling System Events.
- **Applicability:** User-facing permission onboarding.
- **Reusable recommendation:** Explain intent through action labels/feedback and a dedicated diagnostics action; let the operating system own the actual consent dialog rather than attempting to replicate it.

## 137. Adaptive permission button is feasible, but assignment is user-owned (2026-09-17)

- **Environment:** Loupedeck initial profile and macOS protected shortcut flow.
- **Observation:** A preassigned diagnostic action can change label/image/background as permissions progress and recheck on every protected action; the plugin cannot remove the physical assignment automatically.
- **Evidence:** SDK supports dynamic command display/image refresh; macOS prompts only when a protected operation is attempted.
- **Applicability:** First-run onboarding and permission revocation recovery.
- **Reusable recommendation:** Ship one visible adaptive setup button, advance through harmless probes/settings links, retain it as Connection Status after setup, and let users remove it explicitly.

## 138. Permission states now have a shared tested view contract (2026-09-17)

- **Environment:** Shared `CodexDeck.Protocol` library and regression suite.
- **Observation:** Accessibility, Automation, Connection Needed, Ready, and Unknown states map to stable labels/feedback with deterministic enabled behavior.
- **Evidence:** New mapper assertions pass in `scripts/regression_gate.sh`.
- **Applicability:** Plugin button rendering and future macOS/Windows probes.
- **Reusable recommendation:** Keep permission-state wording in the shared contract so platform adapters can differ without fragmenting user-facing status semantics.

## 139. Diagnostic action now renders shared permission-state wording (2026-09-17)

- **Environment:** macOS Loupedeck plugin.
- **Observation:** `Permissions & Connection` uses the shared permission view mapper and refreshes its label after opening settings.
- **Evidence:** Plugin rebuild/reload completed with 0 warnings and 0 errors.
- **Applicability:** Adaptive onboarding button and future platform probes.
- **Reusable recommendation:** Keep host-facing permission labels centralized and trigger an image/display refresh whenever the state changes.

## 140. macOS consent prompt is system-owned and appears on first protected probe (2026-09-17)

- **Environment:** macOS Loupedeck/Logi Plugin Service invoking System Events.
- **Observation:** The first protected operation caused macOS to show its native consent prompt for Logi Plugin Service; approving it allowed the shortcut flow to work.
- **Evidence:** User-provided screenshot showed the native “LogiPluginService wants access to control System Events” dialog; user confirmed the action worked after Allow.
- **Applicability:** macOS onboarding for keyboard automation.
- **Reusable recommendation:** Explain the purpose before triggering the probe, then let macOS present consent; do not implement a fake permission dialog or assume Terminal permission carries over to the host service.

## 141. Protected action feedback now points to permission recovery (2026-09-17)

- **Environment:** macOS Loupedeck plugin, `New Task` action.
- **Observation:** When the adapter reports `not_focused`/unavailable, the action label remains `New Task` but feedback becomes `Permission needed — Open Settings`.
- **Evidence:** Plugin rebuild and full regression gate pass with the new display mapping.
- **Applicability:** Permission-dependent shortcut actions.
- **Reusable recommendation:** Keep action identity stable while making recovery guidance explicit; direct users to the dedicated permissions action rather than silently retrying.

## 142. Usage limits and credits require plan-aware official data (2026-09-17)

- **Environment:** Current official OpenAI usage guidance.
- **Observation:** Codex may expose five-hour and weekly limits, reset times, credits, banked resets, or paid resets depending on plan/account; these are distinct concepts and availability varies.
- **Evidence:** OpenAI Help Center documents five-hour/weekly windows, reset behavior, credits, and plan-dependent eligibility, but no supported plugin-readable local API was identified.
- **Applicability:** Proposed Usage & Credits panels.
- **Reusable recommendation:** Design read-only combined/separate meters with explicit unsupported/stale states; never scrape UI, infer request cost, or expose purchase/reset controls without an official interface.

## 143. Usage alert thresholds are shared and freshness-aware (2026-09-17)

- **Environment:** Shared protocol library and regression suite.
- **Observation:** Usage windows map to Healthy, Warning (≤20% remaining), Exhausted, or Unknown when unsupported or older than five minutes.
- **Evidence:** Typed usage models and threshold assertions pass the consolidated regression gate.
- **Applicability:** Future combined/separate Loupedeck usage panels.
- **Reusable recommendation:** Centralize thresholds and stale handling in shared code so platform panels present consistent alerts without inventing account data.

## 144. Usage fixtures cover supported and plan-unsupported accounts (2026-09-17)

- **Environment:** Dependency-free protocol fixtures.
- **Observation:** Added supported Plus-style usage/credits data and a Business-style unsupported-credit snapshot; fixture validation accepts both typed forms.
- **Evidence:** `shared/validate_fixtures.py` passes with both new usage fixtures.
- **Applicability:** Future panel rendering and plan-matrix tests.
- **Reusable recommendation:** Include at least one fully supported and one unsupported-plan fixture so UI code cannot assume credits or windows exist for every account.

## 145. Usage panels need capability-specific plan behavior (2026-09-17)

- **Environment:** OpenAI plan/usage guidance and proposed Codex Deck panel.
- **Observation:** Personal plans may expose credits or reset options while workspace plans may expose only windows; availability varies by account.
- **Evidence:** Official OpenAI Help Center distinguishes included limits, credits, banked resets, and plan-dependent eligibility.
- **Applicability:** Combined/separate usage and credits surfaces.
- **Reusable recommendation:** Render each field only when explicitly supported, label unavailable capabilities, and link users to official Usage settings for any billing/reset action.

## 146. Stale usage must map to Unknown, not a colored warning (2026-09-17)

- **Environment:** Shared usage alert mapper and C# regression suite.
- **Observation:** Supported data older than five minutes now maps to `Unknown`, even when its remaining percentage appears healthy.
- **Evidence:** Regression assertion covers a six-minute-old snapshot and passes.
- **Applicability:** Hardware usage meters where stale data could cause false reassurance.
- **Reusable recommendation:** Suppress healthy/warning/exhausted color claims when freshness is outside the contract; show stale/unavailable text instead.

## 147. Combined and separate usage layouts should be explicit preferences (2026-09-17)

- **Environment:** Proposed Usage & Credits panel configuration.
- **Observation:** Users may need a compact combined view or separate five-hour/weekly panels; credits are an optional third panel.
- **Evidence:** Usage design now defines stable `combined`, `separate`, and `combined_with_credits` preference values.
- **Applicability:** Profile/configuration persistence and device layouts.
- **Reusable recommendation:** Store layout choice as an opaque enum, preserve it when a field is unsupported, and show an explicit unsupported state rather than silently changing the user’s layout.

## 148. Usage layout preferences are validated at the shared contract boundary (2026-09-17)

- **Environment:** Shared protocol library and regression suite.
- **Observation:** Only `combined`, `separate`, and `combined_with_credits` are accepted layout values; unknown values are rejected.
- **Evidence:** `UsageLayoutValidator` assertions pass in the consolidated regression gate.
- **Applicability:** Profile import, action-editor Save, and migration handling.
- **Reusable recommendation:** Validate presentation preferences centrally before persistence so malformed layouts cannot reach device rendering.

## 149. Usage warning threshold is configurable but bounded (2026-09-17)

- **Environment:** Shared usage alert mapper and regression suite.
- **Observation:** Amber warnings default to 20% remaining; callers may supply a threshold from 1–99%, while invalid values throw validation errors.
- **Evidence:** Custom-threshold assertion passes in the consolidated regression gate.
- **Applicability:** User preference and plan-aware usage panels.
- **Reusable recommendation:** Expose alert sensitivity as a bounded preference, preserving consistent Healthy/Warning/Exhausted semantics.

## 150. Permission probe is host-discoverable and reload-clean (2026-09-17)

- **Environment:** macOS Loupedeck host, Logi Plugin Service 6.4.1.3246.
- **Observation:** The new `PermissionsDiagnosticCommand` loads as the seventh dynamic action after reload and uses a System Events probe before opening settings.
- **Evidence:** `CodexDeck.log` reports seven dynamic actions loaded with `PermissionsDiagnosticCommand`; `git diff --check` is clean.
- **Applicability:** Permission onboarding on the reference host.
- **Reusable recommendation:** Verify both host discovery and probe behavior after adding diagnostic actions; a compiled command alone is insufficient.

## 151. Settings-launch failures are reported separately from permission state (2026-09-17)

- **Environment:** macOS permissions diagnostic action.
- **Observation:** The action now checks the `open` process exit status; a failed settings launch maps to Unknown instead of falsely advancing to Automation Needed.
- **Evidence:** Plugin rebuild/reload completed with 0 warnings and 0 errors.
- **Applicability:** User-facing recovery flows where opening system settings can fail.
- **Reusable recommendation:** Verify the settings launcher itself and distinguish “could not open settings” from “permission still needed.”

## 152. macOS New Task path is now explicitly testable (2026-09-17)

- **Environment:** macOS Codex Desktop and Loupedeck plugin.
- **Observation:** The documented New Task sequence is activation → 250 ms settle → Cmd+N via System Events → exit-status check.
- **Evidence:** The sequence is now a named quality-plan scenario and the implementation logs success/failure plus stderr.
- **Applicability:** Manual QA and troubleshooting of app-targeted shortcuts.
- **Reusable recommendation:** Document the exact OS automation sequence and its observable evidence so user reports can be mapped to a specific stage.

## 153. Permission and New Task validation is captured as a repeatable macOS checklist (2026-09-17)

- **Environment:** macOS Loupedeck/Logi Plugin Service and Codex Desktop.
- **Observation:** A dedicated checklist now covers first consent, repeat probe, New Task verification, log inspection, permission revocation, and recovery.
- **Evidence:** `docs/17-macos-manual-test.md` records the ordered user-facing steps and required evidence.
- **Applicability:** Manual QA, support, and future agent handoffs.
- **Reusable recommendation:** Keep platform-specific permission tests explicit and repeatable; include both initial grant and later revocation recovery.

## 154. Harmless permission recheck is separate from protected actions (2026-09-17)

- **Environment:** macOS Loupedeck plugin.
- **Observation:** Added `Test Permissions`, which probes System Events without sending a Codex shortcut; `Permissions & Connection` remains the settings-recovery action.
- **Evidence:** Full regression gate passes after adding the diagnostic action.
- **Applicability:** First-run setup and permission-revocation recovery.
- **Reusable recommendation:** Provide a non-destructive Test Again path so users can verify access without triggering task mutations.

## 155. Permission diagnostics are discoverable as separate setup and recheck actions (2026-09-17)

- **Environment:** Loupedeck Live, Logi Plugin Service 6.4.1.3246, current plugin reload.
- **Observation:** Action browser exposes `Permissions & Connection`, `Connection Status`, and `Test Permissions` under Diagnostics.
- **Evidence:** Accessibility tree shows all three rows after reload.
- **Applicability:** First-run setup, permission recovery, and connection troubleshooting.
- **Reusable recommendation:** Keep settings launch, harmless permission recheck, and companion status as distinct actions so each has a clear user intent.

## 156. macOS manual test now covers all diagnostic actions (2026-09-17)

- **Environment:** macOS Loupedeck manual QA guide.
- **Observation:** The checklist now tests settings launch, harmless permission recheck, connection status, New Task, revocation, and recovery.
- **Evidence:** `docs/17-macos-manual-test.md` contains the ordered steps and log evidence requirements.
- **Applicability:** User acceptance testing and future support reports.
- **Reusable recommendation:** Keep manual QA aligned with every user-facing diagnostic action; include both healthy and revoked-permission paths.

## 157. Diagnostics summary is shared across platform implementations (2026-09-17)

- **Environment:** Shared protocol library and regression suite.
- **Observation:** Permission state, companion connectivity, freshness, and optional detail now have one typed `DiagnosticsSnapshot` and deterministic label mapping.
- **Evidence:** Mapper assertions pass in the consolidated regression gate.
- **Applicability:** macOS/Windows diagnostics actions and future usage panels.
- **Reusable recommendation:** Keep cross-platform diagnostic semantics in shared contracts; platform adapters should supply evidence, not invent user-facing states.

## 158. Backlog evidence now tracks permission UX and accessibility work (2026-09-17)

- **Environment:** Current P1 implementation and ordered backlog.
- **Observation:** P1-03 now records the macOS consent/probe/settings flow; P1-06 records shared text/non-color semantics while physical accessibility review remains open.
- **Evidence:** `docs/06-backlog.md` reflects the implemented actions and explicit remaining validation.
- **Applicability:** Agent handoffs and Phase 1 exit assessment.
- **Reusable recommendation:** Update ticket evidence with user-observable behavior, not only source-file changes, and keep manual hardware checks visibly outstanding.

## 159. First-run permission explanation is now in the top-level README (2026-09-17)

- **Environment:** Repository onboarding documentation.
- **Observation:** README explains the native Logi Plugin Service/System Events prompt, the purpose of Allow, the setup and recheck actions, and the no-content-access boundary.
- **Evidence:** New First-run macOS permissions section in `README.md`.
- **Applicability:** New users and support handoffs.
- **Reusable recommendation:** Put security-sensitive first-run expectations in the top-level README as well as detailed troubleshooting docs.

## 160. Permission consent and revocation are release-gate scenarios (2026-09-17)

- **Environment:** macOS Loupedeck release checklist.
- **Observation:** The quality plan now requires manual testing of first-run System Events consent, Accessibility recovery, harmless Test Permissions, and later revocation/recovery.
- **Evidence:** New release-checklist item in `docs/07-quality-plan.md`.
- **Applicability:** Any plugin using macOS keyboard automation.
- **Reusable recommendation:** Treat both granting and losing permission as release scenarios; a one-time successful prompt is not sufficient evidence.

## 132. Cross-platform plugins need shared contracts with OS-specific adapters (2026-09-17)

- **Environment:** Loupedeck/Logi Actions plugin targeting macOS and Windows.
- **Observation:** Protocol, state, and action presentation can remain shared, while shortcut dispatch, app activation, and permission probes differ materially by OS.
- **Evidence:** macOS New Task requires `osascript` and Accessibility/Automation authorization; Windows requires different input and permission APIs.
- **Applicability:** Any multi-OS desktop-control plugin.
- **Reusable recommendation:** Select an OS-specific adapter at runtime, package platform implementations separately, and fail closed on unsupported platforms.

## 133. Permission guidance is exposed as a dedicated diagnostic action (2026-09-17)

- **Environment:** macOS Loupedeck plugin.
- **Observation:** `Permissions & Connection` opens the Accessibility settings pane and is grouped under Diagnostics; protected-action failures can direct users there.
- **Evidence:** Plugin builds/reloads cleanly after adding the action; user-facing wording and Automation follow-up are documented in the pairing-hardening guide.
- **Applicability:** macOS shortcut automation onboarding.
- **Reusable recommendation:** Provide an explicit settings action and Test Again flow instead of repeatedly prompting or leaving users with an unexplained unavailable state.

## 134. New Task failure is an automation-process error, not shortcut rejection (2026-09-17)

- **Environment:** macOS Loupedeck plugin, Logi Plugin Service 6.4.1.3246.
- **Observation:** Focus succeeds, but the New Task `osascript` process exits with code 1; the plugin therefore reports `execution_failed` before Codex can process Cmd+N.
- **Evidence:** `CodexDeck.log` records `exited=True, code=1, success=False` on repeated presses.
- **Applicability:** macOS launcher permissions, AppleScript terminology, and System Events automation.
- **Reusable recommendation:** Capture and surface stderr from OS automation processes; distinguish launcher/permission failures from an application declining a valid shortcut.

## 135. ProcessStartInfo argument quoting caused AppleScript syntax failure (2026-09-17)

- **Environment:** macOS .NET plugin invoking `osascript`.
- **Observation:** Passing multiple quoted `-e` expressions through one `Arguments` string produced AppleScript error `-2740`; separate `ArgumentList` entries are required.
- **Evidence:** Plugin log captured `syntax error: A unknown token can’t go here. (-2740)`; code now passes each expression as its own argument and rebuilds cleanly.
- **Applicability:** Any .NET macOS plugin invoking command-line tools with nested quoting.
- **Reusable recommendation:** Use `ProcessStartInfo.ArgumentList` instead of hand-escaped command strings when invoking `osascript` or other multi-argument tools.

## 161. Auto-discovery registers leftover sample action classes (2026-09-17)

- **Environment:** Loupedeck Plugin API on Logi Plugin Service 6.4.1.3246, .NET 10 plugin.
- **Observation:** A scaffold `PluginDynamicAdjustment` class was registered even though it was not referenced by the plugin manifest or layout.
- **Evidence:** Host log reported the sample `CounterAdjustment` and eight total dynamic actions; deleting the unused class and reloading reduced discovery to the seven intended Codex actions.
- **Applicability:** Any Loupedeck plugin using convention/reflection-based action discovery.
- **Reusable recommendation:** Remove or isolate template/sample action classes before packaging; verify the host log’s discovered-action count and names after every clean reload.

## 162. Focus guards must use ArgumentList for macOS osascript (2026-09-17)

- **Environment:** macOS .NET plugin invoking `osascript` through `ProcessStartInfo`.
- **Observation:** The New Task path had already been corrected to separate `-e` arguments, but the focus probe and interrupt dispatcher still used a hand-quoted `Arguments` string.
- **Evidence:** Interrupt consistently returned `not_focused` even after Codex was deliberately focused; the remaining invocations were structurally subject to the same quoting failure as the earlier `-2740` New Task error.
- **Applicability:** Any plugin using AppleScript for frontmost-app detection or keyboard events.
- **Reusable recommendation:** Treat every `osascript` invocation consistently: add each `-e` and script body as separate `ArgumentList` entries, then log stderr/exit status when diagnosing unavailable results.

## 163. macOS shortcut automation needs a repeatable focus/permission diagnostic matrix (2026-09-17)

- **Environment:** macOS Loupedeck plugin using System Events for frontmost-app detection and key events.
- **Observation:** A shortcut can be valid and permissions can be granted while the action still reports unavailable when the frontmost-process probe is malformed, the wrong bundle identifier is assumed, Codex is not frontmost, or System Events authorization has been revoked.
- **Evidence:** Interrupt initially failed as `not_focused`; after converting all AppleScript calls to `ArgumentList`, the same physical action succeeded. New Task had independently shown the same quoting-sensitive failure pattern.
- **Applicability:** Any macOS plugin that activates an app, checks focus, or sends keyboard events.
- **Reusable recommendation:** For each protected shortcut, test and log separately: (1) `osascript` exit code/stderr, (2) detected frontmost bundle identifier, (3) Accessibility/Automation authorization, (4) target app activation, (5) key dispatch result, and (6) behavior when another app is frontmost. Never collapse these into a generic unavailable state.

## 164. Codex interrupt may require a second Escape press (2026-09-17)

- **Environment:** macOS Codex Desktop, Loupedeck plugin using System Events Escape dispatch; user-tested in a separate Codex task chat.
- **Observation:** The first Interrupt press changes Codex’s stop control to an Escape state but does not stop the running task. A second press while that Escape state is visible completes the interruption; pressing after the UI returns to Stop repeats the first-stage transition.
- **Evidence:** Reproduced with a non-finite prime-counting task; plugin action receipts were accepted, but task termination required two physical presses.
- **Applicability:** Codex Desktop interrupt interaction; do not assume the same behavior for other hosts or future Codex UI versions.
- **Reusable recommendation:** Model interrupt as a potentially two-stage interaction. Keep visible “Interrupt again” feedback until completion can be confirmed, and validate stop behavior on each target host rather than treating one accepted Escape event as success.

## 165. Double-stage interrupt is isolated as an explicit action (2026-09-17)

- **Environment:** macOS .NET Loupedeck plugin; Logi Plugin Service 6.4.1.3246.
- **Observation:** The plugin now exposes `Interrupt Codex (Double Press)` separately from the single Escape action and sends two Escape events with a 350 ms delay.
- **Evidence:** Clean plugin/companion build and regression gate pass; physical cancellation validation remains required.
- **Applicability:** Only the verified macOS Codex interaction; not a cross-platform default.
- **Reusable recommendation:** Isolate host-specific composite gestures behind an explicit action and adapter so the normal action remains predictable and future host behavior can diverge safely.

## 166. Dynamic interrupt-again feedback is safe; per-state background color remains unverified (2026-09-17)

- **Environment:** macOS Loupedeck plugin with dynamic command display refresh.
- **Observation:** The normal Interrupt action now displays `Press again to interrupt` for five seconds after an accepted first Escape. The available SDK surface confirms dynamic text/image refresh, but no verified per-state background-color setter is present in this project.
- **Evidence:** Plugin reload and regression gate pass after adding the state; SDK inspection found `ActionImageChanged()` and image overrides, but no documented dynamic background API.
- **Applicability:** Feedback behavior is platform-agnostic; the two-stage trigger is macOS/Codex-specific. Background rendering remains host/API-dependent.
- **Reusable recommendation:** Ship truthful text/icon feedback first; only add red backgrounds after a documented SDK method and physical-device rendering test are available, and retain a non-color cue.

## 167. Installed Plugin API contains image recoloring primitives (2026-09-17)

- **Environment:** Installed `PluginApi.dll` from Logi Plugin Service 6.4.1.3246.
- **Observation:** Binary inspection exposes image-processing symbols including `CreateImage` and `ReplaceImageColor`, suggesting generated/recolored bitmap frames may be supported by the SDK internals.
- **Evidence:** `strings PluginApi.dll` output; the current public project code only uses `ReadImage` and `GetCommandImage`, so the callable surface is not yet confirmed.
- **Applicability:** Potentially agnostic to plugins using runtime image feedback; exact availability may vary by SDK version.
- **Reusable recommendation:** Before implementing dynamic color states, inspect the installed assembly’s public methods and validate a generated frame on physical hardware; do not depend on internal symbols alone.

## 168. Static attention bitmap avoids runtime graphics dependencies (2026-09-17)

- **Environment:** macOS Loupedeck plugin, .NET 10, Logi Plugin Service 6.4.1.3246.
- **Observation:** A pre-rendered red `interrupt-attention.png` can be embedded and selected through `GetCommandImage()` without loading SkiaSharp or requesting additional user permissions.
- **Evidence:** Asset added to the package and the plugin/companion regression gate passed after reload.
- **Applicability:** Any plugin needing finite visual states where runtime image APIs are uncertain.
- **Reusable recommendation:** Prefer pre-rendered, finite bitmap frames with a safe fallback image; verify dimensions, alpha, package inclusion, and physical rendering before release.

## 169. Interrupt attention frame verified on physical Loupedeck (2026-09-17)

- **Environment:** macOS Loupedeck Live, Logi Plugin Service 6.4.1.3246, Codex Desktop.
- **Observation:** The normal Interrupt action displays the red attention bitmap and `Press again to interrupt` after the first accepted Escape, then returns to the normal icon after timeout.
- **Evidence:** User-confirmed physical-device test after plugin reload.
- **Applicability:** Visual feedback pattern is reusable; two-stage semantics remain Codex/macOS-specific.
- **Reusable recommendation:** Require physical confirmation of both dynamic image and label transitions; simulator or build success alone is insufficient.

## 170. Secure token stores should remain isolated until consent and migration are tested (2026-09-17)

- **Environment:** macOS companion, .NET 10, Logi Plugin Service 6.4.1.3246.
- **Observation:** A Keychain-backed `IPairingTokenStore` candidate can coexist with the development file store without changing current behavior.
- **Evidence:** `MacKeychainTokenStore` compiles and the regression gate passes; it is intentionally not selected by the runtime yet.
- **Applicability:** Platform-specific to macOS Keychain; Windows requires a separate protected-store adapter.
- **Reusable recommendation:** Add secure-store adapters behind an interface, keep them disabled until first-run consent, migration, rotation, denial, and recovery are tested, and never silently downgrade release builds to plaintext storage.

## 171. macOS Keychain adapter smoke-tested successfully (2026-09-17)

- **Environment:** macOS arm64, .NET 10 companion, login Keychain, `CODEX_DECK_TOKEN_STORE=keychain`.
- **Observation:** The companion started successfully in Keychain mode and a 32-byte base64 pairing token was readable from service `com.codexdeck.companion`, account `pairing-token`.
- **Evidence:** Short-lived companion run bound successfully; `security find-generic-password` returned a 45-byte token record.
- **Applicability:** macOS-specific; Windows secure-store behavior remains unverified.
- **Reusable recommendation:** Smoke-test secure stores with a bounded run and verify retrieval without printing token contents; keep token values out of logs and test output.

## 172. Keychain token retrieval is stable across repeated reads (2026-09-17)

- **Environment:** macOS login Keychain using the Codex Deck service/account.
- **Observation:** Two consecutive secure reads returned identical token hashes without modifying or exposing the token value.
- **Evidence:** SHA-256 digests matched: `70a028...8a240` (digest only).
- **Applicability:** macOS Keychain adapter; does not establish rotation or Windows behavior.
- **Reusable recommendation:** Include idempotent repeated-read checks in secure-store smoke tests while comparing only digests or equality in memory, never logging secrets.

## 173. Host UI refresh is required to expose newly discovered actions (2026-09-17)

- **Environment:** macOS Loupedeck Live UI, Logi Plugin Service 6.4.1.3246.
- **Observation:** The host log showed the new action immediately after reload, while the already-open Loupedeck panel continued showing the previous action tree until its accessibility state was refreshed.
- **Evidence:** A fresh host accessibility snapshot then showed `Interrupt Codex (Double Press)` and the complete eight-action list.
- **Applicability:** Any convention-discovered Loupedeck action added during development.
- **Reusable recommendation:** Verify both the plugin-service discovery log and a refreshed host UI; do not treat a stale open panel as evidence that a newly loaded action is missing.

## 174. Manual Test 1 — permissions and connection reached Connected (2026-09-17)

- **Environment:** User test on the reference macOS Loupedeck setup.
- **Observation:** `Permissions & Connection` progressed to `Connected`.
- **Evidence:** User-reported test result.
- **Applicability:** Reference macOS setup; broader device/OS validation remains pending.
- **Reusable recommendation:** Record the final user-visible state, not only the underlying permission prompt, when validating onboarding diagnostics.

## 175. Diagnostic actions must refresh visible feedback, not only logs (2026-09-17)

- **Environment:** macOS Loupedeck plugin.
- **Observation:** `Test Permissions` initially logged `ready` but produced no visible button change, making a successful probe appear inert to the user.
- **Evidence:** User reported no change; the action now displays transient `Ready`/`Unavailable` feedback and refreshes the image state.
- **Applicability:** Any diagnostic or probe action with a user-triggered result.
- **Reusable recommendation:** Treat logs as developer evidence only; every user-triggered probe needs immediate visible success/failure feedback and a bounded reset to idle.

## 176. Development companions need explicit foreground lifecycle guidance (2026-09-17)

- **Environment:** macOS development companion launched with `dotnet run`.
- **Observation:** The companion is not installed as a background service and has no visible stop button; users must start it from Terminal and stop it with `Ctrl-C` in that same terminal.
- **Evidence:** Companion README and current process model; connection diagnostics now say `Companion connected` or `Companion unavailable`.
- **Applicability:** Development-only local companion workflows.
- **Reusable recommendation:** Clearly distinguish development foreground processes from installed services, document start/stop commands, and use explicit connection wording rather than implying a daemon is present.

## 177. Endpoint JSON casing mismatch can make a live companion appear unavailable (2026-09-17)

- **Environment:** .NET companion and Loupedeck plugin using `System.Text.Json`.
- **Observation:** Companion publication used lowercase `port`/`token` keys while plugin deserialization expected case-sensitive `Port`/`Token`, causing `Resolve()` to return null despite a live endpoint file.
- **Evidence:** Connection Status logged only the action press and never the probe; enabling case-insensitive property matching fixes parsing and the regression gate passes.
- **Applicability:** Any JSON producer/consumer pair using independently configured serializer naming policies.
- **Reusable recommendation:** Define and test a shared naming policy or enable explicit case-insensitive/annotated deserialization; include a live endpoint fixture with the exact serialized casing.

## 178. Manual Phase 1 results confirm activation, interrupt, and permission recovery (2026-09-17)

- **Environment:** User-tested macOS Loupedeck Live setup with Codex Desktop and local companion.
- **Observation:** Tests 1–3 passed; New Task works when Codex is unfocused because its dispatcher activates Codex first; single and double interrupt work as designed but require Codex focus. Revoking Automation and pressing Companion Status requests permission again. Accessibility consent did not visibly re-prompt from Test Permissions, but did prompt after closing/restarting Loupedeck.
- **Evidence:** User-reported results from the Phase 1 script.
- **Applicability:** macOS permission-prompt timing is host/service-specific; activation behavior is intentional for New Task, while interrupt remains focus-guarded.
- **Reusable recommendation:** Test permission revocation both in-place and after host restart; do not assume a probe always triggers a native prompt immediately. Distinguish actions that safely activate their target from actions that require an already focused target.

## 179. Companion connection survives sleep and screen lock (2026-09-17)

- **Environment:** User-tested macOS Loupedeck Live with the local companion.
- **Observation:** Companion/connection status remained connected across sleep and screen lock.
- **Evidence:** User-reported Test 7 result.
- **Applicability:** Reference macOS setup; other devices and operating systems remain unverified.
- **Reusable recommendation:** Include sleep and screen-lock transitions in local companion lifecycle tests; preserve status only when a fresh probe confirms it.

## 180. Diagnostic success feedback must differ from cached/idle labels (2026-09-17)

- **Environment:** macOS Loupedeck dynamic command UI.
- **Observation:** A successful Test Permissions result rendered as `Ready`, which could appear unchanged when the host retained a cached label from a prior press or reload.
- **Evidence:** User reported the action appeared to start and remain `ready`; success text was changed to `Permissions checked` for a clear transient transition.
- **Applicability:** Any dynamic action whose host may cache labels/images.
- **Reusable recommendation:** Make success/failure feedback semantically distinct from idle labels, and use a bounded transient state so users can tell a press was processed.

## 181. Test Permissions transient feedback verified by user (2026-09-17)

- **Environment:** macOS Loupedeck Live reference setup.
- **Observation:** User confirmed the updated `Permissions checked` feedback is visibly shown after pressing Test Permissions.
- **Evidence:** User confirmation following plugin reload.
- **Applicability:** Reference host rendering; other devices remain unverified.
- **Reusable recommendation:** Close the loop on UI fixes with explicit user-visible confirmation, not only build or log evidence.

## 182. A bridge scaffold is not an enforced automation boundary (2026-09-17)

- **Environment:** Loupedeck plugin plus local companion architecture review.
- **Observation:** A companion can compile, authenticate a health probe, and expose protocol models while user-facing actions still bypass it through direct plugin-side OS automation.
- **Evidence:** Independent architecture audit verified direct plugin `osascript` dispatch and companion-side fabricated accepted receipts.
- **Applicability:** Any hardware-plugin/companion architecture intended to isolate privileged desktop automation.
- **Reusable recommendation:** Verify the runtime call path for every user action; require the companion to validate, dispatch, and return truthful receipts before claiming that an automation boundary or pairing model is enforced.

## 183. Build the companion adapter before migrating plugin actions (2026-09-17)

- **Environment:** .NET 10 companion and shared protocol on macOS.
- **Observation:** A platform adapter provides a bounded migration seam; unsupported actions can fail closed while existing plugin actions remain temporarily functional.
- **Evidence:** `MacCodexAdapter` builds, returns `unavailable/not_supported` for unknown actions, and dispatches only the verified shortcut set.
- **Applicability:** Any staged migration from direct hardware-plugin automation to a local privileged companion.
- **Reusable recommendation:** Land and test the companion adapter first, then migrate each plugin action and remove its local OS dispatcher; do not mark the architecture complete until no plugin action can invoke OS automation.

## 184. Reconcile protocol documentation with the shipped wire format (2026-09-17)

- **Environment:** .NET 10 companion and loopback client.
- **Observation:** The deployed bridge is newline-delimited TCP, while the original architecture text incorrectly called it WebSocket.
- **Evidence:** `TcpListener`/`TcpClient` are used by the companion and plugin; docs now identify the actual transport and its bounded request expectations.
- **Applicability:** Any local plugin bridge whose transport evolves during scaffolding.
- **Reusable recommendation:** Treat implementation, schema, tests, and architecture docs as one contract; update all together when the wire protocol is selected.

## 185. Bound idle bridge reads (2026-09-17)

- **Environment:** .NET 10 newline-delimited TCP companion.
- **Observation:** A connected client that sends no complete line can otherwise hold a server task indefinitely.
- **Evidence:** Companion now applies a 15-second cancellation deadline to each line read and closes the idle client cleanly.
- **Applicability:** Local socket bridges and plugin companion services.
- **Reusable recommendation:** Bound both connect and read operations; treat timeout as a recoverable disconnect rather than allowing an idle client to consume a handler forever.

## 186. Live socket tests may require host permission (2026-09-17)

- **Environment:** macOS sandboxed development shell running the .NET loopback companion.
- **Observation:** The companion could build successfully but binding a loopback listener was denied inside the restricted test sandbox.
- **Evidence:** Direct run returned `SocketException (13): Permission denied`; the same integration test passed when executed with explicit local socket permission.
- **Applicability:** Any plugin companion integration test that binds localhost during automated development.
- **Reusable recommendation:** Distinguish sandbox/network-policy failures from product failures and document the required elevated test invocation.

## 187. Reconnect tests should repeat the full hello contract (2026-09-17)

- **Environment:** .NET 10 loopback companion integration smoke.
- **Observation:** A reconnect is only useful if it re-authenticates and returns a fresh hello acknowledgement, not merely if the TCP socket can be reopened.
- **Evidence:** The live smoke now opens a second authenticated connection and validates `hello.ack` after testing an unauthorized connection.
- **Applicability:** Local companion bridges and reconnecting hardware clients.
- **Reusable recommendation:** Test reconnect as a complete protocol handshake, including authentication and snapshot delivery.

## 188. Secure endpoint files before atomic publication (2026-09-17)

- **Environment:** macOS companion pairing and `connection.json` publication.
- **Observation:** Restricting permissions only after replacing the destination briefly exposes a newly written endpoint file under default permissions.
- **Evidence:** Publication now applies user read/write mode to the temporary file before atomic move; macOS Keychain is the default token store.
- **Applicability:** Local services publishing bearer-token endpoint metadata.
- **Reusable recommendation:** Secure temporary files before publication and make the platform secure secret store the default; require an explicit development opt-out for plaintext.

## 189. Keep secure-store selection explicit in tests (2026-09-17)

- **Environment:** Companion token-store factory on macOS and non-macOS hosts.
- **Observation:** A secure default must still permit deterministic development tests without silently weakening production behavior.
- **Evidence:** Factory selection is now Keychain by default on macOS, file store only via explicit `CODEX_DECK_TOKEN_STORE=file`, and unsupported Keychain requests fail on other platforms.
- **Applicability:** Cross-platform local companion secret storage.
- **Reusable recommendation:** Test default, explicit development override, and unsupported-platform denial as separate cases; never infer the secure-store path from a successful build alone.

## 190. Keep executable hosts separate from testable companion libraries (2026-09-17)

- **Environment:** .NET 10 companion with top-level server entrypoint.
- **Observation:** Referencing the executable project from a test project can execute the host during test startup, binding sockets and invoking platform services.
- **Evidence:** A temporary test-project reference started the companion entrypoint instead of isolating token-store types.
- **Applicability:** Any .NET local service using top-level statements.
- **Reusable recommendation:** Extract shared service logic into a class library before adding in-process unit tests; until then use process-level integration tests.

## 191. Pairing permissions need process-level verification (2026-09-17)

- **Environment:** macOS companion integration smoke with explicit file-store mode.
- **Observation:** Endpoint-file permissions are observable only after the host publishes the live connection metadata.
- **Evidence:** The integration smoke now checks `connection.json` owner-only mode while exercising the real server.
- **Applicability:** Local services publishing credentials or connection metadata.
- **Reusable recommendation:** Verify secret-file permissions in a live process test, not only by inspecting the write helper.

## 192. Keep workflow template persistence metadata-only by default (2026-09-17)

- **Environment:** Shared .NET 10 protocol library and local profile store.
- **Observation:** Workflow controls can be scaffolded safely without persisting prompt text or reconstructing task context.
- **Evidence:** `WorkflowTemplateStore` persists only stable ID, label, action, and enabled state; invalid IDs/labels fail validation and defaults recover on malformed files.
- **Applicability:** Hardware control profiles and other local action registries.
- **Reusable recommendation:** Separate editable action metadata from potentially sensitive content; require an explicit, reviewed feature before storing prompt or repository text.

## 193. Derive capability snapshots from the adapter (2026-09-17)

- **Environment:** .NET 10 companion hello handshake.
- **Observation:** A hand-maintained capability list can drift from the adapter and cause the plugin to enable or hide the wrong controls.
- **Evidence:** Hello snapshots now derive capabilities and idle action states directly from `ICodexAdapter.ProbeAsync`.
- **Applicability:** Any plugin/companion protocol with capability-gated actions.
- **Reusable recommendation:** Make the adapter probe the single source for advertised actions; never duplicate capability registries in the transport handler.

## 194. SDK assembly contains Action Editor surface but load inspection can be dependency-sensitive (2026-09-17)

- **Environment:** Installed macOS `PluginApi.dll` from Logi Plugin Service.
- **Observation:** Raw assembly strings expose `ActionEditorCommandCollection`, `ActionEditorTextboxBase`, `LoadActionEditorActions`, and related controls, but reflection inspection failed when the inspector was not run with the host's SkiaSharp dependency context.
- **Evidence:** `strings PluginApi.dll` found the public-surface names; ApiInspector reported missing `SkiaSharp, Version=2.88.0.0`.
- **Applicability:** SDK/API inspection for managed plugin hosts.
- **Reusable recommendation:** Treat symbol presence as a lead, not proof of usable signatures; run reflection from the host dependency directory and capture a loadable signature record before implementing editor actions.

## 195. Host assembly inspection may require the complete dependency closure (2026-09-17)

- **Environment:** macOS Logi Plugin Service ARM64 bundle and ApiInspector.
- **Observation:** Supplying the missing SkiaSharp assembly revealed a second missing dependency (`Newtonsoft.Json 13.0.0`), so copying one dependency is insufficient for reliable reflection.
- **Evidence:** ApiInspector progressed past SkiaSharp and then reported Newtonsoft.Json missing.
- **Applicability:** Managed SDK inspection across vendor application bundles.
- **Reusable recommendation:** Inspect from the complete host dependency directory or build a resolver for the full assembly closure; do not infer API signatures from partial-load results.

## 196. Dependency closure continues beyond core rendering libraries (2026-09-17)

- **Environment:** Logi Plugin Service Tool bundle, macOS ARM64.
- **Observation:** After supplying PluginApi, SkiaSharp, and Newtonsoft.Json, reflection reported `YamlDotNet 16.0.0` as another required assembly.
- **Evidence:** ApiInspector progressed through each missing dependency in sequence.
- **Applicability:** Vendor-managed .NET plugin hosts with bundled serializers/rendering libraries.
- **Reusable recommendation:** Implement directory-based assembly resolution against the host bundle; manual dependency copying is brittle and obscures whether the target API is actually loadable.

## 197. Action Editor API is loadable with the complete host dependency set (2026-09-17)

- **Environment:** macOS ARM64 Logi Plugin Service Tool bundle with PluginApi, SkiaSharp, Newtonsoft.Json, and YamlDotNet resolved.
- **Observation:** Reflection successfully loads the Action Editor surface and exposes typed controls and command/action collections.
- **Evidence:** ApiInspector enumerated `ActionEditorCommand`, `ActionEditorAction`, `ActionEditorTextbox`, `ActionEditorListbox`, and `ActionEditor.AddControl<T>`.
- **Applicability:** C# Actions SDK editor implementations on this host family.
- **Reusable recommendation:** Capture signatures only after resolving the host’s full managed dependency closure; this is now sufficient evidence to prototype an editor action, but not yet evidence that registration and persistence work end-to-end.

## 198. Compile Action Editor probes before physical registration (2026-09-17)

- **Environment:** C# plugin targeting .NET 10 with installed macOS PluginApi.
- **Observation:** A minimal `ActionEditorCommand` using the verified constructor and `AddControlEx` compiles, while host registration still requires hardware validation.
- **Evidence:** `ReviewTemplateEditorCommand` compiles with a listbox control; post-build development-link creation is sandbox-blocked, so physical appearance is unverified.
- **Applicability:** Incremental SDK feature adoption.
- **Reusable recommendation:** Separate compile/API evidence from host registration evidence and require a physical assignment test before marking an editor feature complete.

## 199. Expose unverified controls as capability-gated, not simulated success (2026-09-17)

- **Environment:** Phase 2 plugin controls for Fast Mode and Continue in New Task.
- **Observation:** Product-requested controls can be discoverable before a verified host shortcut exists, provided they fail closed and show unavailable feedback.
- **Evidence:** New controls dispatch through `CompanionAdapter`; the current adapter rejects these actions as unsupported.
- **Applicability:** Any staged integration where UI scope precedes platform capability evidence.
- **Reusable recommendation:** Prefer honest unavailable controls over local simulations or inferred success; promote only after adapter capability and physical behavior are verified.

## 200. Record physical validation separately from capability completion (2026-09-17)

- **Environment:** macOS Loupedeck host with provided status-image assets.
- **Observation:** User validation can confirm editor registration, rendering, and feedback behavior even while the underlying Codex capability remains unsupported.
- **Evidence:** User reported all current tests passed after workflow-action and status-image adjustments.
- **Applicability:** Hardware plugin phased delivery.
- **Reusable recommendation:** Record physical test success independently from integration capability; do not mark an action complete until both UI behavior and backend capability are verified.

## 201. Unsupported controls should not present an idle-ready state (2026-09-17)

- **Environment:** Loupedeck dynamic actions with no advertised companion capability.
- **Observation:** Initial `idle` mapping rendered “Ready,” which users reasonably interpreted as functional support.
- **Evidence:** Fast Mode and Continue in New Task showed Ready despite returning unsupported receipts.
- **Applicability:** Capability-gated hardware controls.
- **Reusable recommendation:** Initialize controls to unavailable until a capability probe succeeds; reserve Ready for an actionable, verified path.

## 202. Profile Icon Editor templates override dynamic command images (2026-09-17)

- **Environment:** macOS Logi Plugin Service profile for a Loupedeck dynamic action.
- **Observation:** The host invoked `GetCommandImage` and the plugin returned status frames, but a per-action `.ict` file in the active profile supplied a static background and text instead.
- **Evidence:** `CodexDeck.log` recorded `Status image requested: unavailable`; the active `ActionIcons/$CodexDeck___...ContinueNewTaskCommand.ict` defined `backgroundColor`, `text`, and `textColor`.
- **Applicability:** Any dynamic Loupedeck/Logi Actions command with user-customized Icon Editor styling.
- **Reusable recommendation:** When a dynamic frame appears frozen, inspect the active profile's `ActionIcons` overrides before changing PNGs or callback code. Preserve user styling by moving the exact conflicting `.ict` to a recoverable backup, then reload the plugin.

## 203. Use host-sized PNGs and BitmapBuilder for runtime button frames (2026-09-17)

- **Environment:** macOS Logi Actions C# SDK runtime image path.
- **Observation:** Plugin logs confirmed `GetCommandImage` calls and resource lookup, but raw 256px source images did not visibly render on the reference surface.
- **Evidence:** Official SDK guidance specifies 80×80 embedded PNGs for runtime button images and demonstrates composing them through `BitmapBuilder(imageSize).SetBackgroundImage(...)`.
- **Applicability:** Dynamic bitmap feedback on Loupedeck/Logi Actions devices.
- **Reusable recommendation:** Retain high-resolution RGBA masters, generate 80×80 RGBA runtime derivatives, and return a `BitmapBuilder` image at the requested size. Do not treat PNG compression level as a compatibility choice; use PNG rather than JPEG/SVG for this runtime path.

## 204. Prefer native bitmap fills to isolate runtime rendering faults (2026-09-17)

- **Environment:** macOS Logi Actions C# SDK, dynamic command image callback.
- **Observation:** The host invoked `GetCommandImage` but did not visibly render externally sourced status frames, with no decode errors in logs.
- **Evidence:** The renderer was changed to `BitmapBuilder.Clear(BitmapColor)` so that the returned bitmap is fully host-native and independent of PNG decode, scaling, alpha, or compression.
- **Applicability:** Diagnosing runtime image handoff on hardware plugins.
- **Reusable recommendation:** Use a solid native bitmap fill as the first rendering control test. If it works, reintroduce composited images stepwise; if it does not, investigate host assignment/rendering rather than file formats.

## 205. Native runtime status-color rendering physically verified (2026-09-17)

- **Environment:** macOS Loupedeck surface with Codex Deck dynamic actions.
- **Observation:** Host-native bitmap fills rendered correctly for unavailable workflow controls after externally sourced frames did not visibly render.
- **Evidence:** User supplied physical-device confirmation showing amber backgrounds for Fast Mode and Continue in New Task while their status labels remain visible.
- **Applicability:** Loupedeck/Logi Actions dynamic status feedback.
- **Reusable recommendation:** Use `BitmapBuilder.Clear(BitmapColor)` as the reliable baseline for full-button status backgrounds; add text through the host label or a later BitmapBuilder overlay, not a profile Icon Editor template.

## 206. Status-frame treatments can be composed with native primitives (2026-09-17)

- **Environment:** Logi Actions `BitmapBuilder` runtime image path.
- **Observation:** The SDK exposes rectangles and circles but no dedicated rounded-rectangle method.
- **Evidence:** The status comparison uses a full-bleed fill, an inset status rail, and a rounded frame built from rectangles and four circles.
- **Applicability:** Dynamic hardware-button feedback without external image dependencies.
- **Reusable recommendation:** Start with native primitives for status shells. Maintain textual labels outside the color treatment and validate small-device legibility on physical hardware.

## 207. Keep image-surface capabilities separate from visual product choices (2026-09-19)

- **Environment:** macOS Logi Actions C# plugin using dynamic commands and `BitmapBuilder`.
- **Observation:** A plugin has several distinct image surfaces: package identity, action-picker symbols, static embedded images, dynamic `GetCommandImage()` frames, native bitmap primitives, and host-rendered labels. They have different lifecycle and override behavior.
- **Evidence:** Codex Deck now documents and maps each surface separately; native full fills, rails, and rounded frames render on hardware, while externally sourced runtime PNG frames previously invoked the callback without visibly rendering.
- **Applicability:** General plugin visual design and UX planning; the rendering reliability observation is host/runtime-specific to the tested macOS setup.
- **Reusable recommendation:** Treat the host label as the semantic layer, use native primitives as the baseline for dynamic status shells, and introduce raster/SVG artwork as a separately verified composition step. Document the chosen insertion method per action so future visual redesigns do not accidentally replace a reliable runtime path or hide dynamic feedback with a static profile override.

## 208. Tabler is a suitable provisional outline source (2026-09-19)

- **Environment:** Codex Deck visual asset pass; Tabler Icons 3.46.0 checked on 2026-09-19.
- **Observation:** Tabler provides a large MIT-licensed outline set on a consistent 24×24 grid with customizable stroke and color, making it suitable for temporary action glyphs before a product-specific visual pass.
- **Evidence:** The selected source SVGs are stored under `assets/tabler/` with a per-action mapping and are embedded for later testing; no runtime behavior was changed yet.
- **Applicability:** General plugin icon selection; licensing/version details should be rechecked when assets are replaced.
- **Reusable recommendation:** Prefer a small, named, documented icon subset over an undifferentiated icon bundle. Keep source SVGs separate from runtime derivatives and verify the host's SVG/raster composition path on hardware before making them dynamic.

## 209. Logi SVG image loading can flatten transparent outlines (2026-09-19)

- **Environment:** macOS Logi Actions plugin, `PluginResources.ReadImage()` loading Tabler SVG resources through `GetCommandImage()`.
- **Observation:** SVGs declaring `fill="none"` and `stroke="currentColor"` rendered on the Loupedeck with a white filled background.
- **Evidence:** The source SVG attributes were inspected directly; the white fill appeared only after runtime host loading.
- **Applicability:** Host/runtime-specific to the tested macOS image path; do not generalize to browser or design-tool SVG rendering.
- **Reusable recommendation:** Do not ship transparent SVGs directly through this runtime path without physical verification. Preserve SVG masters, convert to transparent PNG derivatives with a verified renderer, or use native `BitmapBuilder` composition.
# Plugin development findings — curated engineering knowledge
