using System;
using System.Reflection;
using Terraria;
using TerrariaModder.Core.Logging;
using TerrariaModder.Core.Net;
using TerrariaModder.Core.Permissions;
using TerrariaModder.Core.Server;

namespace JourneyBuilder
{
    internal sealed class ItemManagementService
    {
        private readonly ILogger _log;
        private readonly MethodInfo _clientPickupItemMethod;

        internal ItemManagementService(ILogger log)
        {
            _log = log;
            _clientPickupItemMethod = typeof(Player).GetMethod("PickupItem", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(WorldItem) }, null);
            if (_clientPickupItemMethod == null)
                _log?.Warn("JourneyBuilder: Player.PickupItem(WorldItem) was not found; local collect-all is unavailable.");
        }

        internal bool CanUseWorldCommands
        {
            get
            {
                try { return !ItemManagementRules.ShouldRequestServerCommand(Main.netMode) || NetSync.LocalPlayerIsAdmin; }
                catch { return false; }
            }
        }

        internal bool CanCollectAll => CanUseWorldCommands &&
            (_clientPickupItemMethod != null || ItemManagementRules.ShouldRequestServerCommand(Main.netMode));

        internal void CollectAll(Player player)
        {
            if (!CanUseWorldCommands) return;
            if (ItemManagementRules.ShouldRequestServerCommand(Main.netMode))
            {
                NetSync.SendServerCommandRequest(ItemManagementRules.CollectAllCommand, string.Empty);
                return;
            }
            CollectAllLocal(player);
        }

        internal void ClearAll()
        {
            if (!CanUseWorldCommands) return;
            if (ItemManagementRules.ShouldRequestServerCommand(Main.netMode))
            {
                NetSync.SendServerCommandRequest(ItemManagementRules.ClearAllCommand, string.Empty);
                return;
            }
            ClearAllLocal();
        }

        internal void HandleServerCommand(int callerSlot, string command)
        {
            if (!ItemManagementRules.IsWorldItemCommand(command)) return;
            if (!PermissionService.IsAdmin(callerSlot))
            {
                NetSync.SendServerCommandResponseTo(callerSlot, command, "denied");
                return;
            }

            try
            {
                int affected = string.Equals(command, ItemManagementRules.CollectAllCommand, StringComparison.Ordinal)
                    ? CollectAllServer(callerSlot) : ClearAllServer();
                string result = string.Equals(command, ItemManagementRules.CollectAllCommand, StringComparison.Ordinal)
                    ? "collected:" + affected : "cleared:" + affected;
                _log?.Info($"JourneyBuilder: {command} by Admin slot {callerSlot}; affected {affected} world items.");
                NetSync.SendServerCommandResponseTo(callerSlot, command, result);
            }
            catch (Exception ex)
            {
                _log?.Error($"JourneyBuilder: server world-item command '{command}' failed.", ex);
                NetSync.SendServerCommandResponseTo(callerSlot, command, "failed");
            }
        }

        internal void HandleServerCommandResponse(string command, string result)
        {
            if (!ItemManagementRules.IsWorldItemCommand(command)) return;
            string message;
            if (string.Equals(result, "denied", StringComparison.Ordinal))
                message = JourneyBuilderLocalization.Get("panel.adminOnly", "World-item commands require server Admin permission.");
            else if (result != null && result.StartsWith("collected:", StringComparison.Ordinal))
                message = string.Format(JourneyBuilderLocalization.Get("panel.collectResult", "Picked up {0} world items."), result.Substring("collected:".Length));
            else if (result != null && result.StartsWith("cleared:", StringComparison.Ordinal))
                message = string.Format(JourneyBuilderLocalization.Get("panel.clearResult", "Cleared {0} world items."), result.Substring("cleared:".Length));
            else
                message = JourneyBuilderLocalization.Get("panel.commandFailed", "World-item command failed on the server.");

            try { Main.NewText("[JourneyBuilder] " + message, 255, 200, 80); } catch { }
        }

        private void CollectAllLocal(Player player)
        {
            if (_clientPickupItemMethod == null || player == null || Main.item == null) return;
            foreach (WorldItem worldItem in Main.item)
            {
                if (worldItem == null || !worldItem.active) continue;
                try { _clientPickupItemMethod.Invoke(player, new object[] { worldItem }); }
                catch (Exception ex) { _log?.Warn("JourneyBuilder: failed to collect a local world item: " + ex.Message); }
            }
        }

        private void ClearAllLocal()
        {
            if (Main.item == null) return;
            foreach (WorldItem worldItem in Main.item)
            {
                if (worldItem == null || !worldItem.active) continue;
                try { worldItem.TurnToAir(); worldItem.SyncItem(); }
                catch (Exception ex) { _log?.Warn("JourneyBuilder: failed to clear a local world item: " + ex.Message); }
            }
        }

        private static int CollectAllServer(int callerSlot)
        {
            object player = DedServProxy.GetPlayer(callerSlot);
            Array worldItems = GetServerWorldItems();
            if (player == null || worldItems == null) return 0;

            int collected = 0;
            MethodInfo pickupItem = null;
            foreach (object worldItem in worldItems)
            {
                if (!IsActive(worldItem)) continue;
                pickupItem = pickupItem ?? FindPickupItemMethod(player.GetType(), worldItem.GetType());
                if (pickupItem == null) break;
                try
                {
                    pickupItem.Invoke(player, new[] { worldItem });
                    if (!IsActive(worldItem)) collected++;
                }
                catch { }
            }
            return collected;
        }

        private static int ClearAllServer()
        {
            Array worldItems = GetServerWorldItems();
            if (worldItems == null) return 0;

            int cleared = 0;
            foreach (object worldItem in worldItems)
            {
                if (!IsActive(worldItem)) continue;
                try
                {
                    InvokeNoArgument(worldItem, "TurnToAir");
                    InvokeNoArgument(worldItem, "SyncItem");
                    if (!IsActive(worldItem)) cleared++;
                }
                catch { }
            }
            return cleared;
        }

        private static Array GetServerWorldItems()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string name = assembly.GetName().Name;
                if (name != "TerrariaServer" && name != "Terraria") continue;
                Type mainType = assembly.GetType("Terraria.Main");
                FieldInfo itemField = mainType?.GetField("item", BindingFlags.Public | BindingFlags.Static);
                if (itemField == null) continue;
                try
                {
                    Array items = itemField.GetValue(null) as Array;
                    if (items != null) return items;
                }
                catch { }
            }
            return null;
        }

        private static MethodInfo FindPickupItemMethod(Type playerType, Type worldItemType)
        {
            foreach (MethodInfo method in playerType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (method.Name != "PickupItem") continue;
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 1 && parameters[0].ParameterType.IsAssignableFrom(worldItemType)) return method;
            }
            return null;
        }

        private static bool IsActive(object worldItem)
        {
            try
            {
                FieldInfo active = worldItem?.GetType().GetField("active", BindingFlags.Public | BindingFlags.Instance);
                return active != null && (bool)active.GetValue(worldItem);
            }
            catch { return false; }
        }

        private static void InvokeNoArgument(object target, string methodName)
        {
            MethodInfo method = target?.GetType().GetMethod(methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
            method?.Invoke(target, null);
        }
    }
}
