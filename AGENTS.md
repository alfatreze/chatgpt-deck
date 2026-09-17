# Codex Deck — contributor instructions

## Mission

Build a reliable local control surface for Codex Desktop. The product must reduce context switching without claiming integrations or statuses it cannot verify.

## Read order

1. `README.md`
2. `plugin-development-findings.md`
3. `docs/01-product-spec.md`
4. `docs/03-architecture.md`
5. `docs/04-protocol-and-state.md`
6. The relevant ticket in `docs/06-backlog.md`

Read `docs/08-decisions.md` before changing scope or architecture.

## Non-negotiable implementation rules

- Keep the Loupedeck plugin presentation-focused. It may render state and dispatch intents, but it must not scrape the Codex UI or contain product-specific automation logic.
- Keep Codex-specific behavior behind the companion’s `CodexAdapter` interface.
- Start in **shortcut mode**. Live status/agent selection may only be enabled after the adapter capability probe proves support on that host.
- Bind all local network endpoints to loopback. Do not transmit prompt text, repository contents, task titles, or telemetry externally.
- Model every action as `accepted`, `running`, `succeeded`, `failed`, `unavailable`, or `cancelled`; never infer success from a keypress alone.
- Preserve accessibility: do not rely on color alone; pair status colors with text/icon/animation and provide reduced-motion mode.
- Do not alter user-defined shortcuts or configuration without explicit confirmation and backup/export.
- Do not rely on long-press gestures for a P0 control: the Loupedeck release notes list inconsistent long-press behavior on Loupedeck+. A separate, visible control is required.
- Treat the Loupedeck and Logitech MX Creative Console runtime as a shared-platform compatibility target until Phase 0 evidence proves a narrower target is safe.
- Treat `plugin-development-findings.md` as a living implementation constraint. Record a dated, reproducible note there when actual SDK/host behavior contradicts it; do not silently work around the difference in action code.
- When an implementation, build, host, packaging, rendering, input, or recovery discovery could help another plugin, append it to `plugin-development-findings.md` before closing the task. Include date, environment/version, observed behavior, evidence, applicability, and a reusable recommendation. Keep project-specific Codex decisions in `docs/`, but preserve general SDK lessons in the findings file.
- The P0 feasibility proof is a minimal C# plugin that loads through the development link, registers one action, and writes a recognizable press log. A successful build is not sufficient evidence.
- Retain a small SDK/API-inspection utility and record the installed host, Plug-in Service path, architecture, runtime, and SDK assembly versions. Installed assemblies are the tie-breaker when documentation is incomplete.

## Engineering conventions

- **Cost routing:** use `gpt-5.6-luna` at `low` reasoning for bounded mechanical work, inspection, and focused implementation. Use `gpt-5.6-terra` at `low` only when Luna’s lower-cost route is unlikely to reliably complete the task. Do not raise reasoning above `low`, switch to a stronger model, or delegate to a stronger agent without first asking the user and explaining the concrete risk/cost trade-off.
- At each material task transition, report whether the recommended model or reasoning level changes. If it does not, state that the current lowest-cost choice remains sufficient; ask before escalating.
- C# is the default language for the plugin, companion, and shared contract because the tested Actions SDK route exposes the required action-editor and dynamic-action behavior. A Phase 0 evidence-backed exception may introduce TypeScript only behind the same protocol boundary.
- Validate every message at the bridge boundary. Keep a versioned, discriminated JSON Schema as the source of truth and generate or hand-maintain matching C# models.
- Put test fixtures in `shared/fixtures`; contract tests must exercise valid, malformed, and version-mismatched payloads.
- Prefer deterministic clocks, adapters, and fake transports in tests.
- Any new action needs: capability requirement, user-visible label, disabled state, success feedback, failure feedback, and a test.
- Action-editor fields must use stable parameter values, validate on Save, and keep failed edits out of persisted configuration. Dynamic action lists refresh when their backing registry/profile changes.
- Do not let static image/layout configuration mask dynamic action feedback. Validate the exact packaged resource names and rendering on physical hardware.

## Definition of done

- The relevant acceptance criteria in `docs/01-product-spec.md` pass.
- Unit and contract tests cover happy path plus unavailable and failed states.
- No secrets, prompt content, code content, or full task labels appear in logs or telemetry.
- Documentation and `docs/06-backlog.md` evidence are updated.
- A manual test has been run on at least one supported Loupedeck surface or its documented simulator, including sleep/wake and profile switching when supported.

## Scope guardrails

Do not add repository/git/terminal command execution, autonomous agent orchestration, or remote syncing in P0. Those capabilities make failure modes substantially riskier and belong behind later design review.
