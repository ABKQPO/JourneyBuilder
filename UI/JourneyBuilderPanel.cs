using System;
using System.Globalization;
using Terraria;
using TerrariaModder.Core.Input;
using TerrariaModder.Core.Logging;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;

namespace JourneyBuilder.UI
{
    public sealed class JourneyBuilderPanel
    {
        private readonly JourneyBuilderConfig _config;
        private readonly Action _clampValues;
        private readonly Func<bool> _showClampNotice;
        private readonly Action _configChanged;
        private readonly ILogger _log;
        private readonly ItemManagementService _itemManagement;
        private readonly DraggablePanel _panel;
        private readonly Slider _placementRangeSlider = new Slider();
        private readonly Slider _breakRangeSlider = new Slider();
        private readonly Slider _placementSpeedSlider = new Slider();
        private readonly Slider _breakSpeedSlider = new Slider();
        private readonly Slider _itemPickupRangeSlider = new Slider();
        private readonly TextInput _placementRangeInput = NewInput();
        private readonly TextInput _breakRangeInput = NewInput();
        private readonly TextInput _placementSpeedInput = NewInput();
        private readonly TextInput _breakSpeedInput = NewInput();
        private readonly TextInput _itemPickupRangeInput = NewInput();
        private const int NumericInputWidth = 88;
        private const int NumericUnitWidth = 20;
        private const int ButtonGap = 6;
        private bool _dirty;
        private DateTime _lastChangeUtc;
        private DateTime _clearConfirmationUntilUtc;

        internal JourneyBuilderPanel(JourneyBuilderConfig config, Action clampValues, Func<bool> showClampNotice, Action configChanged, ItemManagementService itemManagement, ILogger log)
        {
            _config = config;
            _clampValues = clampValues;
            _showClampNotice = showClampNotice;
            _configChanged = configChanged;
            _itemManagement = itemManagement;
            _log = log;
            _panel = new DraggablePanel("journey-builder", "JourneyBuilder",
                PanelLayoutMetrics.PanelWidth, PanelLayoutMetrics.PanelHeight)
            {
                CloseOnEscape = true,
                ClipContent = PanelLayoutMetrics.UseContentClipping,
                OnClose = OnPanelClosed
            };
        }

        public bool IsOpen => _panel.IsOpen;

        public void RegisterDrawCallback() => _panel.RegisterDrawCallback(Draw);

        public void UnregisterDrawCallback() => _panel.UnregisterDrawCallback();

        public void Toggle()
        {
            if (_panel.IsOpen)
                _panel.Close();
            else
                _panel.Open();
        }

        public void Close()
        {
            UnfocusInputs();
            _panel.Close();
        }

        public void Refresh() => _clampValues?.Invoke();

        private void Draw()
        {
            _panel.Title = Text("panel.title", "JourneyBuilder");
            if (!_panel.BeginDraw())
                return;

            try
            {
                _clampValues?.Invoke();
                var layout = new StackLayout(_panel.ContentX, _panel.ContentY, _panel.ContentWidth, spacing: 4);

                int leftButtonWidth = (layout.Width - ButtonGap) / 2;
                int rightButtonX = layout.X + leftButtonWidth + ButtonGap;
                int rightButtonWidth = layout.Width - leftButtonWidth - ButtonGap;
                if (layout.ToggleAt(layout.X, leftButtonWidth, Text("panel.enabled", "Enabled"), _config.Enabled, PanelLayoutMetrics.CommandRowHeight))
                {
                    _config.Enabled = !_config.Enabled;
                    MarkChanged();
                }
                if (layout.ButtonAt(rightButtonX, rightButtonWidth, Text("panel.reset", "Reset to Vanilla"), PanelLayoutMetrics.CommandRowHeight))
                    ResetToVanilla();
                layout.Advance(PanelLayoutMetrics.CommandRowHeight);

                layout.SectionHeader(Text("panel.placement", "PLACEMENT"));
                DrawIntegerSetting(ref layout, Text("panel.placementRange", "Placement range"), Text("unit.tiles", "tiles"), _config.PlacementRange, 1, _config.MaxPlacementRange, _placementRangeSlider, _placementRangeInput, value => _config.PlacementRange = value);
                DrawFloatSetting(ref layout, Text("panel.placementSpeed", "Placement speed"), Text("unit.multiplier", "x"), _config.PlacementSpeed, 0.1f, _config.MaxPlacementSpeed, _placementSpeedSlider, _placementSpeedInput, value => _config.PlacementSpeed = value);

                layout.SectionHeader(Text("panel.breaking", "BREAKING"));
                DrawIntegerSetting(ref layout, Text("panel.breakRange", "Break range"), Text("unit.tiles", "tiles"), _config.BreakRange, 1, _config.MaxBreakRange, _breakRangeSlider, _breakRangeInput, value => _config.BreakRange = value);
                DrawFloatSetting(ref layout, Text("panel.breakSpeed", "Break speed"), Text("unit.multiplier", "x"), _config.BreakSpeed, 0.1f, _config.MaxBreakSpeed, _breakSpeedSlider, _breakSpeedInput, value => _config.BreakSpeed = value);

                layout.SectionHeader(Text("panel.items", "ITEM MANAGEMENT"));
                DrawIntegerSetting(ref layout, Text("panel.itemPickupRange", "Item pickup range"), Text("unit.tiles", "tiles"), _config.ItemPickupRange, ItemManagementRules.MinimumPickupTiles, ItemManagementRules.MaximumPickupTiles, _itemPickupRangeSlider, _itemPickupRangeInput, value => _config.ItemPickupRange = value);
                DrawItemManagementCommands(ref layout);

                layout.SectionHeader(Text("panel.serverLimits", "SERVER LIMITS"));
                DrawSmallLabel(ref layout, string.Format(CultureInfo.InvariantCulture, Text("panel.placementLimit", "Placement: {0} tiles / {1:0.0}x"), _config.MaxPlacementRange, _config.MaxPlacementSpeed), UIColors.TextDim);
                DrawSmallLabel(ref layout, string.Format(CultureInfo.InvariantCulture, Text("panel.breakLimit", "Break: {0} tiles / {1:0.0}x"), _config.MaxBreakRange, _config.MaxBreakSpeed), UIColors.TextDim);

                if (_showClampNotice?.Invoke() == true)
                    layout.Label(Text("panel.clamped", "Values were clamped to the server limits."), UIColors.Warning, 22);

                if (_dirty && (DateTime.UtcNow - _lastChangeUtc).TotalMilliseconds >= 500)
                    FlushSave();
            }
            catch (Exception ex)
            {
                _log?.Error("JourneyBuilder panel draw failed.", ex);
            }
            finally
            {
                _panel.EndDraw();
            }
        }

        private void DrawItemManagementCommands(ref StackLayout layout)
        {
            bool canUseCommands = _itemManagement != null && _itemManagement.CanUseWorldCommands;
            if (!canUseCommands)
            {
                DrawSmallLabel(ref layout, Text("panel.hostOnly", "World-item commands are available to the host only."), UIColors.TextDim);
                return;
            }

            int collectWidth = (layout.Width - ButtonGap) / 2;
            int clearX = layout.X + collectWidth + ButtonGap;
            int clearWidth = layout.Width - collectWidth - ButtonGap;
            if (layout.ButtonAt(layout.X, collectWidth, Text("panel.collectAll", "Pick Up All World Items"), PanelLayoutMetrics.CommandRowHeight))
            {
                Player localPlayer = Main.player != null && Main.myPlayer >= 0 && Main.myPlayer < Main.player.Length
                    ? Main.player[Main.myPlayer]
                    : null;
                _itemManagement.CollectAll(localPlayer);
            }

            DateTime now = DateTime.UtcNow;
            bool clearConfirmed = ItemManagementRules.IsClearConfirmed(_clearConfirmationUntilUtc, now);
            if (!clearConfirmed)
                _clearConfirmationUntilUtc = DateTime.MinValue;

            string clearText = clearConfirmed
                ? Text("panel.clearConfirm", "Click again to clear all world items")
                : Text("panel.clearAll", "Clear All World Items");
            if (layout.ButtonAt(clearX, clearWidth, clearText, PanelLayoutMetrics.CommandRowHeight))
            {
                if (clearConfirmed)
                {
                    _itemManagement.ClearAll();
                    _clearConfirmationUntilUtc = DateTime.MinValue;
                }
                else
                {
                    _clearConfirmationUntilUtc = ItemManagementRules.ArmClearConfirmation(now);
                }
            }
            layout.Advance(PanelLayoutMetrics.CommandRowHeight);
        }

        private void DrawIntegerSetting(ref StackLayout layout, string label, string unit, int current, int minimum, int maximum, Slider slider, TextInput input, Action<int> assign)
        {
            int y = layout.Advance(PanelLayoutMetrics.SettingRowHeight);
            UIRenderer.DrawTextSmall(label, layout.X, y + 1, UIColors.Text);
            int inputX = layout.X + layout.Width - NumericInputWidth - NumericUnitWidth;
            int sliderWidth = Math.Max(140, layout.Width - NumericInputWidth - NumericUnitWidth - 8);
            int maximumValue = Math.Max(minimum, maximum);
            int value = slider.Draw(layout.X, y + 20, sliderWidth, 16, current, minimum, maximumValue);
            PrepareInput(input, inputX, y + 18);
            SyncInput(input, value.ToString(CultureInfo.InvariantCulture));
            string inputText = input.Draw(inputX, y + 18, NumericInputWidth, 20);
            UIRenderer.DrawTextSmall(unit, inputX + NumericInputWidth + 3, y + 21, UIColors.TextDim);
            if (input.HasChanged && int.TryParse(inputText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int typedValue))
            {
                typedValue = Math.Max(minimum, Math.Min(maximumValue, typedValue));
                if (typedValue != current)
                {
                    assign(typedValue);
                    MarkChanged();
                }
            }
            ConfirmInputOnEnter(input);
            if (value != current)
            {
                assign(value);
                MarkChanged();
            }
        }

        private void DrawFloatSetting(ref StackLayout layout, string label, string unit, float current, float minimum, float maximum, Slider slider, TextInput input, Action<float> assign)
        {
            int y = layout.Advance(PanelLayoutMetrics.SettingRowHeight);
            UIRenderer.DrawTextSmall(label, layout.X, y + 1, UIColors.Text);
            int inputX = layout.X + layout.Width - NumericInputWidth - NumericUnitWidth;
            int sliderWidth = Math.Max(140, layout.Width - NumericInputWidth - NumericUnitWidth - 8);
            float maximumValue = Math.Max(minimum, maximum);
            float value = slider.Draw(layout.X, y + 20, sliderWidth, 16, current, minimum, maximumValue);
            PrepareInput(input, inputX, y + 18);
            SyncInput(input, value.ToString("0.0", CultureInfo.InvariantCulture));
            string inputText = input.Draw(inputX, y + 18, NumericInputWidth, 20);
            UIRenderer.DrawTextSmall(unit, inputX + NumericInputWidth + 3, y + 21, UIColors.TextDim);
            if (input.HasChanged && float.TryParse(inputText.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float typedValue))
            {
                typedValue = Math.Max(minimum, Math.Min(maximumValue, typedValue));
                if (Math.Abs(typedValue - current) > 0.0001f)
                {
                    assign(typedValue);
                    MarkChanged();
                }
            }
            ConfirmInputOnEnter(input);
            if (Math.Abs(value - current) > 0.0001f)
            {
                assign(value);
                MarkChanged();
            }
        }

        private void MarkChanged()
        {
            _configChanged?.Invoke();
            _dirty = true;
            _lastChangeUtc = DateTime.UtcNow;
        }

        private void ResetToVanilla()
        {
            _config.PlacementRange = 5;
            _config.BreakRange = 5;
            _config.PlacementSpeed = 1f;
            _config.BreakSpeed = 1f;
            _config.ItemPickupRange = 5;
            MarkChanged();
            FlushSave();
        }

        private static void DrawSmallLabel(ref StackLayout layout, string text, Color4 color)
        {
            int y = layout.Advance(PanelLayoutMetrics.SmallLabelHeight);
            UIRenderer.DrawTextSmall(text, layout.X, y + 2, color);
        }

        private static TextInput NewInput()
            => new TextInput("", 16) { KeyBlockId = "journey-builder" };

        private static void SyncInput(TextInput input, string value)
        {
            if (input.IsFocused)
                return;

            _ = input.HasChanged;
            input.Text = value;
            _ = input.HasChanged;
        }

        private static void ConfirmInputOnEnter(TextInput input)
        {
            if (input.IsFocused && (InputState.IsKeyJustPressed(KeyCode.Enter) || InputState.IsKeyJustPressed(KeyCode.NumPadEnter)))
                input.Unfocus();
        }

        private void PrepareInput(TextInput input, int x, int y)
        {
            input.ClearLabel = Text("panel.inputClear", "Clear");
            if (!UIRenderer.MouseLeftClick || !UIRenderer.IsMouseOver(x, y, NumericInputWidth, 20))
                return;

            if (!ReferenceEquals(input, _placementRangeInput)) _placementRangeInput.Unfocus();
            if (!ReferenceEquals(input, _breakRangeInput)) _breakRangeInput.Unfocus();
            if (!ReferenceEquals(input, _placementSpeedInput)) _placementSpeedInput.Unfocus();
            if (!ReferenceEquals(input, _breakSpeedInput)) _breakSpeedInput.Unfocus();
            if (!ReferenceEquals(input, _itemPickupRangeInput)) _itemPickupRangeInput.Unfocus();
        }

        private void OnPanelClosed()
        {
            UnfocusInputs();
            _clearConfirmationUntilUtc = DateTime.MinValue;
            FlushSave();
        }

        private void UnfocusInputs()
        {
            _placementRangeInput.Unfocus();
            _breakRangeInput.Unfocus();
            _placementSpeedInput.Unfocus();
            _breakSpeedInput.Unfocus();
            _itemPickupRangeInput.Unfocus();
        }

        private void FlushSave()
        {
            if (!_dirty)
                return;

            try
            {
                _config.Save();
                _dirty = false;
            }
            catch (Exception ex)
            {
                _log?.Error("JourneyBuilder: failed to save panel settings.", ex);
            }
        }

        private static string Text(string key, string fallback)
            => JourneyBuilderLocalization.Get(key, fallback);
    }
}
