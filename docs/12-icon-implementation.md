# Icon implementation

The plugin uses three distinct icon surfaces:

1. **Package identity:** `src/package/metadata/Icon256x256.png` (host-required raster derivative; currently the supplied OpenAI icon).
2. **Action-picker symbols:** small recolorable SVG assets under the package action-icon resource path. These identify actions in the configuration browser.
3. **Runtime button images:** optional `GetCommandImage` overrides loaded through embedded resources and refreshed with `ActionImageChanged`.

Implementation rules:

- Keep the supplied SVG as the editable source and use `currentColor` where recoloring is expected.
- Record source icon name, license, and conversion date in `plugin-development-findings.md`.
- Verify the exact embedded-resource name with the SDK assembly/resource listing before calling `PluginResources.ReadImage`.
- Do not add runtime imagery when the text label and state feedback are already sufficient.
- Preserve non-color cues and reduced-motion behavior.

## Dynamic state imagery

For stateful controls such as Interrupt, prefer a small finite set of embedded bitmap frames (normal, attention/red, unavailable) returned by `GetCommandImage()` and refreshed with `ActionImageChanged()`. The installed Plugin API binary contains `CreateImage` and `ReplaceImageColor` symbols, but these are not treated as public API until their callable signatures are confirmed by reflection or official SDK documentation. Verify each frame on physical hardware and retain text/icon cues for accessibility.
# Status image set

Status PNGs are embedded from `assets/status/` and selected by normalized action status. Action-specific icons remain deferred; status images provide color-coded feedback while labels retain the semantic text.

The master status assets remain 256×256 RGBA PNGs. The Actions SDK documents 80×80 PNG button images, but the current host-native renderer uses `BitmapBuilder.Clear(BitmapColor)` for status backgrounds. That removes file decoding/scaling from the live rendering path while retaining the source PNGs as visual specifications. Runtime button frames should use PNG when a raster asset is required; changing PNG compression is not a separate compatibility mode because PNG compression is lossless.

### Profile override constraint

The Icon Editor can create an action-specific `.ict` file in the active Logi profile. That static icon template can override a dynamic command's `GetCommandImage()` output. Dynamic-status actions must not be manually styled through the Icon Editor; if an existing user template blocks the runtime frame, preserve it in a backup and remove the active override before reloading the plugin.

### Runtime status-frame comparison

The current physical comparison assigns a distinct host-native treatment to three controls: Fast Mode uses a full-bleed status color, Continue in New Task uses a narrow status rail on a dark background, and Companion Status uses a rounded status frame around a dark center. All keep the host-rendered textual label and status so color is supplementary.

## Image capabilities and composition options

This section is the visual-design reference for future UX passes. It distinguishes what the Logi Actions host can render from what has been physically verified in this plugin.

### Image surfaces

| Surface | How it is supplied | Best use | Current status |
| --- | --- | --- | --- |
| Package icon | Required packaged raster (`Icon256x256.png`) | Plugin identity in the host | In use; source artwork may be SVG, but the package uses a raster derivative |
| Action-picker icon | SVG in the packaged `actionsymbols/` folder, named for the action class | Identifying an action while configuring a button | Now applied for Focus, Double-Press Interrupt, and Permissions using filled-path outline SVGs |
| Static action image | Embedded PNG returned by `PluginResources.ReadImage(...)` | A stable icon or a small finite set of known frames | Supported; `interrupt-attention.png` is in use |
| Dynamic action image | `GetCommandImage(...)`, invalidated with `ActionImageChanged()` | State-dependent feedback | Supported and physically verified |
| Host text label | `GetCommandDisplayName(...)` | Action name, state, result, and instructions | Required semantic layer; do not replace it with color or artwork |
| Native generated bitmap | `BitmapBuilder` primitives converted with `ToImage()` | Solid fills, rails, borders, rounded frames, and other deterministic shells | Most reliable runtime path on the current macOS host |

### Insertion and composition methods

1. **Full-frame fill** — clear the entire image to a status color. This maximizes visibility and is useful for a simple, high-salience state. It is currently used by Fast Mode and the general fallback renderer.
2. **Inset rail or band** — clear to a neutral background, then fill a narrow rectangle at the edge. This preserves a stable dark field for text while retaining a persistent status cue. It is currently used by Continue in New Task.
3. **Rounded frame** — construct a border from rectangles and circles, then clear the interior to the neutral background. The SDK has no dedicated rounded-rectangle primitive in the verified API, so the helper composes four corner circles with horizontal and vertical rectangles. It is currently used by Companion Status.
4. **Embedded raster insertion** — load a packaged PNG as a `BitmapImage`. Use this for a stable glyph or finite state frame when the artwork itself is important. Keep masters separate from device-sized derivatives.
5. **Native-plus-raster composition** — draw a native status shell and place a transparent glyph or other artwork over it if the SDK/API supports the required composition operation. This is a design candidate, not yet the default runtime path; validate the exact API and physical rendering first.
6. **Text overlay** — draw text into the bitmap only when necessary and when the host label cannot provide the required layout. Prefer the host label because it is already the semantic/action-name channel and is easier to update consistently.
7. **Static profile/Icon Editor styling** — user-created `.ict` templates can supply a fixed visual, but they may override `GetCommandImage()`. They are not appropriate for dynamic-status controls unless the override is intentional and documented.

### Asset guidance

Use transparent RGBA PNG or SVG masters for artwork. Keep a 256×256 master for review and generate any smaller runtime derivative explicitly. The SDK documentation references 80×80 button PNGs; PNG compression is lossless and is not a separate rendering mode. Avoid assuming that a valid file will render dynamically: the current host failed to visibly display externally sourced runtime frames while invoking the callback, whereas native `BitmapBuilder` fills rendered correctly.

The temporary Tabler set in `assets/tabler/` uses `external-link`, `target`, `plus`, `player-stop`, `bolt`, `activity`, `plug-connected`, and `shield-check` for the obvious Codex Deck actions. The SVG sources are retained for later transparent PNG conversion, but are not currently used as runtime images because the tested Logi SVG rasterizer rendered a white background despite the source `fill="none"` declaration.

### Action-to-method map

| Action/control | Dynamic state image method | Composition | Semantic/status text |
| --- | --- | --- | --- |
| Open Codex | `StatusImages.For` | Full-frame native fill | Host label plus transient receipt state |
| Focus Codex | Packaged action symbol plus host default runtime image | Filled-path outline `target` symbol in the action picker | Host label; action is intentionally simple |
| New Task | `StatusImages.For` | Full-frame native fill | Host label plus unavailable/permission guidance |
| Interrupt Codex | `StatusImages.For`, with embedded `interrupt-attention.png` for the second-press state | Native fill normally; embedded attention frame during confirmation | Host label changes to “Press again to interrupt” |
| Interrupt Codex (Double Press) | Packaged action symbol plus host default runtime image | Filled-path outline `player-stop` symbol in the action picker | Host label plus receipt state |
| Fast Mode | `StatusImages.For(..., FullBleed)` | Full-frame native fill | Host label plus status |
| Continue in New Task | `StatusImages.For(..., InsetRail)` | Dark field with colored status rail | Host label plus status |
| Companion Status | `StatusImages.For(..., RoundedFrame)` | Rounded colored border with dark center | Host label plus connection state |
| Counter | `StatusImages.For` | Full-frame native fill | Host label plus counter/status |
| Test Permissions | Host default currently | Text-led diagnostic feedback; dynamic image treatment remains available for a later pass | Host label plus permission guidance |
| Permissions & Connection | Packaged action symbol plus host default runtime image | Filled-path outline `shield-check` symbol in the action picker | Host label plus permission guidance |

### Design and verification rules

- Keep the action label and state text visible; color, borders, and imagery are supplementary cues.
- For each new visual, specify the default, checking/running, succeeded, failed, unavailable, and cancelled/unsupported appearances before implementation.
- Validate both the editor preview and a physical surface. A profile `.ict` override can mask the runtime image even when callbacks are working.
- Prefer deterministic native primitives for status shells. Add raster artwork only when it contributes meaningfully, then verify its scaling, alpha, contrast, and reload behavior on hardware.
- Record new host/API discoveries in `plugin-development-findings.md`; keep product-specific visual choices in this document.
