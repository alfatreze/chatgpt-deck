# Codex Deck

Codex Deck is a Loupedeck plugin and local companion service that turns the most frequent Codex actions into glanceable, tactile controls. It takes its interaction inspiration from the [Codex Micro collaboration with Work Louder](https://openai.com/supply/co-lab/work-louder/): agent status at a glance, fast workflow launch, dedicated approval controls, and in-the-moment reasoning adjustment.

This repository contains the product specification plus an actively built shortcut-mode plugin and localhost companion. It deliberately starts with controls that work with Codex Desktop today, while reserving live agent-state controls for a supported local integration adapter.

## What ships first

1. **Act on the current task** — accept, reject, interrupt, Fast mode, and safe focus handling.
2. **Move between work** — previous/next task, new task, and continue-in-new-task.
3. **Start common work** — review, debug, and refactor prompt templates.
4. **Tune reasoning** — up/down controls with a visible confirmation.

The full prioritized scope and success criteria are in [the roadmap](docs/05-roadmap.md). Read [the product specification](docs/01-product-spec.md) before proposing implementation changes.

## Documentation map

| Document | Purpose |
| --- | --- |
| [AGENTS.md](AGENTS.md) | Operating agreement for humans and AI agents |
| [Product specification](docs/01-product-spec.md) | Users, jobs, features, scope, and acceptance criteria |
| [Control map](docs/02-control-map.md) | Default Loupedeck layout, gestures, and feedback |
| [Architecture](docs/03-architecture.md) | Plugin/companion boundaries and capability strategy |
| [Protocol and state](docs/04-protocol-and-state.md) | Stable bridge contract, state machine, and failure behavior |
| [Roadmap](docs/05-roadmap.md) | Phases, exit gates, risks, and sequencing |
| [Backlog](docs/06-backlog.md) | Ordered implementation tickets ready for delegation |
| [Quality plan](docs/07-quality-plan.md) | Test matrix, telemetry, privacy, and release checklist |
| [Decisions](docs/08-decisions.md) | Assumptions and decisions that agents must preserve |
| [Community research](docs/09-community-research.md) | Evidence log, promoted requirements, and stretch experiments |
| [Plugin development findings](plugin-development-findings.md) | Living implementation reference from a tested Loupedeck/Logi Actions plugin |
| [Pairing hardening](docs/14-pairing-hardening.md) | Release security and recovery checklist for the local companion |
| [GitHub handoff](docs/16-github-handoff.md) | Phase 1 publication preflight and verification checklist |
| [Claude audit response](docs/18-claude-audit-response.md) | Architecture gap disposition, remediation, and ChatGPT Desktop future track |
| [Usage and credits design](docs/15-usage-credits-design.md) | Capability-gated read-only usage, reset, and credit display |
| [macOS manual test](docs/17-macos-manual-test.md) | Reproducible permission and New Task validation steps |

## Proposed repository shape

```text
codex-deck/
├── AGENTS.md
├── README.md
├── docs/                 # source of truth until implementation begins
├── plugin/               # Loupedeck SDK package (later)
├── companion/            # localhost bridge to Codex (later)
├── shared/               # protocol types and fixtures (later)
└── tests/                # contract, unit, and end-to-end tests (later)
```

## Start an implementation task

Give an agent this compact brief:

> Implement the next unchecked highest-priority ticket in `docs/06-backlog.md`. Follow `AGENTS.md`, preserve the public bridge contract in `docs/04-protocol-and-state.md`, add the specified tests, and update the ticket’s evidence field. Do not build live status integration until its capability probe is implemented.

## Product constraints

- Never use a cloud service or send prompt/code contents off-device.
- Never fake an action’s success: show `unavailable`, `failed`, or `pending` truthfully.
- Do not depend on a particular Loupedeck model; expose controls according to discovered hardware capabilities.
- Treat destructive/irreversible Codex operations as confirmation-required unless Codex itself is already asking for approval.

## First-run macOS permissions

Assign the included `Permissions & Connection` action to a spare control. On first use, macOS may ask whether **Logi Plugin Service** can control **System Events**; choose **Allow** to enable shortcut actions. The action explains the next step, opens Accessibility settings when needed, and can be checked again with `Test Permissions`. No prompts, code, or task content are read by this flow.

## Implementation baseline

`plugin-development-findings.md` is a living, mandatory reference for implementation work. It records tested Loupedeck/Logi Actions behavior that generic SDK documentation often omits. Its requirements are incorporated into the feasibility gate and quality plan; update it with concise, reproducible evidence when this project confirms or disproves a finding.

Important discoveries are deliberately recorded there for future plugins too. Every such update should include the environment/version, observed behavior, evidence, scope of applicability, and a reusable recommendation.

## Build status

Phase 0 is complete for the reference Loupedeck Live scaffold. The plugin now exposes eight dynamic actions, including guarded single- and double-stage interrupt controls, a localhost companion transport, authenticated endpoint discovery, dynamic permission feedback, and connection diagnostics. The normal Interrupt action shows a red attention frame and `Press again to interrupt` after the first accepted Escape. The current runtime is a physically tested macOS scaffold, not yet a fully conformant companion-owned automation architecture; bridge remediation is tracked before Phase 2 work. See [the audit response](docs/18-claude-audit-response.md), [development environment baseline](docs/10-development-environment.md), and [backlog](docs/06-backlog.md).

## Verification shortcuts

- `scripts/regression_gate.sh` — builds both .NET projects and runs all protocol/fixture smoke tests.
- `scripts/github_preflight.sh` — checks repository hygiene before the Phase 1 GitHub handoff.

## External reference

OpenAI describes the Codex Micro as supporting live agent-state feedback, workflow launch, accept/reject/push-to-talk/new-chat controls, and an in-the-moment reasoning dial. This project translates those interaction principles—not its hardware implementation—to Loupedeck surfaces. [OpenAI Supply Co. × Work Louder](https://openai.com/supply/co-lab/work-louder/)
