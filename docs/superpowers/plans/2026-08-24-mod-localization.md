# Mod Localization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add shared TerrariaModder localization support and localize JourneyBuilder plus framework-visible text for installed mods.

**Architecture:** Core loads flat per-mod resources and resolves active culture with exact/family/English/source fallback. ModMenu uses the resolver for manifests, keybinds, and config metadata; JourneyBuilder uses it for its custom panel. Third-party DLL literals remain outside the safe automatic boundary.

**Tech Stack:** C#/.NET Framework 4.8, Terraria Localization API, existing TerrariaModder UI and config systems, dependency-free .NET tests.

---

### Task 1: Add failing localization behavior tests

**Files:**
- Create: `tests/LocalizationTests.cs`
- Modify: `tests/Program.cs`

- [ ] Add assertions for exact culture, language-family fallback, English fallback, and `@TerrariaKey` fallback.
- [ ] Run `dotnet run --project tests\JourneyBuilderLogicTests.csproj`; confirm the new API tests fail before implementation.

### Task 2: Implement Core localization service

**Files:**
- Create: `E:\Github\terraria-modder\src\Core\Localization\LocalizationManager.cs`
- Modify: `E:\Github\terraria-modder\src\Core\PluginLoader.cs`
- Modify: `E:\Github\terraria-modder\src\Core\Manifest\ModManifest.cs`
- Modify: `E:\Github\terraria-modder\src\Core\Manifest\ManifestParser.cs`

- [ ] Add culture normalization, resource loading from `core/localization` and `<mod>/localization`, and resolver APIs.
- [ ] Register localization after CoreConfig is available and after each manifest is parsed.
- [ ] Parse an optional manifest `localization` directory while keeping `localization` as the default.

### Task 3: Localize F6 framework metadata

**Files:**
- Modify: `E:\Github\terraria-modder\src\Core\UI\ModMenu.cs`

- [ ] Resolve mod name/description, keybind label/description, and config labels/descriptions through LocalizationManager at draw time.
- [ ] Keep source strings as fallback and preserve config JSON keys.

### Task 4: Localize JourneyBuilder panel and resources

**Files:**
- Create: `localization/en.json`
- Create: `localization/zh-Hans.json`
- Create: `localization/zh-Hant.json`
- Create: `localization/ja.json`
- Modify: `Mod.cs`
- Modify: `UI/JourneyBuilderPanel.cs`
- Modify: `manifest.json`
- Modify: `JourneyBuilder.csproj`

- [ ] Use the Core resolver for all panel strings and register the current mod folder through `ModContext`.
- [ ] Add English, Simplified Chinese, Traditional Chinese, and Japanese resources; use Terraria key references for standard UI terms where stable.
- [ ] Copy localization resources into the build package.

### Task 5: Add installed-mod framework resource overlays

**Files:**
- Create: `deployment/localization/<mod-id>/<culture>.json` for each installed mod manifest.

- [ ] Add manifest name, description, keybind, and known config labels for installed mods with source or declaration metadata.
- [ ] Copy overlays to `TerrariaModder\mods\<mod-id>\localization` without replacing DLLs.

### Task 6: Verify and deploy

- [ ] Run logic tests.
- [ ] Build Core and JourneyBuilder against `F:\SteamLibrary\steamapps\common\Terraria`.
- [ ] Validate package resources and manifest consistency.
- [ ] Deploy JourneyBuilder package and localization overlays.
