# Architecture and product decisions

| ID | Decision | Status | Rationale | Revisit when |
| --- | --- | --- | --- | --- |
| ADR-001 | Ship shortcut mode before live task status | Accepted | Core tactile value does not depend on uncertain status integration | a supported Codex local interface is available |
| ADR-002 | Use local companion plus thin plugin | Accepted | Separates hardware UI from OS automation/integration and makes testing practical | SDK supports secure equivalent natively |
| ADR-003 | No UI scraping for status | Accepted | Scraping is brittle, privacy-sensitive, and can yield false actions/status; current official OpenAI developer documentation exposes use cases and API/CLI guidance, but no supported Codex Desktop local control/status interface | only official supported integration changes this |
| ADR-004 | Localhost-only and local-first | Accepted | Prompt/code/task data must not leave the device | user explicitly requests a reviewed sync service |
| ADR-005 | Action receipts are not action completion | Accepted | Key dispatch is not proof that Codex performed an action | live adapter can verify outcome |
| ADR-006 | Reference one Loupedeck model first | Accepted | Hardware SDK/capability uncertainty should be burned down early | P0/P1 are stable |
| ADR-007 | Interrupt remains a dedicated, unmapped safety action | Accepted | Fast cancellation is a core job and should not hide in a mode | strong usability evidence says otherwise |
| ADR-008 | Treat Logitech MX Creative Console as a co-target | Accepted | Loupedeck ended product sales; its stated shared backend makes the Creative Console the continuity path | Phase 0 SDK/device evidence proves otherwise |
| ADR-009 | Guard approval/rejection on verified Codex focus | Accepted | Shortcut-based custom actions target the foreground app and misdirected approval is unacceptable | a direct, task-identified action interface replaces shortcuts |
| ADR-010 | No P0 long-press dependency | Accepted | Loupedeck documents inconsistent long press on Loupedeck+ | cross-device testing shows reliable semantics and a nonessential use case |
| ADR-011 | Use C# as the initial production implementation route | Accepted | Tested project findings show the C# Actions SDK offers the needed editor/dynamic-action behavior; a build alone does not prove host compatibility | Phase 0 validates a supported TypeScript route with equal required capabilities |
| ADR-012 | Treat the findings file as a living engineering constraint | Accepted | It preserves hard-won SDK, host, reload, rendering, and input behavior that generic docs may omit | Replaced by a better reproducible project-specific evidence base |
| ADR-013 | Target `net10.0` for the initial plugin | Accepted | The generated template targets `net8.0`, but the installed `PluginApi.dll` requires `System.Runtime 10.0`; the user also has a prior successful net10 plugin baseline; clean build and host load now succeed | Revisit only if a supported host/API requires a different target |

## Open questions

1. Which Loupedeck model and operating systems are release targets?
2. What current plugin SDK, packaging, signing, and distribution requirements apply to that model?
3. Which Codex Desktop shortcuts are user-configurable and reliably dispatchable on each target OS?
4. Is there an official, permissioned local Codex status/action interface appropriate for `LiveCodexAdapter`?
5. Should task templates use a controlled pasteboard flow or a native task-composer integration when supported?
6. Which Logitech Actions SDK package/version is suitable for a shared Loupedeck/MX Creative Console build, and what are its current signing/distribution requirements?
7. Is an official, user-permissioned usage/queue signal available that does not expose private task content?

An agent may investigate an open question and update this document with source/evidence. It must not silently convert an unanswered question into a product guarantee.
## ADR-015 — Defer SDK Action Editor adoption until host support is proven

**Status:** Accepted pending reproducible registration proof (2026-09-17)

An initial reload did not expose a documented `ActionEditorCommand` prototype. A later UI observation showed a stale editor entry, but a clean reload after source reconciliation removed it and exposed the eight current dynamic actions. Until editor discovery is reproducible from the active source, Core profile configuration remains companion-backed and represented by stable dynamic actions.

**Revisit when:** a host/plugin-service update or official sample demonstrates the required registration/package convention.

## ADR-016 — Treat packaged action icons and Icon Library indexing as separate surfaces

**Status:** Accepted (2026-09-19)

The package may provide `actionicons/` defaults and `actionsymbols/` picker symbols without creating a CodexDeck collection in the host-managed Icon Library. Release acceptance therefore requires correct packaged defaults/reset behavior and physical rendering; Icon Library discoverability is a separate, host-dependent enhancement and must not block the default icon path.

**Revisit when:** the host exposes a documented custom-library registration/indexing mechanism and it is reproduced after a clean install.

## ADR-017 — Require three independent package-release gates

**Status:** Accepted (2026-09-19)

An archive can be syntactically accepted by the installer and still fail during plugin-service load. Packaging is complete only when (1) the installer accepts the archive, (2) the installed package remains on disk, and (3) a fresh plugin-service log proves the packaged assembly loaded and registers actions. The deterministic USTAR packer is retained, but publication is deferred until all three gates pass.

**Revisit when:** a signed/distributed package path replaces local direct installation.

## ADR-014 — Provide explicit two-stage interrupt handling on verified hosts

**Status:** Proposed

Codex on macOS may require two Escape events: the first reveals the stop/escape state and the second confirms cancellation. The plugin may expose a dynamic feedback state such as `Interrupt again`, plus a separately labeled `Interrupt (double press)` action that sends the two events with a short delay. The composite action must remain host-adapter specific, opt-in or capability-gated, and must not be treated as universally safe on Windows or future Codex versions.

**Revisit when:** direct task-aware interruption is available, or Codex changes the macOS interaction.
