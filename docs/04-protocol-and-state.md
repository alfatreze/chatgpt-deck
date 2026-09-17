# Local bridge protocol and state

## 1. Transport

- Localhost WebSocket, JSON UTF-8, protocol name `codex-deck.v1`.
- Plugin authenticates with an installation-scoped token supplied during pairing.
- All messages are discriminated by `type`, carry `protocolVersion: 1`, and have a UUID `id` where a reply is expected.
- Unknown fields are ignored; unknown message types receive an `unsupported` error.
- JSON Schema is the protocol source of truth. The TypeScript-shaped definitions below are explanatory only; the production plugin and companion use matching validated C# models.

## 2. Messages

```ts
type ActionName =
  | 'accept' | 'reject' | 'interrupt' | 'new_task' | 'continue_new_task'
  | 'fast_mode_toggle' | 'push_to_talk' | 'voice_toggle' | 'voice_cancel' | 'send_composer'
  | 'task_previous' | 'task_next' | 'launch_review' | 'launch_debug'
  | 'launch_refactor' | 'reasoning_up' | 'reasoning_down' | 'focus_task'
  | 'review_changes' | 'open_in_editor' | 'dial_mode_set';

type ActionIntent = {
  type: 'action.intent'; protocolVersion: 1; id: string;
  action: ActionName; source: 'hardware'; taskId?: string;
  requestedDialMode?: 'reasoning' | 'composer' | 'conversation' | 'custom';
};

type ActionReceipt = {
  type: 'action.receipt'; protocolVersion: 1; id: string;
  status: 'accepted' | 'unavailable' | 'failed';
  reason?: 'not_configured' | 'not_supported' | 'not_connected' | 'not_focused' | 'permission_denied' | 'execution_failed';
};

type StateSnapshot = {
  type: 'state.snapshot'; protocolVersion: 1;
  mode: 'setup_needed' | 'shortcut' | 'live' | 'degraded';
  capabilities: string[]; updatedAt: string;
  actions: Record<ActionName, { enabled: boolean; status: ActionStatus }>;
  tasks?: TaskSummary[]; reasoning?: { label: string; verified: boolean };
  fastMode?: { label: string; verified: boolean };
  focus?: { codexReady: boolean; detail?: 'not_focused' | 'unknown' };
};

type ActionStatus = 'idle' | 'sending' | 'accepted' | 'running' | 'succeeded' | 'failed' | 'unavailable' | 'cancelled';
type TaskSummary = { id: string; label: string; state: TaskState; updatedAt: string };
type TaskState = 'idle' | 'thinking' | 'running' | 'needs_input' | 'failed' | 'done' | 'unknown';
```

## 3. Action lifecycle

```text
idle → sending → accepted → running → succeeded
                  └───────────────→ failed
idle → unavailable
sending → failed | cancelled
```

Shortcut mode generally ends at `accepted`: the companion can confirm it dispatched an input event, but cannot assert that Codex completed the underlying action. Only a live adapter may emit `running`/`succeeded` based on verified feedback.

## 4. Freshness and ordering

- Snapshots include ISO 8601 `updatedAt`; events include a monotonic connection sequence number.
- The plugin drops older sequence numbers.
- Task state is stale after 10 seconds without a heartbeat and becomes `unknown` after 30 seconds.
- On reconnect, companion sends a complete snapshot before incremental events.

## 5. Error presentation

| Receipt / condition | Surface copy | Recovery |
| --- | --- | --- |
| `not_configured` | `Set up in Codex Deck` | Open plugin settings |
| `not_focused` | `Focus Codex first` | Focus Codex control |
| `permission_denied` | `Automation permission needed` | OS-specific setup help |
| `not_supported` | `Not available in this mode` | No false retry |
| disconnect | `Companion disconnected` | Retrying indicator + diagnostics |
| stale status | `Status unavailable` | Keep non-live shortcut controls distinct |

## 6. Contract tests required before a plugin is wired

1. Valid hello/pairing, snapshot, action receipt, and event streams.
2. Invalid schema, unknown version, oversized payload, missing token, and replayed ID.
3. Out-of-order and stale event rejection.
4. Shortcut-mode receipt cannot transition an action to `succeeded`.
5. Task `needs_input` produces the highest attention render state.
6. `accept` and `reject` return `not_focused` without dispatch when the focus guard is false or unknown.
7. P0 actions never require a long-press event; P1 long-press bindings do not mask a normal press.
