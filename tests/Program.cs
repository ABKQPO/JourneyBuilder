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

        Check("tool damage scales after the vanilla tool-tier check",
            SettingsMath.ApplyToolDamage(25, 4f) == 100);

        Check("tool damage preserves a zero-damage vanilla result",
            SettingsMath.ApplyToolDamage(0, 7f) == 0);

        Check("break delay keeps one frame minimum",
            SettingsMath.ApplyBreakDelay(1, 7f) == 1);

        Check("speed multiplier cap is seven",
            Math.Abs(SettingsMath.ClampToServer(20f, SettingsMath.MaxSpeedMultiplier, 0.1f) - 7f) < 0.0001f);

        Check("server cap clamps local value",
            SettingsMath.ClampToServer(150, 100, 1) == 100);

        Check("pickup range uses tile pixels", ItemManagementRules.ToPickupPixels(5) == 80);
        Check("pickup range clamps to one tile", ItemManagementRules.ToPickupPixels(0) == 16);
        Check("pickup range clamps to 100 tiles", ItemManagementRules.ToPickupPixels(101) == 1600);

        DateTime now = new DateTime(2026, 8, 27, 12, 0, 0, DateTimeKind.Utc);
        DateTime armedUntil = ItemManagementRules.ArmClearConfirmation(now);
        Check("first clear click arms for three seconds", armedUntil == now.AddSeconds(3));
        Check("second click inside window confirms", ItemManagementRules.IsClearConfirmed(armedUntil, now.AddSeconds(2)));
        Check("expired clear window does not confirm", !ItemManagementRules.IsClearConfirmed(armedUntil, now.AddSeconds(3)));
        Check("empty clear window does not confirm", !ItemManagementRules.IsClearConfirmed(DateTime.MinValue, now));
        Check("single player owns world items", ItemManagementRules.CanMutateWorldItems(0, false));
        Check("host and play owns world items", ItemManagementRules.CanMutateWorldItems(1, true));
        Check("remote client cannot mutate world items", !ItemManagementRules.CanMutateWorldItems(1, false));

        LocalizationTests.Run(Check);

        Check("Terraria Chinese culture resolves simplified Chinese", LocalizationCultureRules.PrimaryResource("Chinese") == "zh-Hans");
        Check("Terraria Japanese culture resolves Japanese", LocalizationCultureRules.PrimaryResource("Japanese") == "ja");
        Check("traditional Chinese culture resolves traditional Chinese", LocalizationCultureRules.PrimaryResource("zh-TW") == "zh-Hant");

        Check("panel uses compact width", PanelLayoutMetrics.PanelWidth == 500);
        Check("panel uses compact height", PanelLayoutMetrics.PanelHeight == 520);
        Check("panel uses compact setting rows", PanelLayoutMetrics.SettingRowHeight == 40);
        Check("panel fits compact base sections",
            PanelLayoutMetrics.RequiredContentHeight(false, false, false) <= PanelLayoutMetrics.ContentHeight);
        Check("panel fits compact full command state",
            PanelLayoutMetrics.RequiredContentHeight(true, true, true) <= PanelLayoutMetrics.ContentHeight);
        Check("panel disables Core clipping under UI scale",
            !PanelLayoutMetrics.UseContentClipping);

        Console.WriteLine($"{_passed} checks passed");
        return 0;
    }
}
