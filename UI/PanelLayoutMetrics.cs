namespace JourneyBuilder
{
    /// <summary>
    /// Shared dimensions for the JourneyBuilder panel and its layout regression tests.
    /// Values include the StackLayout spacing added after every control.
    /// </summary>
    public static class PanelLayoutMetrics
    {
        public const int PanelWidth = 520;
        public const int PanelHeight = 680;
        public const int HeaderHeight = 35;
        public const int Padding = 8;
        public const int LayoutSpacing = 4;
        public const int SettingRowHeight = 54;
        public const int CommandRowHeight = 26;
        public const bool UseContentClipping = false;

        public const int ContentHeight = PanelHeight - HeaderHeight - Padding;

        public static int RequiredContentHeight(bool showClampNotice, bool showHostOnlyNotice, bool showClearConfirmation)
        {
            int height = 0;
            height += 26 + LayoutSpacing; // Enabled
            height += 22 + LayoutSpacing; // Placement
            height += 2 * (SettingRowHeight + LayoutSpacing);
            height += 22 + LayoutSpacing; // Breaking
            height += 2 * (SettingRowHeight + LayoutSpacing);
            height += 22 + LayoutSpacing; // Item management
            height += SettingRowHeight + LayoutSpacing;
            height += 2 * (CommandRowHeight + LayoutSpacing);
            height += 22 + LayoutSpacing; // SERVER LIMITS
            height += 2 * (22 + LayoutSpacing);
            if (showClampNotice)
                height += 22 + LayoutSpacing;
            if (showHostOnlyNotice)
                height += 22 + LayoutSpacing;
            if (showClearConfirmation)
                height += 0; // Confirmation replaces the normal clear-command row.
            height += CommandRowHeight + LayoutSpacing; // Reset
            return height;
        }
    }
}
