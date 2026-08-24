using System;

namespace JourneyBuilder
{
    public readonly struct RangeValues
    {
        public RangeValues(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }
    }

    public readonly struct PlacementSpeedValues
    {
        public PlacementSpeedValues(float tile, float wall)
        {
            Tile = tile;
            Wall = wall;
        }

        public float Tile { get; }
        public float Wall { get; }
    }

    public static class SettingsMath
    {
        public const float MaxSpeedMultiplier = 7f;

        public static RangeValues MapRange(int range)
        {
            int clamped = Math.Max(1, range);
            return new RangeValues(clamped, Math.Max(0, clamped - 1));
        }

        public static float ApplyBreakSpeed(float pickSpeed, float multiplier)
        {
            return pickSpeed / Math.Max(0.1f, multiplier);
        }

        public static int ApplyBreakDelay(int frames, float multiplier)
        {
            if (frames <= 0)
                return frames;

            int adjusted = (int)(frames / Math.Max(0.1f, multiplier));
            return Math.Max(1, adjusted);
        }

        public static PlacementSpeedValues ApplyPlacementSpeed(float tileSpeed, float wallSpeed, float multiplier)
        {
            float factor = Math.Max(0.1f, multiplier);
            // Terraria multiplies these values into useTime, so a larger
            // multiplier must reduce the delay rather than increase it.
            return new PlacementSpeedValues(tileSpeed / factor, wallSpeed / factor);
        }

        public static int ClampToServer(int value, int serverMax, int minimum)
        {
            return Math.Max(minimum, Math.Min(value, serverMax));
        }

        public static float ClampToServer(float value, float serverMax, float minimum)
        {
            return Math.Max(minimum, Math.Min(value, serverMax));
        }
    }
}
