# Community research: wishes, friction, and stretch hypotheses

**Research date:** 2026-09-16  
**Question:** Which community-reported Codex and Loupedeck needs should change Codex Deck’s scope now, and which deserve controlled validation later?

## Codex Micro workaround scan (2026-09-16)

Community projects show practical substitutes, but they do not make a third-party Loupedeck plugin an officially supported Codex integration:

| Approach | What it provides | Project treatment |
| --- | --- | --- |
| Work Louder Input / native shortcuts | Custom layers and key/dial/joystick shortcuts | Supported shortcut-mode reference |
| BetterTouchTool | Codex Micro keys, encoder turns, and clicks as arbitrary macOS triggers | Useful analogue; avoid simultaneous device ownership |
| `codex-micro-light` | Unofficial LED/ambient-ring control driven by external task signals | Experimental state-rendering research only |
| `microbridge` / `freemicro` | Community HID bridges and multi-agent status experiments | Stretch-only; explicit device-claim and rollback controls required |
| Phone/local companion projects | Shortcut-based remote control inspired by Codex Micro | UX reference; no proof of a Codex Desktop API |

Roadmap implication: strengthen shortcut mode and diagnostics now, while keeping live status/task mutation behind an opt-in research gate. Do not reverse-engineer or claim compatibility with the private Codex Micro bridge in the core release.

## Adaptation assessment

The most transferable ideas are architectural, not hardware-specific:

1. **Capability-gated adapters:** Microbridge only routes actions an integration explicitly advertises. Adopt this for `ShortcutAdapter` and future `LiveCodexAdapter`.
2. **Privacy-safe state projection:** The phone companion exposes generic slot state rather than prompts, titles, or raw events. Adopt this as the maximum state shape for any future companion bridge.
3. **Focus ownership:** Microbridge gives one session ownership and lets approval requests preempt. Adapt as a local focus policy, without claiming task identity until verified.
4. **Event-driven watchers and backoff:** Reuse the event-driven/no-HID-polling principle for companion diagnostics and reconnect behavior.
5. **Verified shortcut compatibility:** The phone project validates expected Codex shortcuts and reports incompatibilities. Adapt as a setup diagnostic for our configurable shortcut mappings.

Not transferable to Loupedeck: vendor HID framing, `node-hid` shims, Work Louder LED commands, device claiming, and Codex Micro process injection. Those target a different hardware/API boundary and remain stretch-only research.

## HID/API reverse-engineering research plan

There is a technically feasible but fragile path to investigate the Codex Micro integration:

1. Capture USB/HID reports from owned hardware while the official app is connected and while it is disconnected.
2. Map input reports, encoder events, LED/status reports, and vendor-channel framing.
3. Inspect locally installed Codex/Work Louder Electron bundles for device-kit packages, local sockets, and exported commands.
4. Build a read-only analyzer before attempting any writes.
5. If writes are tested, isolate them behind an explicit opt-in, device-claim warning, and rollback path.

This can inform a compatibility branch, but it does not create a supported Loupedeck integration: the Loupedeck plugin would still need a separate local bridge and would inherit update/firmware fragility. Production remains shortcut-based and capability-gated. Research must use owned hardware, a disposable profile, and preserve licensing/terms-of-use review.

## Method and confidence

This review combines: (1) current official Codex Micro documentation, (2) current official Loupedeck/Logitech support and release information, (3) the OpenAI Developer Community’s Codex feature-request index and individual requests, and (4) selected public community bug reports as qualitative signals. Feature-request posts are not market-size data; a low-reply post can still identify a real safety or workflow issue. Items were promoted only when corroborated by official product behavior, a clear safety/reliability consequence, or a direct fit with the physical-control product.

| Evidence grade | Meaning |
| --- | --- |
| A | Official documentation or vendor release/support record |
| B | Concrete OpenAI community request with a credible workflow and/or support acknowledgement |
| C | Qualitative third-party/community anecdote; useful for risk discovery, not prevalence |

## Findings and decisions

| Finding | Evidence | Assessment | Plan decision |
| --- | --- | --- | --- |
| Micro supports task state, recent/pinned/priority/custom task selection, focus behavior, Fast Mode, approve/decline, continue-in-new-chat, voice, composer send, review, skills, and configurable dial modes | A — [Codex Micro docs](https://learn.chatgpt.com/docs/features/codex-micro) | Direct interaction benchmark; several controls were absent from our first P0/P1 map | Promoted Fast Mode and Continue in New Task to P0; promoted task policies, dial modes, review/open-editor, and voice/composer controls to P1 |
| Users seek a fast, reliable switch among model/reasoning configurations; existing shortcut behavior is imperfect across modes/platforms | B — [shortcut request](https://community.openai.com/t/custom-keyboard-shortcuts-to-switch-model-and-reasoning/1386403) | Reinforces physical reasoning control, but saved model bundles are not confirmed as a supported command | Keep relative reasoning P0; exact labels/bundles only after capability probe. Do not invent model presets |
| Users want more control over queue advancement and protection when usage is exhausted | B — [queue-control request](https://community.openai.com/t/more-control-over-the-queue-system/1383534), [queue transfer request](https://community.openai.com/t/allow-transferring-queued-tasks-to-a-new-session/1388372) | High utility, but queue mutation needs an official interface and strong recovery semantics | Phase 6 evaluate-only: read-only usage warning first; no auto-resume, auto-spend, or queue deletion |
| Users value one-click review/editor handoff and retaining human control over changes | B — [open-in-editor request](https://community.openai.com/t/what-happened-to-the-open-in-editor-icon/1393877), [proposed-edits discussion](https://community.openai.com/t/feature-request-vs-code-proposed-edits-with-per-block-keep-undo-in-codex/1385204) | A review handoff is low-risk and strongly aligned with physical controls; Git/PR mutation is not | Add capability-gated Review Changes/Open in Editor to P1; retain Git/PR mutation as Phase 6 research |
| Approval safety and the ability to interrupt are recurring trust concerns | B — [rushing-fixes report](https://community.openai.com/t/how-to-stop-codex-from-rushing-fixes/1382830), [auto-review discussion](https://community.openai.com/t/what-triggers-codex-auto-review-why-is-its-usage-so-high-and-can-it-be-disabled/1390124/2) | A physical Approve button amplifies foreground/focus mistakes | Added focus guard and optional cautious-approval profile immediately; Interrupt stays dedicated |
| Loupedeck discontinued product sales; future plug-ins are intended to work through the shared backend with Logitech MX Creative Console | A — [vendor update](https://loupedeck.com/us/blog/important-update-end-of-loupedeck-sales-and-future-development-plans/) | Critical platform-continuity constraint | MX Creative Console became a Phase 0 co-target and compatibility gate |
| Loupedeck known issues include inconsistent long press, sleep/wake problems, profile-state mismatch, and shortcut-copy limitations; its release notes document Node.js SDK support on macOS/Windows | A — [release notes](https://loupedeck.com/us/loupedeck-release-notes-6-3/) | Direct reliability constraints | Prohibit P0 long-press reliance; add sleep/wake/profile switching tests; own configuration in the companion rather than depend on copied Loupedeck shortcut actions |
| Shortcut-based Loupedeck actions target the foreground application and cannot deeply integrate without a target API | A — [Loupedeck support](https://support.loupedeck.com/plugin_profiles_and_custom_profiles.html) | This validates our companion and capability-gated live-adapter design | Added focus guard; retained direct/live integration boundary; no UI scraping |
| Remote/mobile control and richer task boards are active community interests | B — [current Codex requests](https://community.openai.com/c/codex/feature-requests/45) | Materially expands security and privacy scope | Phase 6 research only, gated by threat model and local-first design |
| Reports of input-focus and hardware-bridge performance problems exist | C — [macOS input-focus report](https://www.reddit.com/r/codex/comments/1twg4lj/latest_codex_desktop_input_focus_completely_dies/), [Windows hardware-bridge report](https://www.reddit.com/r/codex/comments/1uxpr1x/severe_codex_windows_app_lag_and_systemwide_mouse/) | Not sufficient to assert prevalence, but enough to require defensive engineering | Companion must be event-driven, avoid HID polling, expose diagnostics, and be tested under disconnect/reconnect load |

## Immediate changes made

1. Added **Fast Mode** and **Continue in New Task** to P0, because they are part of the documented default Micro workflow and require no new risky integration beyond a capability-gated command.
2. Added **foreground focus guard** for approval/rejection, **optional cautious approvals**, and a no-long-press P0 rule.
3. Added P1 **task attention policies**, **dial modes**, **Review Changes/Open in Editor**, and **separately configurable voice/composer controls**.
4. Made **Loupedeck + Logitech MX Creative Console runtime validation** a Phase 0 release gate.
5. Added hardware recovery tests for **sleep/wake, profile switching, and stale action prevention**.

## Phase 6: community-informed stretch validation

Phase 6 exists to test—not promise—the remaining requests. A candidate must satisfy all of the following before it enters the product backlog:

1. **Capability:** a supported local or vendor interface exists; no UI scraping or undisclosed data extraction.
2. **Safety:** a failure cannot misdirect an approval, destroy queued work, spend money, mutate a repository, or leak task content.
3. **Usability:** at least 10 representative users can complete the task more quickly or with fewer errors than the baseline.
4. **Privacy:** a data-flow review proves that prompt/code/task content remains local and absent from telemetry.
5. **Reversibility:** the action is disabled by default, recoverable, and has a clear unsupported state.

The detailed stretch candidates and exit gate live in [the roadmap](05-roadmap.md#phase-6--community-informed-stretch-validation-time-boxed-no-committed-release-scope).

## Research limitations

- Community forum pages change rapidly; this document records the evidence available on the research date rather than a permanent vote count.
- The current official Micro feature set demonstrates a useful interaction model, not an API commitment for third-party Loupedeck plugins.
- Vendor documentation confirms a shared platform direction but Phase 0 still must prove SDK packaging, capabilities, and licensing for the selected target devices.

## Implementation evidence update

The community research remains the source for product prioritization. `plugin-development-findings.md` is now the continuous source for host/SDK implementation behavior. Its initial review changed the plan to use a C# feasibility proof, retain an SDK-inspection utility, validate action-editor persistence and dynamic refresh, use a 75 ms dial coalescing rule, and make physical rendering/reload tests release gates. Its network-device registry and discovery guidance does not apply to this local Codex integration and is intentionally not imported as scope.
