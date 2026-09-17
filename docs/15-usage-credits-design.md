# Usage and credits panel

## Scope

Read-only visibility for supported Codex plans. No purchases, resets, upgrades, or billing mutations are performed by the plugin.

## Data model

- Five-hour window: used percentage, remaining percentage, reset time.
- Weekly window: used percentage, remaining percentage, reset time.
- Credits: available balance and currency/unit, when the account exposes it.
- Last request cost: amount and timestamp, only when an official signal provides it.
- Plan/account capability: which fields are available; never assume all plans expose the same data.

## Display modes

- **Combined:** one compact panel showing 5-hour and weekly meters plus next reset.
- **Separate:** dedicated 5-hour and weekly actions/panels for larger displays.
- **Credits:** separate optional panel; hidden when unsupported.

## Plan-aware behavior

| Account capability | Display | Action policy |
| --- | --- | --- |
| Five-hour + weekly windows | Combined or separate meters and reset times | Read-only |
| Credits available | Optional credits balance and last-cost panel | Read-only; link to official Usage settings only |
| Banked reset available | Show reset availability and expiry if officially exposed | Never redeem automatically |
| Workspace/business limits without credits | Show supported windows only; label credits unavailable | No inferred balance or cost |
| Unknown/unsupported plan | Show `Usage unavailable for this account` | No polling loop or guessed values |

## User layout preference

Store a stable `usageLayout` preference with these values:

- `combined` — one compact button/panel for both windows.
- `separate` — one button/panel for five-hour and one for weekly.
- `combined_with_credits` — combined windows plus optional credits panel.

If the selected layout requests unsupported data, retain the preference but render that panel as `Unsupported for this account`; never silently substitute another account’s or plan’s value.

## Alert semantics

Use text/icon plus color, never color alone:

- Green: ample remaining allowance.
- Amber: configurable near-limit threshold.
- Red: exhausted/blocked.
- Neutral: unavailable, stale, or unsupported for this plan.

The amber threshold defaults to 20% remaining and may be changed by the user from 1–99%. Invalid thresholds are rejected at validation time.

Reset times should show local time and a relative countdown when fresh. Stale data must show its age and never be presented as current.

## Evidence gate

Implement only after an official, permissioned local signal is identified and tested across relevant plans. Current OpenAI guidance confirms that five-hour and weekly limits, reset behavior, credits, and reset eligibility vary by plan/account; it does not by itself provide a supported plugin-readable local API.

References: [Using Codex with your ChatGPT plan](https://help.openai.com/en/articles/11369540-using-codex-with-your-chatgpt-plan%28.pdf), [How banked Codex resets work](https://help.openai.com/en/articles/20001498-how-banked-codex-resets-work).
