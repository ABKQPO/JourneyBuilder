namespace JourneyBuilder
{
    /// <summary>
    /// Shared dimensions for the JourneyBuilder panel and its layout regression tests.
    /// Values include the StackLayout spacing added after every control.
    /// </summary>
    public static class PanelLayoutMetrics
    {
        public const int PanelWidth = 460;
        public const int PanelHeight = 520;
        public const int HeaderHeight = 35;
        public const int Padding = 8;
        public const int LayoutSpacing = 4;
        public const int SettingRowHeight = 54;
        public const bool UseContentClipping = false;

        public const int ContentHeight = PanelHeight - HeaderHeight - Padding;

        public static int RequiredContentHeight(bool showClampNotice)
        {
            int height = 0;
            height += 22 + LayoutSpacing; // GENERAL
            height += 26 + LayoutSpacing; // Enabled
            height += 22 + LayoutSpacing; // RANGE
            height += 2 * (SettingRowHeight + LayoutSpacing);
            height += 22 + LayoutSpacing; // SPEED
            height += 2 * (SettingRowHeight + LayoutSpacing);
            height += 22 + LayoutSpacing; // SERVER LIMITS
            height += 2 * (22 + LayoutSpacing);
            if (showClampNotice)
                height += 22 + LayoutSpacing;
            height += 26 + LayoutSpacing; // Reset
            return height;
        }
    }
}
