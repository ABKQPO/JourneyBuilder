using System;
using System.Globalization;
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
        private readonly DraggablePanel _panel;
        private readonly Slider _placementRangeSlider = new Slider();
        private readonly Slider _breakRangeSlider = new Slider();
        private readonly Slider _placementSpeedSlider = new Slider();
        private readonly Slider _breakSpeedSlider = new Slider();
        private readonly TextInput _placementRangeInput = NewInput();
        private readonly TextInput _breakRangeInput = NewInput();
        private readonly TextInput _placementSpeedInput = NewInput();
        private readonly TextInput _breakSpeedInput = NewInput();
        private const int NumericInputWidth = 112;
        private const int NumericUnitWidth = 24;
        private bool _dirty;
        private DateTime _lastChangeUtc;

        public JourneyBuilderPanel(JourneyBuilderConfig config, Action clampValues, Func<bool> showClampNotice, Action configChanged, ILogger log)
        {
            _config = config;
            _clampValues = clampValues;
            _showClampNotice = showClampNotice;
            _configChanged = configChanged;
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

                layout.SectionHeader(Text("panel.general", "GENERAL"));
                if (layout.Toggle(Text("panel.enabled", "Enabled"), _config.Enabled))
                {
                    _config.Enabled = !_config.Enabled;
                    MarkChanged();
                }

                layout.SectionHeader(Text("panel.range", "RANGE"));
                DrawIntegerSetting(ref layout, Text("panel.placementRange", "Placement range"), Text("unit.tiles", "tiles"), _config.PlacementRange, 1, _config.MaxPlacementRange, _placementRangeSlider, _placementRangeInput, value => _config.PlacementRange = value);
                DrawIntegerSetting(ref layout, Text("panel.breakRange", "Break range"), Text("unit.tiles", "tiles"), _config.BreakRange, 1, _config.MaxBreakRange, _breakRangeSlider, _breakRangeInput, value => _config.BreakRange = value);

                layout.SectionHeader(Text("panel.speed", "SPEED"));
                DrawFloatSetting(ref layout, Text("panel.placementSpeed", "Placement speed"), Text("unit.multiplier", "x"), _config.PlacementSpeed, 0.1f, _config.MaxPlacementSpeed, _placementSpeedSlider, _placementSpeedInput, value => _config.PlacementSpeed = value);
                DrawFloatSetting(ref layout, Text("panel.breakSpeed", "Break speed"), Text("unit.multiplier", "x"), _config.BreakSpeed, 0.1f, _config.MaxBreakSpeed, _breakSpeedSlider, _breakSpeedInput, value => _config.BreakSpeed = value);

                layout.SectionHeader(Text("panel.serverLimits", "SERVER LIMITS"));
                layout.Label(string.Format(CultureInfo.InvariantCulture, Text("panel.placementLimit", "Placement: {0} tiles / {1:0.0}x"), _config.MaxPlacementRange, _config.MaxPlacementSpeed), UIColors.TextDim, 22);
                layout.Label(string.Format(CultureInfo.InvariantCulture, Text("panel.breakLimit", "Break: {0} tiles / {1:0.0}x"), _config.MaxBreakRange, _config.MaxBreakSpeed), UIColors.TextDim, 22);

                if (_showClampNotice?.Invoke() == true)
                    layout.Label(Text("panel.clamped", "Values were clamped to the server limits."), UIColors.Warning, 22);

                if (layout.Button(Text("panel.reset", "Reset to Vanilla")))
                {
                    _config.PlacementRange = 5;
                    _config.BreakRange = 5;
                    _config.PlacementSpeed = 1f;
                    _config.BreakSpeed = 1f;
                    MarkChanged();
                    FlushSave();
                }

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

        private void DrawIntegerSetting(ref StackLayout layout, string label, string unit, int current, int minimum, int maximum, Slider slider, TextInput input, Action<int> assign)
        {
            int y = layout.Advance(PanelLayoutMetrics.SettingRowHeight);
            UIRenderer.DrawText(label, layout.X, y, UIColors.Text);
            int inputX = layout.X + layout.Width - NumericInputWidth - NumericUnitWidth;
            int sliderWidth = Math.Max(140, layout.Width - NumericInputWidth - NumericUnitWidth - 8);
            int maximumValue = Math.Max(minimum, maximum);
            int value = slider.Draw(layout.X, y + 24, sliderWidth, 20, current, minimum, maximumValue);
            PrepareInput(input, inputX, y + 22);
            SyncInput(input, value.ToString(CultureInfo.InvariantCulture));
            string inputText = input.Draw(inputX, y + 22, NumericInputWidth, 24);
            UIRenderer.DrawText(unit, inputX + NumericInputWidth + 4, y + 26, UIColors.TextDim);
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
            UIRenderer.DrawText(label, layout.X, y, UIColors.Text);
            int inputX = layout.X + layout.Width - NumericInputWidth - NumericUnitWidth;
            int sliderWidth = Math.Max(140, layout.Width - NumericInputWidth - NumericUnitWidth - 8);
            float maximumValue = Math.Max(minimum, maximum);
            float value = slider.Draw(layout.X, y + 24, sliderWidth, 20, current, minimum, maximumValue);
            PrepareInput(input, inputX, y + 22);
            SyncInput(input, value.ToString("0.0", CultureInfo.InvariantCulture));
            string inputText = input.Draw(inputX, y + 22, NumericInputWidth, 24);
            UIRenderer.DrawText(unit, inputX + NumericInputWidth + 4, y + 26, UIColors.TextDim);
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
            if (!UIRenderer.MouseLeftClick || !UIRenderer.IsMouseOver(x, y, NumericInputWidth, 24))
                return;

            if (!ReferenceEquals(input, _placementRangeInput)) _placementRangeInput.Unfocus();
            if (!ReferenceEquals(input, _breakRangeInput)) _breakRangeInput.Unfocus();
            if (!ReferenceEquals(input, _placementSpeedInput)) _placementSpeedInput.Unfocus();
            if (!ReferenceEquals(input, _breakSpeedInput)) _breakSpeedInput.Unfocus();
        }

        private void OnPanelClosed()
        {
            UnfocusInputs();
            FlushSave();
        }

        private void UnfocusInputs()
        {
            _placementRangeInput.Unfocus();
            _breakRangeInput.Unfocus();
            _placementSpeedInput.Unfocus();
            _breakSpeedInput.Unfocus();
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
