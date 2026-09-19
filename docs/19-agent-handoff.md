# ChatGPT Deck — continuation handoff

Updated: 2026-09-19

This is the compact continuation document for a new agent. It records the current project model, verified evidence, decisions, and the shortest safe path to resume work. General SDK lessons remain in `plugin-development-findings.md`; this file is project state, not a chronological change log.

## Identity and source of truth

- Product name: **ChatGPT Deck** (formerly CodexDeck/Codex Deck).
- Repository: [alfatreze/chatgpt-deck](https://github.com/alfatreze/chatgpt-deck).
- Working directory: `/Users/abel.santos/Downloads/DEV PROJECTS/Loupedeck Agent`.
- Current branch is the local development branch; do not publish unless the user requests it or a phase is explicitly complete.
- Latest local commit at handoff: `bd62a42 build: add deterministic lplug4 packaging workflow`.
- The working tree should be checked before editing. Package artifacts are ignored; the development link is the supported working install.

Read in order: `README.md`, `plugin-development-findings.md`, `docs/01-product-spec.md`, `docs/03-architecture.md`, `docs/04-protocol-and-state.md`, `docs/06-backlog.md`, `docs/08-decisions.md`, then this file and the relevant feature document.

## Mission and boundaries

ChatGPT Deck is a reliable local control surface for Codex Desktop first, with a later ChatGPT Desktop target. The Loupedeck plug-in is presentation-focused: it renders state and sends intents. The companion owns OS integration through `CodexAdapter`; it must not scrape the Codex UI, transmit prompts/repository contents, or pretend that a keypress proves completion. All bridge endpoints are loopback-only and protocol messages are versioned, bounded, authenticated, and correlated.

P0 deliberately excludes terminal/repository mutation, autonomous agent orchestration, remote syncing, and unsupported live task scraping. Live status, task selection, and mutation remain blocked until a supported, permissioned local interface is proven.

## Roadmap and current phase

1. Phase 0 feasibility and the minimal C# development-link plugin: complete.
2. Phase 1 bridge, pairing, shortcut adapter, receipts, permissions, accessibility, and Core profile: substantially implemented and physically exercised on macOS; packaging and final host integration remain the release gate.
3. Phase 2 workflows and convenience controls: implementation exists and is under user validation. Workflow convenience actions were removed; configurable workflow and capability-gated Fast Mode/Continue-in-New-Task remain.
4. Phase 3 live adapter/task status: blocked by missing supported evidence.
5. Later phases cover multi-agent views, usage/queue research, evidence cards, mutation safety, remote/mobile evaluation, Micro HID/API research, and ChatGPT Desktop support. These are evaluation goals, not current guarantees.

The phase-1 completion checklist is in `docs/05-roadmap.md`. The backlog is authoritative for item status; update its evidence when a manual or packaging result changes.

## Verified feature matrix

Verified on a physical Loupedeck surface with the macOS companion unless stated otherwise:

- Open Codex, Focus Codex, and New Task dispatch through the companion shortcut adapter; New Task uses the working macOS `Cmd+N` path and does not require focus.
- Normal Interrupt is a two-step macOS interaction. The first Escape exposes the stop/escape state; the second confirms cancellation. A separate Interrupt (Double Press) action sends both events with a short delay. Focus is required before interrupting.
- Permission diagnostics and connection/companion status actions exist. The status label is intentionally named “Companion Status” to avoid confusing it with Codex connection state.
- Workflow controls, Fast Mode, and Continue-in-New-Task are capability-gated and report `ready`/`unavailable`; no success is inferred from the button press.
- Dynamic status labels and native bitmap status frames render on hardware. Three native treatments are verified: Fast Mode full bleed, Continue-in-New-Task inset rail, and Companion Status rounded frame.
- Accessibility uses text/icon/animation in addition to color; reduced-motion support is part of the model.
- The development-link plugin loads and operates. Action Editor discovery/registration is not yet a release guarantee; see ADR-015.

Not verified or intentionally unavailable: live Codex task status, task scraping/selection/mutation, reliable usage/credit data, multi-agent assignment, remote sync, and a CodexDeck collection in Loupedeck’s Icon Library.

## Images and icons

`docs/12-icon-implementation.md` is the detailed visual reference. The important distinction is:

- `actionicons/` supplies default/reset icons for individual actions.
- `actionsymbols/` supplies the symbols shown by the Loupedeck action picker.
- Neither package surface has been proven to create a new visible Icon Library collection. The absence of a CodexDeck library folder is therefore not evidence that packaged defaults failed.

The current provisional assets are root `assets/tabler/*.svg` line/stroke sources, with black stroke, `fill="none"`, and restored stroke width 2. Package action icons are normalized to SVG 32×32 canvas with a 24×24 viewBox. Focus, InterruptDouble, and Permissions are the packaged action symbols/defaults. Physical testing proved line SVG rendering, width-1 rendering, reset-to-packaged-default behavior, and that the user’s manually selected icon can be replaced by the packaged target.

Important rendering caveat: an externally loaded SVG through the dynamic image path rendered with an opaque white background on macOS, while packaged line SVGs render correctly. Native bitmap composition is the current reliable status-frame path. Do not let static image/layout configuration mask dynamic status feedback.

## Decisions that must not be re-litigated casually

- C# is the production route; target `net10.0` because the installed `PluginApi.dll` requires `System.Runtime 10.0` and the user has a successful net10 baseline (ADR-013).
- Shortcut mode ships before any live adapter (ADR-001); no UI scraping (ADR-003); local-only bridge (ADR-004); receipts are not completion (ADR-005).
- Companion owns Codex behavior; plugin only renders/dispatches (ADR-002).
- Interrupt remains a dedicated safety control and has an explicit two-stage macOS treatment (ADR-007, ADR-014).
- No P0 long-press dependency (ADR-010).
- Findings are living reusable constraints and must receive dated evidence for SDK/host/build/rendering/recovery discoveries (ADR-012).
- Action Editor remains deferred until clean registration and persistence proof (ADR-015).
- Packaged action icons/defaults are a valid release surface; Icon Library discoverability is not a release criterion until host indexing is demonstrated (ADR-016).
- Package archive acceptance and post-install plugin load are separate gates; do not call packaging complete after the installer merely displays success (ADR-017).

## Packaging state and unresolved failure

`CodexDeckPlugin/src/package/metadata/LoupedeckPackage.yaml` is now version `1.0.0`. `scripts/pack-lplug4.py` creates a deterministic POSIX USTAR `.lplug4` with fixed metadata, root `metadata`, `bin`, `actionicons`, and `actionsymbols` directories. This mirrors the community SubtitleEdit package evidence and is documented in `docs/11-build-and-reload.md` and finding 213.

The development link loads. Direct package installation has not passed the full gate:

1. Earlier packages produced the generic “Installation of the add-on failed” dialog.
2. A version `1.0.0` package displayed installer success, then Loupedeck reported a load error and no installed CodexDeck directory remained.
3. A temporary extracted-package link was attempted, but the service did not relaunch with fresh package evidence; the development link was restored.

This is not currently attributed to icons. Treat it as an unresolved package-load/host-runtime/manifest issue. Never delete caches or user plugin data while diagnosing.

## Exact next steps after the user reboots

1. Confirm Loupedeck opens normally with the restored development link and run the existing physical smoke test.
2. Capture a fresh plugin-service log before and after stopping/restarting the service; do not rely on an old load line.
3. Build the package with the documented pack script, disable the development link, and test the `.lplug4` once with a clean service restart.
4. Record three independent results: installer accepted archive, installed directory remains, and plugin service loaded the packaged assembly. A failure at any gate stays a packaging blocker.
5. If it fails, compare the packaged tree against the known-working development tree, inspect manifest/version/target-runtime errors in the fresh log, and compare with the community direct-install layout. Keep the dev link restored while investigating.
6. Only after packaged load succeeds, retest icon-library indexing; treat it as a separate host feature and do not regress actionicons/actionsymbols defaults.
7. Update `docs/06-backlog.md`, `docs/11-build-and-reload.md`, `docs/08-decisions.md`, and `plugin-development-findings.md` with the reproducible result. Commit locally. Publish only when the phase is complete or explicitly requested.

Useful files/scripts: `scripts/pack-lplug4.py`, `scripts/build-and-reload.sh`, `docs/17-macos-manual-test.md`, `docs/12-icon-implementation.md`, `docs/13-action-editor-design.md`, `CodexDeckPlugin/src/package/metadata/LoupedeckPackage.yaml`, and `CodexDeckPlugin.link`.

## Do not redo

- Do not reintroduce Debug/Refactor convenience actions removed during user validation.
- Do not replace native status-frame rendering with external dynamic SVG until the transparency issue is solved and physically verified.
- Do not infer live task state, usage, credits, or mutation support from the desktop UI.
- Do not add permissions merely to make a speculative integration work.
- Do not publish a package or GitHub release based only on a successful archive build or installer dialog.

