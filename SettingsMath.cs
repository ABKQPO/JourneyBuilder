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
        private const float MinimumDelayFactor = 1f / MaxSpeedMultiplier;

        public static RangeValues MapRange(int range)
        {
            int clamped = Math.Max(1, range);
            return new RangeValues(clamped, Math.Max(0, clamped - 1));
        }

        public static float ApplyBreakSpeed(float pickSpeed, float multiplier)
        {
            return ApplyCappedDelayFactor(pickSpeed, multiplier);
        }

        public static int ApplyBreakDelay(int frames, float multiplier)
        {
            if (frames <= 0)
                return frames;

            float adjusted = ApplyCappedDelayFactor(frames, multiplier);
            return Math.Max(1, (int)Math.Ceiling(adjusted));
        }

        public static int ApplyWallBreakDelay(int frames, float multiplier)
        {
            return ApplyBreakDelay(frames, multiplier);
        }

        public static int ApplyWallPlacementDelay(int frames, float multiplier)
        {
            return ApplyBreakDelay(frames, multiplier);
        }

        public static PlacementSpeedValues ApplyPlacementSpeed(float tileSpeed, float wallSpeed, float multiplier)
        {
            // These values are already the final vanilla delay factors after
            // UpdateEquips has applied accessories, buffs and Journey bonuses.
            // Apply the requested multiplier here, then cap the resulting
            // total speed at 7x so vanilla acceleration cannot be multiplied
            // beyond the configured global limit.
            return new PlacementSpeedValues(
                ApplyCappedDelayFactor(tileSpeed, multiplier),
                ApplyCappedDelayFactor(wallSpeed, multiplier));
        }

        public static float ApplyCappedDelayFactor(float vanillaDelayFactor, float multiplier)
        {
            if (vanillaDelayFactor <= 0f)
                return vanillaDelayFactor;

            float requested = Math.Max(0.1f, multiplier);
            float combined = vanillaDelayFactor / requested;
            return Math.Max(MinimumDelayFactor, combined);
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
