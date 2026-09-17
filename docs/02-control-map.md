# Control map

## Design principles

- Put interrupt and approval on dedicated controls; they should not be hidden in modes.
- A single gesture has a single meaning within a profile.
- Text is the source of truth. Color is a supporting cue.
- The layout adapts to available Loupedeck hardware; it does not pretend every device has keys, screens, dials, and LEDs.

## Default reference layout

This is a logical layout, not a physical model promise.

### Currently verified host actions

The current macOS development plugin exposes `Open Codex`, `Interrupt Codex`, `Focus Codex`, and `New Task` under `Commands###Core`, plus `Companion Status` under `Diagnostics`. `Focus Codex` is a distinct recovery label but currently performs the same app activation as `Open Codex` on macOS. Accept/reject, reasoning, task navigation, templates, and voice controls remain planned and are not represented as implemented actions.

| Zone | Primary binding | Secondary / feedback | Phase |
| --- | --- | --- | --- |
| Safety | Interrupt | Red while pressed; `Interrupted`/`Failed` acknowledgement | P0 |
| Approval | Accept | Green check + `Sent` then confirmed outcome | P0 |
| Approval | Reject | Amber x + `Sent` then confirmed outcome | P0 |
| Mode | Fast Mode | `Fast: sent` or verified on/off label | P0 |
| Tasks | Previous / Next | Press/turn repeats only after 120 ms | P0 |
| Tasks | New Task | Opens configured task-composer path | P0 |
| Tasks | Continue in New Task | Uses only a supported Codex command; no copied context | P0 |
| Voice | Push-to-Talk | Only visible if configured | P0 |
| Workflows | Review | Editable template label | P0 |
| Workflows | Debug | Editable template label | P0 |
| Workflows | Refactor | Editable template label | P0 |
| Reasoning | Dial left/right or −/+ | `Lower` / `Higher`, exact value when verified | P0 |
| Status slots | Select/focus task | State glyph, label, and pulse treatment | P1 |
| Handoff | Review Changes / Open Editor | Disabled unless adapter verifies action | P1 |
| Compose | Push-to-Talk / Send / Cancel | Recording state is text plus animation | P1 |

## Status semantics

| State | Text/glyph | Color cue | Motion (unless reduced motion) | Attention |
| --- | --- | --- | --- | --- |
| `idle` | `Idle` / hollow circle | Neutral | None | None |
| `thinking` | `Thinking` / spark | Violet | Slow breathe | Low |
| `running` | `Running` / play | Blue | Gentle sweep | Low |
| `needs_input` | `Needs input` / hand | Amber | Two short pulses | Highest |
| `failed` | `Failed` / warning | Red | One pulse then steady | High |
| `done` | `Done` / check | Green | Brief confirmation then dim | Medium |
| `unknown` | `Unavailable` / question | Neutral | None | Low |

Color tokens must remain configurable and meet contrast expectations wherever text is rendered. Never encode a state only by color.

## Interaction rules

- **Press:** dispatches one intent and shows `Sending…` immediately.
- **Long press:** not used by any P0 binding; target hardware has known long-press inconsistency. P1 may expose it as an optional duplicate binding only.
- **Dial rotation:** coalesce events over 75 ms; the controller accumulates a signed step count, clamps it to the valid range, and clears queued movement after an explicit Set action. It serializes reads and controls so each new target starts from the latest known state.
- **Double press:** disabled by default to prevent accidental actions, except for an explicitly enabled voice-latch control with visible `Recording` state.
- **Offline:** all remote/live actions are disabled; shortcut controls may remain enabled only if their dispatch path is locally verified.

## P1 attention and dial policies

Live task slots support `Most recent`, `Pinned`, `Priority`, and `Custom` policies. Priority order is `needs_input`, unread `done`, `running`/`thinking`, then `idle`; tie-break by most recently updated. The display marks the current policy.

The dial has four explicitly selectable P1 modes: Reasoning, Composer navigation, Conversation scroll, and Custom. A hardware surface that cannot show the active mode must reserve a nearby label/button or default to a single documented mode.

## Profiles

`Core` is the default P0 profile. Future profiles: `Review`, `Debug`, `Voice`, and `Multi-agent`. A profile may change layout and templates, but it may not remap Interrupt to an unrelated action.
