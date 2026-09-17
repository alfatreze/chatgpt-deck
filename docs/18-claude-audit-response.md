# Claude audit response — 2026-09-17

## Disposition

The audit correctly identifies a material architecture divergence: the Loupedeck plugin currently performs macOS automation directly while the companion accepts intents but returns fabricated `accepted` receipts. This contradicts the intended thin-plugin/companion-owned automation boundary.

The current repository remains a useful, physically tested macOS scaffold, but it must not be described as a fully conformant Phase 1 architecture. The remediation work is tracked as `R1-01` through `R1-04` in `docs/06-backlog.md` and is required before Phase 2 feature work.

## Verified audit findings

- The companion uses sequential newline-delimited TCP JSON, not the WebSocket transport described in `docs/04-protocol-and-state.md`.
- The companion currently returns `accepted` for syntactically valid action intents without dispatching a known action.
- macOS automation and focus checks currently live in plugin action classes.
- The Action Editor is deferred and must not be presented as complete.
- `ADR-009` was duplicated; the Action Editor decision is now `ADR-015`.

## Items requiring remediation design rather than immediate patching

- Moving automation changes the deployed trust boundary and needs real-server integration tests.
- Windows support requires a Windows validation environment; no untested adapter will be represented as supported.
- ChatGPT Desktop support is a future, opt-in target evaluation. It is not enabled, inferred, or promised by the Codex implementation.

## ChatGPT Desktop future goal

After bridge remediation, evaluate ChatGPT Desktop through a product-neutral target adapter/configuration. The evaluation must establish official integration evidence, target-app identity, supported actions, permission model, privacy constraints, and per-platform behavior. It must never reuse Codex-specific bundle IDs, keyboard shortcuts, or status claims without separate evidence.
