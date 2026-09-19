# Temporary Tabler icon set

These outline SVGs are temporary visual defaults for the first working plugin pass. They are sourced from [Tabler Icons](https://tabler.io/icons), version 3.46.0 as checked on 2026-09-19. Tabler Icons is MIT-licensed; retain the upstream license notice when redistributing the assets.

| File | Intended Codex Deck use |
| --- | --- |
| `external-link.svg` | Open Codex |
| `target.svg` | Focus Codex |
| `plus.svg` | New Task |
| `player-stop.svg` | Interrupt Codex |
| `bolt.svg` | Fast Mode |
| `activity.svg` | Continue in New Task / running activity |
| `plug-connected.svg` | Companion Status |
| `shield-check.svg` | Permissions & Connection |

The `outline/` files are filled-path outline masters and are now used as action-picker symbols for three actions under `CodexDeckPlugin/src/package/actionsymbols/`. The `line/` files retain the original stroke-based alternatives. Neither set is substituted into dynamic status frames. The current reliable runtime path remains native `BitmapBuilder` status shells, while the host-rendered label supplies the action and state text.
