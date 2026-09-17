# Product specification

## 1. Product statement

Codex Deck gives people using Codex Desktop a physical, always-visible control surface for the actions they repeat most often: responding to approvals, moving among tasks, starting a common workflow, and changing reasoning effort. It is a local-first Loupedeck plugin backed by an optional localhost companion.

The product is inspired by the Codex Micro’s documented interaction model: live agent feedback, rapid workflow invocation, dedicated core actions, and a reasoning control. It is not a clone of the Micro, and it must adapt to the controls and displays present on each Loupedeck model.

## 2. Target user and jobs

**Primary user:** a software developer who spends several hours daily in Codex Desktop and alternates between reviewing work, approving actions, debugging, and starting focused tasks.

| Job | Trigger | Desired outcome | Priority |
| --- | --- | --- | --- |
| Respond to an approval | Codex asks for confirmation | Approve/reject without hunting for the window | P0 |
| Return to a waiting task | An agent needs input | Find and focus the right task fast | P0 |
| Stop runaway/incorrect work | Agent is running unexpectedly | Interrupt immediately and clearly | P0 |
| Start a standard workflow | Review/debug/refactor work begins | Open a prefilled task in seconds | P0 |
| Spend more/less reasoning | Task complexity changes | Adjust effort with feedback | P0 |
| Trade speed for depth | Task urgency changes | Toggle Fast mode with verified/unverified feedback | P0 |
| Scan work health | Several tasks are active | Know which task needs attention | P1 |
| Review output where work happens | Agent finishes a change | Open review or editor in one intentional action | P1 |
| Operate parallel agents | Work is delegated | Switch/select agent and understand progress | P2 |

## 3. Goals and non-goals

### Goals

- Make the top five jobs usable in one physical action or gesture.
- Keep response latency under 150 ms for local shortcut dispatch and under 500 ms for a companion acknowledgement.
- Work usefully even when no live Codex status API is available.
- Make status understandable through more than RGB color.
- Preserve user trust by reporting only confirmed capability and action outcome.

### Non-goals for the first release

- Replacing the Codex Desktop interface.
- Sending prompts, code, task data, or telemetry to a cloud service.
- Executing shell, git, deployment, or repository actions.
- Scraping pixels/accessibility trees to manufacture unsupported live state.
- Supporting every Loupedeck model before one reference device is proven.

## 4. Feature requirements

### F-01 Core command controls — P0

Provide labeled controls for Accept, Reject, Interrupt, New Task, Continue in New Task, Fast Mode, Push-to-Talk (when configured), and Focus Codex. Each action dispatches a user-configured desktop shortcut in shortcut mode.

The initial profile also includes an adaptive `Permissions & Connection` button. It guides first-run macOS permission setup, then becomes a persistent connection diagnostic that can be removed by the user after setup. Permission-dependent actions always recheck their own preflight and return the button to the required setup step if access is later revoked.

**Acceptance criteria**

- An unavailable shortcut renders as disabled with setup guidance.
- Press feedback appears within 150 ms.
- Dispatch result is never rendered as completion unless the adapter confirms completion.
- Interrupt requires no confirmation; Accept and Reject act only on Codex’s currently focused approval.
- Approval and rejection remain disabled unless the companion has verified the configured focus path. The surface shows `Focus Codex first` rather than sending a potentially misdirected shortcut.
- A `Cautious approvals` profile provides an optional hold-to-confirm interaction, but the product never relies on a long-press gesture as the sole P0 activation path.

### F-02 Task navigation — P0

Offer Next Task and Previous Task controls. On devices with a dial, a dial turn navigates tasks and a press focuses the highlighted task when the host supports that operation; otherwise it sends the configured navigation shortcut.

**Acceptance criteria**

- The UI communicates whether navigation is `shortcut-only` or `live`.
- A user can restore default bindings after customization.
- Repeated turning is rate-limited so events cannot overwhelm the host.

### F-03 Workflow launcher — P0

Expose Review, Debug, and Refactor as configurable prompt templates. Launch creates a new task only after the user has configured a supported shortcut/template path; it does not silently paste potentially sensitive context.

**Acceptance criteria**

- Templates contain no project data by default.
- Each template is editable, duplicable, and disableable.
- Launch presents a compact success/failure acknowledgement.

### F-04 Reasoning adjustment — P0

Map a dial or paired buttons to reasoning down/up. Use the adapter to discover exact supported levels; if discovery is absent, map only configured shortcuts and display `Higher reasoning`/`Lower reasoning`, not an invented level.

**Acceptance criteria**

- Every change is debounced and visually acknowledged. When custom imagery is needed, prefer a documented Tabler Icon asset; never make color the sole state cue.
- The rendered value becomes exact only after verified state arrives.
- Unsupported controls are hidden or disabled, never dead buttons.

### F-04a Fast Mode — P0

Provide a configurable Fast Mode toggle. In shortcut mode it shows `Fast mode: command sent`; it may display the exact on/off value only when a live adapter confirms it. This responds to a high-frequency control in the official Codex Micro default layout without asserting that every Codex host exposes it.

### F-04b Continue in New Task — P0

Offer a distinct `Continue in new task` action alongside blank `New task`. It carries existing context only through a supported Codex command; in shortcut mode the product must not copy or reconstruct conversation content.

### F-05 Availability and setup — P0

Show one of `Connected`, `Shortcut mode`, `Setup needed`, or `Disconnected`. The setup screen verifies Codex focus and each configured shortcut without storing keystroke data.

### F-06 Live task status — P1

When a supported adapter can provide it, show up to the device’s available task slots with state, compact label, progress activity, and attention indicator. Loupedeck display surfaces should render text/icon; LED-only surfaces use color plus distinct pulse patterns.

**Acceptance criteria**

- Status is sourced from a declared adapter capability, never UI scraping.
- A stale status becomes `Unknown` after 10 seconds without a heartbeat.
- `Needs input` outranks `Failed`, `Running`, `Thinking`, `Done`, and `Idle` in attention presentation.

### F-07 Task selection and attention profiles — P1

For live-capable hosts, offer `Most recent`, `Pinned`, `Priority`, and `Custom` task-slot policies. `Priority` orders `Needs input`, unread/complete, and active work above idle items. A selection mapping may choose whether one press focuses the selected task or merely selects it; the mapping must be shown on the surface or in its accessible label.

### F-08 Contextual controls and review handoff — P1

Provide opt-in dial modes: `Reasoning`, `Composer navigation`, `Conversation scroll`, and `Custom`. Provide configurable `Review changes` and `Open in editor` controls when a supported Codex action exists. These are navigation/review actions only: Git execution, pull-request mutation, deploy, and terminal-command controls remain out of scope.

**Acceptance criteria**

- Each dial mode has a discoverable, non-gesture-only mode switch and its current label is visible in settings/accessibility output.
- `Review changes`/`Open in editor` are disabled if unavailable; they never fall back to running a shell or Git command.
- Live task-slot policies produce deterministic ordering and retain a task identity until it is no longer eligible.

### F-09 Voice and composer controls — P1

When the operating system permits it, offer Push-to-Talk, press-to-toggle recording, Cancel recording, and Send Composer as separately configurable controls. Voice recording state must be visible and the product must stop recording on companion disconnect. It must not depend on a long press being reliable on the target surface.

### F-10 Profiles and customization — P1

Support a portable JSON profile with mappings, templates, colors, reduced-motion preference, and shortcut bindings. Import must preview differences and export must omit any sensitive template content unless the user deliberately includes it.

### F-11 Agent/task focus and queues — P2

Add live task selection, agent slot assignment, and queue navigation only after F-06 has shipped reliably.

## 5. Success measures

Measure locally and only with explicit opt-in:

- Median elapsed time from `Needs input` to Accept/Reject.
- Shortcut dispatch acknowledgement latency.
- Setup completion rate.
- Number of unavailable action attempts per active hour.

No prompt, code, task title, repository path, user identifier, or raw key event may be collected.

## 6. Primary flow

```text
Codex asks for input
  → companion receives verified “needs_input” (P1) or user sees focused Codex (P0)
  → Loupedeck attention treatment appears
  → user presses Accept / Reject / Interrupt
  → plugin sends action intent to companion
  → companion dispatches shortcut or adapter command
  → surface shows accepted, then confirmed result—or a truthful failure/unavailable state
```

## 7. Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Codex has no supported status interface on launch | Deliver shortcut mode first; feature-gate live state |
| Shortcut conflicts vary by OS/user | Setup validation, editable bindings, capability diagnostics |
| A foreground shortcut lands in the wrong app | Companion focus guard; disable approval controls when focus cannot be verified |
| Hardware capabilities differ | Capability-driven layout and a small common control subset |
| Loupedeck platform lifecycle changes | Target the shared Logitech Actions runtime, maintain a device capability matrix, and validate an MX Creative Console path in Phase 0 |
| RGB is inaccessible/ambiguous | Always pair color with glyph/text/pattern; reduced-motion setting |
| A stale task gets approved | P0 operates the currently focused Codex approval; live mode includes freshness and task identity checks |
