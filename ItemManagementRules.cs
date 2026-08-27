using System;

namespace JourneyBuilder
{
    internal static class ItemManagementRules
    {
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

        internal static bool CanMutateWorldItems(int netMode, bool isHostAndPlay)
        {
            return netMode != 1 || isHostAndPlay;
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
