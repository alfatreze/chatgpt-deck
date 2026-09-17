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
