# TerrariaModder Mod Localization Design

## Goal

Provide language-aware text for TerrariaModder manifests, keybinds, config metadata, and JourneyBuilder's custom panel, using Terraria's active culture. Chinese Simplified values use the supplied Official Terraria Wiki resource pack as terminology reference; English and Japanese prefer Terraria's built-in localization keys where a vanilla key exists.

## Boundary

Core can localize framework-owned text and any mod that supplies a flat localization resource. It cannot safely rewrite arbitrary literal strings embedded in third-party DLLs. Existing installed mods without source therefore receive manifest/keybind/config coverage only unless they add resource files.

## Architecture

`LocalizationManager` owns active-culture normalization, flat JSON resource loading, fallback order, and optional Terraria `Language.GetTextValue` lookups. `ModManifest` registers a per-mod localization directory. `ModMenu` resolves manifest, keybind, label, and description values at render time. JourneyBuilder uses the same service for its independent panel.

Resource files use flat keys such as `manifest.name`, `keybind.toggle.label`, `config.PlacementRange.label`, and `panel.range`. Existing JSON property names remain unchanged.

## Fallback

Culture order is exact culture, language family, then `en`, then source fallback. Unsupported or malformed resource files are ignored with a warning. A value beginning with `@` is treated as a Terraria localization key; failure falls back to the resource's literal fallback.

## Testing

Dependency-free tests cover culture normalization, fallback order, flat resource parsing, and Terraria-key fallback behavior without requiring the game process. A Release build against the installed Terraria executable validates Core and JourneyBuilder integration.
