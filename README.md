# JourneyBuilder

JourneyBuilder adds practical Journey Mode-style building and item-management controls to Terraria. Adjust placement range, break range, placement speed, break speed, and item pickup range while you play, then use the settings that fit the task at hand.

Open the draggable JourneyBuilder panel with `O`, or use the F6 Mod Menu for the same configuration. Every setting can be changed with a slider or by typing an exact number directly into the panel.

The mod supports single-player and Host & Play. In multiplayer, configured client values are limited by the server maxima when available; world-item collection and clearing are disabled for remote clients because the server owns those items. This is a gameplay convenience mod, not a server-side anti-cheat system.

---

## Configuration

### Enabled

Enables or disables JourneyBuilder's range and speed changes for the local player.

### Placement Range

Sets the base tile and wall placement range from 1 to 100 tiles. Item-specific placement boosts remain available. The default value of 5 preserves Terraria's normal 5 x 4 placement reach.

### Break Range

Sets the base mining, axe, hammer, and wall-breaking range from 1 to 100 tiles. Item-specific tile boosts remain available. The default value of 5 preserves Terraria's normal 5 x 4 tool reach.

### Placement Speed

Sets the placement speed multiplier for tiles and walls. The effective final speed, including Terraria's existing equipment, buff, and Journey Mode bonuses, is capped at 7.0x to prevent invalid zero-delay placement.

### Break Speed

Sets the mining and breaking speed multiplier. The effective final speed, including Terraria's existing equipment, buff, and Journey Mode bonuses, is capped at 7.0x to prevent invalid zero-delay breaking.

### Item Pickup Range

Sets the local player's automatic world-item pickup range from 1 to 100 tiles. The default is 5 tiles. Terraria's normal item eligibility and inventory or Void Vault acceptance rules still apply.

### JourneyBuilder Panel

Press `O` to open the draggable settings panel. It separates placement, breaking, and item management controls; every numeric setting has both a slider and direct input. It also includes **Pick Up All World Items**, which respects normal inventory and Void Vault acceptance, and **Clear All World Items**, which requires a second click within three seconds. These world-item commands are available only in single-player and Host & Play. **Reset to Vanilla** restores 5-tile ranges, 1.0x speeds, and a 5-tile item pickup range. Press `Esc` or the panel close button to close it.

---

## Installation

Install [TerrariaModder](https://www.nexusmods.com/terraria/mods/135) first. JourneyBuilder requires Terraria 1.4.5 on Windows and must be launched through TerrariaModder, not by starting `Terraria.exe` directly.

### With TerrariaModder Vault

When JourneyBuilder is available in TerrariaModder Vault, search for **JourneyBuilder** in Browse Nexus and install it normally. Launch the game with **Run Modded** after installation.

### Manually

1. Download the latest release from the [Files tab](https://www.nexusmods.com/terraria) or [GitHub](https://github.com/ABKQPO/JourneyBuilder).
2. Extract the downloaded archive.
3. Move the included `journey-builder` folder into `Terraria/TerrariaModder/mods/`.
4. Confirm that the folder contains `JourneyBuilder.dll` and `manifest.json`.
5. Start the game through `TerrariaInjector.exe` or TerrariaModder Vault.

For dedicated multiplayer, install JourneyBuilder on every client that should use its local controls. A modded server can define range and speed maxima, but the mod does not alter Terraria's core network protocol or provide authoritative anti-cheat enforcement. Global world-item panel commands are intentionally unavailable to remote clients.

---

## Questions, Suggestions, Bug Reports and Contributing

For questions, suggestions, or bug reports, open an issue on [GitHub](https://github.com/ABKQPO/JourneyBuilder/issues). Include your Terraria version, TerrariaModder version, whether the issue occurs in single-player, Host & Play, or dedicated multiplayer, and any relevant `terrariamodder.log` entries.

Contributions are welcome through GitHub issues and pull requests. Keep reports focused on one problem or feature request so they can be reproduced and reviewed clearly.

---

## Credits

Thanks to **Re-Logic** for Terraria and its Journey Mode building systems.

Thanks to **Inidar1** and the **TerrariaModder** project for the framework, configuration UI, widget library, and runtime patching support.

Thanks to **ConfuzzedCat** for TerrariaInjector, which allows TerrariaModder mods to load without modifying the Terraria game executable.

Thanks to everyone who tested range, speed, panel, and multiplayer behavior and reported issues.
