using JourneyBuilder;

static class Program
{
    private static int _passed;

    private static void Check(string name, bool condition)
    {
        if (!condition) throw new InvalidOperationException($"FAILED: {name}");
        _passed++;
        Console.WriteLine($"PASS: {name}");
    }

    public static int Main()
    {
        var range = SettingsMath.MapRange(5);
        Check("default range maps to vanilla 5x4", range.X == 5 && range.Y == 4);

        Check("minimum range keeps non-negative vertical reach",
            SettingsMath.MapRange(1).X == 1 && SettingsMath.MapRange(1).Y == 0);

        Check("break speed multiplier uses inverse pick speed",
            Math.Abs(SettingsMath.ApplyBreakSpeed(1f, 2f) - 0.5f) < 0.0001f);

        var placement = SettingsMath.ApplyPlacementSpeed(1f, 1.25f, 2f);
        Check("placement speed multiplier applies to tile and wall speed",
            Math.Abs(placement.Tile - 0.5f) < 0.0001f && Math.Abs(placement.Wall - 0.625f) < 0.0001f);

        Check("higher speed multipliers reduce placement delay",
            SettingsMath.ApplyPlacementSpeed(1f, 1f, 2f).Tile < SettingsMath.ApplyPlacementSpeed(1f, 1f, 0.5f).Tile);

        Check("direct mining tool delay uses break multiplier",
            SettingsMath.ApplyBreakDelay(20, 2f) == 10);

        Check("break delay keeps one frame minimum",
            SettingsMath.ApplyBreakDelay(1, 7f) == 1);

        Check("speed multiplier cap is seven",
            Math.Abs(SettingsMath.ClampToServer(20f, SettingsMath.MaxSpeedMultiplier, 0.1f) - 7f) < 0.0001f);

        Check("server cap clamps local value",
            SettingsMath.ClampToServer(150, 100, 1) == 100);

        LocalizationTests.Run(Check);

        Check("panel content has room for every control",
            PanelLayoutMetrics.RequiredContentHeight(true) <= PanelLayoutMetrics.ContentHeight);

        Console.WriteLine($"{_passed} checks passed");
        return 0;
    }
}
