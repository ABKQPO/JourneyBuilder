using System;
using System.Reflection;
using Terraria;
using TerrariaModder.Core.Logging;

namespace JourneyBuilder
{
    internal sealed class ItemManagementService
    {
        private readonly ILogger _log;
        private readonly MethodInfo _pickupItemMethod;

        internal ItemManagementService(ILogger log)
        {
            _log = log;
            _pickupItemMethod = typeof(Player).GetMethod(
                "PickupItem",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(WorldItem) },
                null);

            if (_pickupItemMethod == null)
                _log?.Warn("JourneyBuilder: Player.PickupItem(WorldItem) was not found; collect-all is unavailable.");
        }

        internal bool CanUseWorldCommands
        {
            get
            {
                try
                {
                    return ItemManagementRules.CanMutateWorldItems(Main.netMode, Netplay.IsHostAndPlay);
                }
                catch
                {
                    return false;
                }
            }
        }

        internal bool CanCollectAll => CanUseWorldCommands && _pickupItemMethod != null;

        internal void CollectAll(Player player)
        {
            if (!CanCollectAll || player == null || Main.item == null)
                return;

            foreach (WorldItem worldItem in Main.item)
            {
                if (worldItem == null || !worldItem.active)
                    continue;

                try
                {
                    _pickupItemMethod.Invoke(player, new object[] { worldItem });
                }
                catch (Exception ex)
                {
                    _log?.Warn("JourneyBuilder: failed to collect a world item: " + ex.Message);
                }
            }
        }

        internal void ClearAll()
        {
            if (!CanUseWorldCommands || Main.item == null)
                return;

            foreach (WorldItem worldItem in Main.item)
            {
                if (worldItem == null || !worldItem.active)
                    continue;

                try
                {
                    worldItem.TurnToAir();
                    worldItem.SyncItem();
                }
                catch (Exception ex)
                {
                    _log?.Warn("JourneyBuilder: failed to clear a world item: " + ex.Message);
                }
            }
        }
    }
}
