# Phased development roadmap

## Priority logic

The first release optimizes for the loop a developer repeats many times a day: **notice → focus → respond → continue**. It does not wait on uncertain live-status integration. Higher-complexity multi-agent features come only once the core control surface is trusted.

## Phase 0 — Discovery spike (3–5 days)

**Objective:** prove the runtime assumptions before committing to the full implementation.

- Select a reference Loupedeck model **and** validate the shared Logitech Actions runtime / MX Creative Console path; document controls, displays, lifecycle, packaging, and local-network permissions.
- Record the installed host/Plugin Service paths, CPU architecture, OS/runtime, selected SDK assembly version, device matrix, development `.link` behavior, and build/reload command.
- Build a minimal **C#** plugin that loads through the development link, registers one action, and emits a recognizable press log; then connect it to a localhost mock companion.
- Add a small SDK/API-inspection utility, and use it to verify uncertain event/property names against installed assemblies.
- Implement a Codex Desktop shortcut inventory and manual setup validator for the reference OS.
- Investigate only official/documented Codex local integration points; record the result as a decision.
- Create protocol schema fixtures and a mock companion.

**Exit gate:** the minimal plugin demonstrably loads, logs a physical/simulated button press, and round-trips to the mock bridge; the team has a definitive `shortcut` capability list, an evidence-backed live-adapter decision, a recorded SDK/host baseline, and a support-matrix decision for Loupedeck versus MX Creative Console.

## Phase 1 — Foundation and safe core (2–3 weeks)

**Objective:** a reliable P0 product in shortcut mode.

- Companion: pairing, localhost server, config store, `ShortcutAdapter`, diagnostics, and action state reducer.
- Platform adapters: keep shared protocol/core logic common; add separate macOS and Windows Codex adapters with runtime OS selection and platform-specific permission guidance.
- Plugin: connection indicator, configurable Core profile, action buttons, disabled/failure feedback, accessibility settings.
- Action editor: Save-time validation, stable action parameter IDs, and dynamic refresh after profile/registry changes.
- Commands: Focus Codex, Accept, Reject, Interrupt, New Task, Continue in New Task, Fast Mode, and task previous/next. Approval/rejection includes a foreground guard.
- Reasoning higher/lower controls, with only unverified relative labels in shortcut mode.
- Automated unit/contract tests and a manual setup flow.

**Exit gate:** all F-01, F-02, F-04, and F-05 acceptance criteria pass on the reference device. Actions are truthfully reported, survive companion restart, and remain correct after action-editor changes and a plugin reload.

**Phase 1 completion handoff:** After the exit gate passes, create the initial GitHub repository and upload the project, preserving the documented findings, decisions, and build instructions. Confirm the repository name, visibility, license, and remote URL in the handoff record.

## Phase 2 — Common workflows and polish (1–2 weeks)

**Objective:** make task start and customization fast enough for daily use.

- Add Review, Debug, and Refactor templates (F-03).
- Add task attention policies (most-recent, pinned, priority, custom) and contextual dial modes (F-07/F-08), when adapter capabilities permit.
- Add Review Changes/Open in Editor controls; deliberately exclude Git mutation and terminal execution.
- Add separately configurable voice/composer controls with recording-state feedback (F-09).
- Add profile export/import preview and backup (F-10 core).
- Add setup wizard, shortcut conflict help, and usage telemetry opt-in shell (no data collection by default).
- Improve encoder event coalescing and device-specific layout adaptation.
- Add a read-only Usage & Credits panel only if an official local usage signal is available: five-hour and weekly usage, reset timestamps, credit balance, and last request cost. Support combined or separate day/week views.

**Exit gate:** templates are editable and launch reliably through configured paths; profile round-trip preserves mappings; manual accessibility checks pass.

## Phase 3 — Verified live state (time-boxed 2-week spike, then 2–4 weeks if viable)

**Objective:** add real agent awareness without scraping or overstating state.

- Implement `LiveCodexAdapter` only if Phase 0 found a supported local integration.
- Add task status slots, fresh/stale state, live task focus, and exact reasoning labels.
- Include task identity with action intents when live task selection is offered.
- Add disconnect, stale-state, and multiple-task stress tests.

**Exit gate:** F-06 passes with verified status provenance, not UI observation. If the integration is unavailable, ship Phase 2 and keep Phase 3 deferred; do not block release.

## Phase 4 — Multi-agent operation (3–5 weeks)

**Objective:** scale the proven status model to parallel tasks.

- Task pinning/slot assignment, attention ordering, agent/task focus, queue navigation, and profile contexts.
- Optional configurable alert policy with quiet hours and reduced motion.

**Exit gate:** users can identify and focus a task requiring input in under five seconds in a five-task scenario, with no ambiguity about status freshness.

## Phase 5 — Ecosystem hardening (ongoing)

- Additional Loupedeck models and OS support.
- Signed installers, migrations, crash recovery, compatibility diagnostics.
- Accessibility audit, performance profiling, and documentation examples.

## Phase 6 — Community-informed stretch validation (time-boxed, no committed release scope)

**Objective:** test recurring community requests without compromising the local-first, trustworthy core product.

| Candidate | Why evaluate | Evidence gate | Safe outcome |
| --- | --- | --- | --- |
| Usage/limit awareness | Community requests emphasize avoiding work that cannot complete and preserving queues | Official supported local signal plus 10-user usability test | Read-only, local warning; no auto-spend or auto-resume |
| Queue pause/resume | Repeated request to stop future work while current work completes | Supported queue-control API and recovery semantics | Explicit Pause Future Work control; never discard queued work |
| Task handoff/evidence card | Users want a compact record of goal, evidence, and open questions between agents | Adapter exposes structured metadata without prompt/code leakage | Read-only task summary, no autonomous supervision |
| Git/PR command controls | Official Micro supports them, but mutation scope is high | Capability, permission, and accidental-activation review | Start with Review/Open only; require separate approval for any mutation |
| Remote/mobile companion | Community interest exists but expands the security boundary | Threat model, pairing design, and offline recovery test | Defer unless local-only security model remains intact |
| Codex Micro HID/API interoperability | Could reveal a compatibility path for richer status/control | Owned-device read-only capture, local bundle inventory, terms review, and explicit go/no-go | Keep research-only; never block shortcut-mode release |

**Exit gate:** each candidate has a written research result, a prototype or explicit rejection, usability evidence, privacy/threat-model review, and a decision in `docs/08-decisions.md`. Nothing graduates merely because it appears on a wishlist.

## Release sequence

| Release | Includes | Explicitly excludes |
| --- | --- | --- |
| `0.1.0-alpha` | mock bridge, reference device button, diagnostics | Codex action dispatch |
| `0.2.0-beta` | P0 shortcut controls and safe feedback | live task state |
| `0.3.0-beta` | templates, profiles, setup polish | multi-agent UI |
| `1.0.0` | hardened P0/P1 only if supported | unsupported status claims |
| `1.x` | live state and multi-agent features as capability allows | cloud sync by default |
| `stretch` | only evidence-gated Phase 6 additions | unsafe automation or unsupported state |

## Critical path and dependencies

```text
Reference SDK/host validation + minimal C# load-and-log proof
  → bridge contract + mock
  → shortcut adapter + pairing
  → core controls + action truthfulness
  → templates/profiles
  ├─→ releaseable shortcut-mode product
  └─→ supported live adapter discovery → live state → multi-agent UI
```
