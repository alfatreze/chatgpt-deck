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

The master status assets remain 256×256 RGBA PNGs. Runtime derivatives live in `assets/status-runtime/` as 80×80 RGBA PNGs, matching the Actions SDK runtime-image guidance. They are rendered through `BitmapBuilder(imageSize).SetBackgroundImage(...)`, so the host receives a bitmap at the requested device size. Runtime button frames should use PNG; changing PNG compression is not a separate compatibility mode because PNG compression is lossless.

### Profile override constraint

The Icon Editor can create an action-specific `.ict` file in the active Logi profile. That static icon template can override a dynamic command's `GetCommandImage()` output. Dynamic-status actions must not be manually styled through the Icon Editor; if an existing user template blocks the runtime frame, preserve it in a backup and remove the active override before reloading the plugin.
