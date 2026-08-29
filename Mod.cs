using System;
using System.Reflection;
using HarmonyLib;
using Terraria;
using Terraria.DataStructures;
using TerrariaModder.Core;
using TerrariaModder.Core.Config;
using TerrariaModder.Core.Logging;
using TerrariaModder.Core.Net;
using JourneyBuilder.UI;

namespace JourneyBuilder
{
    public sealed class JourneyBuilderConfig : ModConfig
    {
        public override int Version => 1;

        [Client, Label("Enabled"), Description("Enable JourneyBuilder changes for the local player.")]
        public bool Enabled { get; set; } = true;

        [Client, Label("Placement Range"), Description("Base placement range in tiles. Item-specific tile boosts are preserved."), Range(1, 100)]
        public int PlacementRange { get; set; } = 5;

        [Client, Label("Break Range"), Description("Base tool and mining range in tiles. Item-specific tile boosts are preserved."), Range(1, 100)]
        public int BreakRange { get; set; } = 5;

        [Client, Label("Placement Speed"), Description("Total placement speed multiplier, including vanilla bonuses. The final speed is capped at 7x."), Range(0.1f, SettingsMath.MaxSpeedMultiplier)]
        public float PlacementSpeed { get; set; } = 1f;

        [Client, Label("Break Speed"), Description("Total mining and breaking speed multiplier, including vanilla bonuses. The final speed is capped at 7x."), Range(0.1f, SettingsMath.MaxSpeedMultiplier)]
        public float BreakSpeed { get; set; } = 1f;

        [Client, Label("Item Pickup Range"), Description("Automatic world-item pickup range in tiles."), Range(1, 100)]
        public int ItemPickupRange { get; set; } = 5;

        [Server, Label("Max Placement Range"), Description("Maximum placement range clients may configure."), Range(1, 100)]
        public int MaxPlacementRange { get; set; } = 100;

        [Server, Label("Max Break Range"), Description("Maximum break range clients may configure."), Range(1, 100)]
        public int MaxBreakRange { get; set; } = 100;

        [Server, Label("Max Placement Speed"), Description("Maximum total placement speed, including vanilla bonuses (up to 7x)."), Range(0.1f, SettingsMath.MaxSpeedMultiplier)]
        public float MaxPlacementSpeed { get; set; } = SettingsMath.MaxSpeedMultiplier;

        [Server, Label("Max Break Speed"), Description("Maximum total mining speed, including vanilla bonuses (up to 7x)."), Range(0.1f, SettingsMath.MaxSpeedMultiplier)]
        public float MaxBreakSpeed { get; set; } = SettingsMath.MaxSpeedMultiplier;
    }

    public sealed class Mod : IMod
    {
        private const string HarmonyId = "com.journeybuilder";
        private static Mod _instance;

        private ILogger _log;
        private ModContext _context;
        private JourneyBuilderConfig _config;
        private Harmony _harmony;
        private MethodInfo _itemCheckMethod;
        private MethodInfo _tileReachMethod;
        private MethodInfo _itemGrabRangeMethod;
        private MethodInfo _miningToolMethod;
        private MethodInfo _hitTileAddDamageMethod;
        private MethodInfo _pickWallMethod;
        private MethodInfo _smartToolStrategyMethod;
        [ThreadStatic]
        private static bool _smartToolStrategyActive;
        [ThreadStatic]
        private static float _toolDamageMultiplier;
        private JourneyBuilderPanel _panel;
        private ItemManagementService _itemManagement;
        private Action<int, string> _collectAllServerCommandHandler;
        private Action<int, string> _clearAllServerCommandHandler;
        private bool _collectAllServerCommandRegistered;
        private bool _clearAllServerCommandRegistered;
        private bool _serverResponseSubscribed;
        private DateTime _clampNoticeUntilUtc;

        public string Id => "journey-builder";
        public string Name => "JourneyBuilder";
        public string Version => "1.0.0";

        public void Initialize(ModContext context)
        {
            _instance = this;
            _context = context;
            _log = context.Logger;
            _config = context.GetConfig<JourneyBuilderConfig>();

            if (_config == null)
            {
                _log.Error("JourneyBuilder configuration could not be loaded; leaving vanilla behavior unchanged.");
                return;
            }

            JourneyBuilderLocalization.Initialize(context, _config);

            ClampClientValues();

            _itemManagement = new ItemManagementService(_log);

            if (context.IsServer)
            {
                _collectAllServerCommandHandler = (callerSlot, _) =>
                    _itemManagement?.HandleServerCommand(callerSlot, ItemManagementRules.CollectAllCommand);
                _clearAllServerCommandHandler = (callerSlot, _) =>
                    _itemManagement?.HandleServerCommand(callerSlot, ItemManagementRules.ClearAllCommand);
                _collectAllServerCommandRegistered = ServerCommandRegistry.Register(
                    ItemManagementRules.CollectAllCommand, _collectAllServerCommandHandler);
                _clearAllServerCommandRegistered = ServerCommandRegistry.Register(
                    ItemManagementRules.ClearAllCommand, _clearAllServerCommandHandler);
                if (!_collectAllServerCommandRegistered || !_clearAllServerCommandRegistered)
                    _log.Warn("JourneyBuilder: could not register one or more server world-item commands.");
                _log.Info("JourneyBuilder: dedicated server mode, UI and client patches skipped.");
                return;
            }

            context.RegisterKeybind(
                "toggle",
                JourneyBuilderLocalization.Get("keybind.toggle.label", "Toggle JourneyBuilder Panel"),
                JourneyBuilderLocalization.Get("keybind.toggle.description", "Open or close the JourneyBuilder settings panel"),
                "O",
                TogglePanel);
            NetSync.OnServerCommandResponse += OnServerCommandResponse;
            _serverResponseSubscribed = true;
            _panel = new JourneyBuilderPanel(_config, ClampClientValues, () => DateTime.UtcNow < _clampNoticeUntilUtc, OnPanelChanged, _itemManagement, _log);
            _panel.RegisterDrawCallback();

            _log.Info("JourneyBuilder initialized. Press O to open the settings panel.");
        }

        public static void OnGameReady()
        {
            Mod mod = _instance;
            if (mod == null)
                return;

            JourneyBuilderLocalization.RefreshConfigMetadata(mod._config);
            mod.ApplyPatches();
        }

        private void ApplyPatches()
        {
            if (_context == null || _context.IsServer || _harmony != null)
                return;

            try
            {
                _itemCheckMethod = typeof(Player).GetMethod(
                    "ItemCheck",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    Type.EmptyTypes,
                    null);

                if (_itemCheckMethod == null)
                {
                    _log.Error("JourneyBuilder: Player.ItemCheck() was not found; vanilla behavior will be used.");
                    return;
                }

                _tileReachMethod = typeof(Player).GetMethod(
                    "IsInTileInteractionRange",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] { typeof(int), typeof(int), typeof(TileReachCheckSettings), typeof(int) },
                    null);

                _itemGrabRangeMethod = typeof(Player).GetMethod(
                    "GetItemGrabRange",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] { typeof(WorldItem) },
                    null);

                _miningToolMethod = typeof(Player).GetMethod(
                    "ItemCheck_UseMiningTools_ActuallyUseMiningTool",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                _hitTileAddDamageMethod = typeof(HitTile).GetMethod(
                    "AddDamage",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] { typeof(int), typeof(int), typeof(bool) },
                    null);
                _pickWallMethod = typeof(Player).GetMethod(
                    "PickWall",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] { typeof(int), typeof(int), typeof(int) },
                    null);
                _smartToolStrategyMethod = typeof(Player).GetMethod(
                    "SmartSelect_GetToolStrategy",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                _harmony = new Harmony(HarmonyId);
                _harmony.Patch(
                    _itemCheckMethod,
                    prefix: new HarmonyMethod(typeof(Mod), nameof(ItemCheckPrefix)));

                if (_tileReachMethod != null)
                {
                    _harmony.Patch(_tileReachMethod,
                        prefix: new HarmonyMethod(typeof(Mod), nameof(TileReachPrefix)));
                }
                else
                {
                    _log.Warn("JourneyBuilder: Player.IsInTileInteractionRange() was not found; range limits may remain vanilla.");
                }

                if (_itemGrabRangeMethod != null)
                {
                    _harmony.Patch(_itemGrabRangeMethod,
                        postfix: new HarmonyMethod(typeof(Mod), nameof(ItemGrabRangePostfix)));
                }
                else
                {
                    _log.Warn("JourneyBuilder: Player.GetItemGrabRange(WorldItem) was not found; pickup range remains vanilla.");
                }

                if (_miningToolMethod != null)
                {
                    _harmony.Patch(_miningToolMethod,
                        prefix: new HarmonyMethod(typeof(Mod), nameof(MiningToolPrefix)),
                        postfix: new HarmonyMethod(typeof(Mod), nameof(MiningToolPostfix)));
                }
                else
                {
                    _log.Warn("JourneyBuilder: mining tool method was not found; axe and hammer damage remains vanilla.");
                }

                if (_hitTileAddDamageMethod != null)
                {
                    _harmony.Patch(_hitTileAddDamageMethod,
                        prefix: new HarmonyMethod(typeof(Mod), nameof(HitTileAddDamagePrefix)));
                }
                else
                {
                    _log.Warn("JourneyBuilder: HitTile.AddDamage() was not found; axe and hammer damage remains vanilla.");
                }

                if (_pickWallMethod != null)
                {
                    _harmony.Patch(_pickWallMethod,
                        prefix: new HarmonyMethod(typeof(Mod), nameof(PickWallPrefix)));
                }
                else
                {
                    _log.Warn("JourneyBuilder: Player.PickWall() was not found; wall damage remains vanilla.");
                }

                if (_smartToolStrategyMethod != null)
                {
                    _harmony.Patch(_smartToolStrategyMethod,
                        prefix: new HarmonyMethod(typeof(Mod), nameof(SmartToolStrategyPrefix)),
                        postfix: new HarmonyMethod(typeof(Mod), nameof(SmartToolStrategyPostfix)));
                }

                _log.Info("JourneyBuilder: patched Player.ItemCheck().");
            }
            catch (Exception ex)
            {
                _log.Error("JourneyBuilder: failed to apply Player.ItemCheck() patch.", ex);
                _harmony = null;
            }
        }

        private static void ItemCheckPrefix(Player __instance)
        {
            Mod mod = _instance;
            if (mod == null || mod._config == null || !mod._config.Enabled)
                return;

            try
            {
                if (__instance == null || Main.player == null || Main.myPlayer < 0 ||
                    Main.myPlayer >= Main.player.Length || __instance != Main.player[Main.myPlayer])
                    return;

                Item heldItem = __instance.HeldItem;
                bool isPlacement = heldItem != null &&
                    (heldItem.createTile >= 0 || heldItem.createWall >= 0 || heldItem.tileWand > 0);
                bool isTool = isPlacement || (heldItem != null &&
                    (heldItem.pick > 0 || heldItem.hammer > 0 || heldItem.axe > 0));
                if (!isTool)
                    return;

                int requestedRange = isPlacement ? mod._config.PlacementRange : mod._config.BreakRange;
                int maxRange = isPlacement ? mod._config.MaxPlacementRange : mod._config.MaxBreakRange;
                requestedRange = SettingsMath.ClampToServer(requestedRange, maxRange, 1);

                RangeValues range = SettingsMath.MapRange(requestedRange);
                Player.tileRangeX = range.X;
                Player.tileRangeY = range.Y;

                // Use the configured base range as an absolute value; preserve item tileBoost below
                // the range calculation but discard Journey-mode-style extra block range bonuses.
                __instance.blockRange = 0;

                if (isPlacement)
                {
                    float speed = SettingsMath.ClampToServer(mod._config.PlacementSpeed, mod._config.MaxPlacementSpeed, 0.1f);
                    PlacementSpeedValues placement = SettingsMath.ApplyPlacementSpeed(__instance.tileSpeed, __instance.wallSpeed, speed);
                    __instance.tileSpeed = placement.Tile;
                    __instance.wallSpeed = placement.Wall;
                }
                else if (__instance.pickSpeed > 0f)
                {
                    float speed = SettingsMath.ClampToServer(mod._config.BreakSpeed, mod._config.MaxBreakSpeed, 0.1f);
                    __instance.pickSpeed = SettingsMath.ApplyBreakSpeed(__instance.pickSpeed, speed);
                }
            }
            catch (Exception ex)
            {
                mod._log?.Error("JourneyBuilder: ItemCheck prefix failed; this frame used vanilla values.", ex);
            }
        }

        private static void ItemGrabRangePostfix(Player __instance, ref int __result)
        {
            Mod mod = _instance;
            if (!CanModifyPlayer(__instance, mod))
                return;

            try
            {
                __result = ItemManagementRules.ToPickupPixels(mod._config.ItemPickupRange);
            }
            catch (Exception ex)
            {
                mod._log?.Error("JourneyBuilder: item pickup range postfix failed; vanilla range was used.", ex);
            }
        }

        private static void MiningToolPrefix(Player __instance, Item sItem, int x, int y, ref float __state)
        {
            Mod mod = _instance;
            __state = _toolDamageMultiplier;
            _toolDamageMultiplier = 0f;
            if (!CanModifyPlayer(__instance, mod) || sItem == null || Main.tile == null ||
                !WorldGen.InWorld(x, y, 1))
                return;

            Tile target = Main.tile[x, y];
            if (target == null ||
                !((sItem.axe > 0 && Main.tileAxe[target.type]) ||
                  (sItem.hammer > 0 && Main.tileHammer[target.type])))
                return;

            _toolDamageMultiplier = SettingsMath.ClampToServer(
                mod._config.BreakSpeed, mod._config.MaxBreakSpeed, 0.1f);
        }

        private static void MiningToolPostfix(Player __instance, Item sItem, int x, int y, float __state)
        {
            _toolDamageMultiplier = __state;
        }

        private static void HitTileAddDamagePrefix(ref int damageAmount)
        {
            if (_toolDamageMultiplier > 0f && damageAmount > 0)
                damageAmount = SettingsMath.ApplyToolDamage(damageAmount, _toolDamageMultiplier);
        }

        private static void PickWallPrefix(Player __instance, ref int damage)
        {
            Mod mod = _instance;
            if (!CanModifyPlayer(__instance, mod) || damage <= 0)
                return;

            float speed = SettingsMath.ClampToServer(
                mod._config.BreakSpeed, mod._config.MaxBreakSpeed, 0.1f);
            damage = SettingsMath.ApplyToolDamage(damage, speed);
        }

        private static void SmartToolStrategyPrefix(Player __instance)
        {
            Mod mod = _instance;
            _smartToolStrategyActive = CanModifyPlayer(__instance, mod);
        }

        private static void SmartToolStrategyPostfix()
        {
            _smartToolStrategyActive = false;
        }

        private static bool CanModifyPlayer(Player player, Mod mod)
        {
            return mod != null && mod._config != null && mod._config.Enabled &&
                player != null && Main.player != null && Main.myPlayer >= 0 &&
                Main.myPlayer < Main.player.Length && player == Main.player[Main.myPlayer];
        }

        private static void TileReachPrefix(Player __instance, ref TileReachCheckSettings settings)
        {
            Mod mod = _instance;
            if (!CanModifyPlayer(__instance, mod))
                return;

            try
            {
                Item heldItem = __instance.HeldItem;
                bool isPlacement = heldItem != null &&
                    (heldItem.createTile >= 0 || heldItem.createWall >= 0 || heldItem.tileWand > 0);
                bool isTool = isPlacement || (heldItem != null &&
                    (heldItem.pick > 0 || heldItem.hammer > 0 || heldItem.axe > 0));
                if (!isTool && !_smartToolStrategyActive)
                    return;
                int requestedRange = isPlacement ? mod._config.PlacementRange : mod._config.BreakRange;
                int maxRange = isPlacement ? mod._config.MaxPlacementRange : mod._config.MaxBreakRange;
                RangeValues range = SettingsMath.MapRange(
                    SettingsMath.ClampToServer(requestedRange, maxRange, 1));

                // Terraria 1.4.5 Simple settings cap reach at 20 tiles. Explicit
                // overrides are required for configured ranges above that limit.
                settings.TileReachLimit = null;
                settings.OverrideXReach = range.X;
                settings.OverrideYReach = range.Y;
                settings.TileRangeMultiplier = 1;
            }
            catch (Exception ex)
            {
                mod._log?.Error("JourneyBuilder: tile reach prefix failed; vanilla reach was used.", ex);
            }
        }

        private void TogglePanel()
        {
            if (_config == null || _panel == null)
                return;

            ClampClientValues();
            _panel.Toggle();
        }

        private void OnPanelChanged()
        {
            ClampClientValues();
        }

        private void OnServerCommandResponse(string command, string result)
        {
            _itemManagement?.HandleServerCommandResponse(command, result);
        }

        private void ClampClientValues()
        {
            if (_config == null)
                return;

            int oldPlacementRange = _config.PlacementRange;
            int oldBreakRange = _config.BreakRange;
            int oldItemPickupRange = _config.ItemPickupRange;
            float oldPlacementSpeed = _config.PlacementSpeed;
            float oldBreakSpeed = _config.BreakSpeed;

            _config.MaxPlacementSpeed = Math.Max(0.1f, Math.Min(SettingsMath.MaxSpeedMultiplier, _config.MaxPlacementSpeed));
            _config.MaxBreakSpeed = Math.Max(0.1f, Math.Min(SettingsMath.MaxSpeedMultiplier, _config.MaxBreakSpeed));

            _config.PlacementRange = SettingsMath.ClampToServer(_config.PlacementRange, _config.MaxPlacementRange, 1);
            _config.BreakRange = SettingsMath.ClampToServer(_config.BreakRange, _config.MaxBreakRange, 1);
            _config.ItemPickupRange = ItemManagementRules.ClampPickupTiles(_config.ItemPickupRange);
            _config.PlacementSpeed = SettingsMath.ClampToServer(_config.PlacementSpeed, _config.MaxPlacementSpeed, 0.1f);
            _config.BreakSpeed = SettingsMath.ClampToServer(_config.BreakSpeed, _config.MaxBreakSpeed, 0.1f);

            if (oldPlacementRange != _config.PlacementRange || oldBreakRange != _config.BreakRange ||
                oldItemPickupRange != _config.ItemPickupRange ||
                Math.Abs(oldPlacementSpeed - _config.PlacementSpeed) > 0.0001f ||
                Math.Abs(oldBreakSpeed - _config.BreakSpeed) > 0.0001f)
            {
                _clampNoticeUntilUtc = DateTime.UtcNow.AddSeconds(5);
                _log?.Warn("JourneyBuilder: local settings were clamped to the server limits.");
            }
        }

        public void OnConfigChanged()
        {
            ClampClientValues();
            _panel?.Refresh();
        }

        public void Unload()
        {
            if (_collectAllServerCommandRegistered)
            {
                ServerCommandRegistry.Unregister(ItemManagementRules.CollectAllCommand, _collectAllServerCommandHandler);
                _collectAllServerCommandRegistered = false;
            }
            if (_clearAllServerCommandRegistered)
            {
                ServerCommandRegistry.Unregister(ItemManagementRules.ClearAllCommand, _clearAllServerCommandHandler);
                _clearAllServerCommandRegistered = false;
            }
            _collectAllServerCommandHandler = null;
            _clearAllServerCommandHandler = null;

            if (_serverResponseSubscribed)
            {
                NetSync.OnServerCommandResponse -= OnServerCommandResponse;
                _serverResponseSubscribed = false;
            }

            try
            {
                _harmony?.UnpatchAll(HarmonyId);
            }
            catch (Exception ex)
            {
                _log?.Warn($"JourneyBuilder: failed to remove Harmony patches: {ex.Message}");
            }

            _harmony = null;
            _itemCheckMethod = null;
            _tileReachMethod = null;
            _itemGrabRangeMethod = null;
            _miningToolMethod = null;
            _hitTileAddDamageMethod = null;
            _pickWallMethod = null;
            _smartToolStrategyMethod = null;
            _smartToolStrategyActive = false;
            _toolDamageMultiplier = 0f;
            _itemManagement = null;

            if (_panel != null)
            {
                _panel.Close();
                _panel.UnregisterDrawCallback();
                _panel = null;
            }

            if (ReferenceEquals(_instance, this))
                _instance = null;
        }
    }
}
