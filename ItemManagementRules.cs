using System;

namespace JourneyBuilder
{
    internal static class ItemManagementRules
    {
        internal const string CollectAllCommand = "journeybuilder_collect_world_items";
        internal const string ClearAllCommand = "journeybuilder_clear_world_items";
        internal const int MinimumPickupTiles = 1;
        internal const int MaximumPickupTiles = 100;
        internal const int PixelsPerTile = 16;

        internal static int ClampPickupTiles(int tiles)
        {
            return Math.Max(MinimumPickupTiles, Math.Min(MaximumPickupTiles, tiles));
        }

        internal static int ToPickupPixels(int tiles)
        {
            return ClampPickupTiles(tiles) * PixelsPerTile;
        }

        internal static bool ShouldRequestServerCommand(int netMode)
        {
            return netMode == 1;
        }

        internal static bool IsWorldItemCommand(string command)
        {
            return string.Equals(command, CollectAllCommand, StringComparison.Ordinal) ||
                string.Equals(command, ClearAllCommand, StringComparison.Ordinal);
        }

        internal static DateTime ArmClearConfirmation(DateTime nowUtc)
        {
            return nowUtc.AddSeconds(3);
        }

        internal static bool IsClearConfirmed(DateTime armedUntilUtc, DateTime nowUtc)
        {
            return armedUntilUtc != DateTime.MinValue && nowUtc < armedUntilUtc;
        }
    }
}
