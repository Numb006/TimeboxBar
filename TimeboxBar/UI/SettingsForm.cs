using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using TimeboxBar.Core;
using S = TimeboxBar.Core.Strings;

namespace TimeboxBar.UI
{
    public class SettingsForm : Form
    {
        private readonly AppConfig _config;

        private NumericUpDown _heightSpinner;
        private ComboBox      _positionCombo;
        private TrackBar      _opacitySlider;
        private Label         _opacityLabel;
        private NumericUpDown _quick1Spinner;
        private NumericUpDown _quick2Spinner;
        private TextBox       _hotkeyBox;
        private CheckBox      _soundCheck;
        private ComboBox      _languageCombo;

        private uint _hotkeyModifier;
        private uint _hotkeyKey;

        public SettingsForm(AppConfig config)
        {
            _config         = config;
            _hotkeyModifier = config.HotkeyModifier;
            _hotkeyKey      = config.HotkeyKey;

            Text            = S.SettingsTitle;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            ShowInTaskbar   = false;
            StartPosition   = FormStartPosition.CenterScreen;
            ClientSize      = new Size(340, 340);

            BuildUI();
            LoadValues();
        }

        private void BuildUI()
        {
            int y    = 16;
            int lx   = 16;
            int cx   = 170;
            int step = 42;
            var lblFont  = new Font("Segoe UI", 9f);
            var ctrlFont = new Font("Segoe UI", 9.5f);

            AddLabel(S.LabelBarHeight, lx, y + 3, lblFont);
            _heightSpinner = new NumericUpDown
            {
                Minimum = AppConfig.MinBarHeight, Maximum = AppConfig.MaxBarHeight,
                Location = new Point(cx, y), Width = 60, Font = ctrlFont
            };
            Controls.Add(_heightSpinner);
            y += step;

            AddLabel(S.LabelPosition, lx, y + 3, lblFont);
            _positionCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location      = new Point(cx, y),
                Width         = 120,
                Font          = ctrlFont
            };
            _positionCombo.Items.AddRange(new object[] { S.PositionTop, S.PositionBottom });
            Controls.Add(_positionCombo);
            y += step;

            AddLabel(S.LabelOpacity, lx, y + 3, lblFont);
            _opacitySlider = new TrackBar
            {
                Minimum   = 10,
                Maximum   = 100,
                TickFrequency = 10,
                SmallChange   = 5,
                LargeChange   = 10,
                Location      = new Point(cx, y - 4),
                Width         = 120,
                Height        = 30
            };
            _opacitySlider.ValueChanged += (s, e) =>
                _opacityLabel.Text = $"{_opacitySlider.Value}%";
            Controls.Add(_opacitySlider);

            _opacityLabel = new Label
            {
                Location = new Point(cx + 126, y + 3),
                AutoSize = true,
                Font     = lblFont
            };
            Controls.Add(_opacityLabel);
            y += step;

            AddLabel(S.LabelQuickStart1, lx, y + 3, lblFont);
            _quick1Spinner = new NumericUpDown
            {
                Minimum = 1, Maximum = 240, Location = new Point(cx, y), Width = 60, Font = ctrlFont
            };
            Controls.Add(_quick1Spinner);
            y += step;

            AddLabel(S.LabelQuickStart2, lx, y + 3, lblFont);
            _quick2Spinner = new NumericUpDown
            {
                Minimum = 1, Maximum = 240, Location = new Point(cx, y), Width = 60, Font = ctrlFont
            };
            Controls.Add(_quick2Spinner);
            y += step;

            AddLabel(S.LabelHotkey, lx, y + 3, lblFont);
            _hotkeyBox = new TextBox
            {
                Location  = new Point(cx, y),
                Width     = 140,
                ReadOnly  = true,
                Font      = ctrlFont,
                BackColor = SystemColors.Window
            };
            _hotkeyBox.KeyDown += OnHotkeyKeyDown;
            _hotkeyBox.GotFocus  += (s, e) => _hotkeyBox.Text = S.HotkeyPrompt;
            _hotkeyBox.LostFocus += (s, e) =>
            {
                if (_hotkeyBox.Text == S.HotkeyPrompt)
                    _hotkeyBox.Text = FormatHotkey(_hotkeyModifier, _hotkeyKey);
            };
            Controls.Add(_hotkeyBox);
            y += step;

            _soundCheck = new CheckBox
            {
                Text     = S.LabelSound,
                Location = new Point(lx, y),
                AutoSize = true,
                Font     = ctrlFont
            };
            Controls.Add(_soundCheck);
            y += 36;

            AddLabel(S.LabelLanguage, lx, y + 3, lblFont);
            _languageCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location      = new Point(cx, y),
                Width         = 120,
                Font          = ctrlFont
            };
            _languageCombo.Items.AddRange(new object[] { S.LangAuto, S.LangEnglish, S.LangGerman });
            Controls.Add(_languageCombo);
            y += step;

            // OK / Abbrechen
            var btnOk = new Button
            {
                Text         = "OK",
                DialogResult = DialogResult.OK,
                Location     = new Point(ClientSize.Width - 180, y),
                Width        = 80
            };
            btnOk.Click += (s, e) => SaveValues();

            var btnCancel = new Button
            {
                Text         = S.ButtonCancel,
                DialogResult = DialogResult.Cancel,
                Location     = new Point(ClientSize.Width - 92, y),
                Width        = 84
            };

            AcceptButton = btnOk;
            CancelButton = btnCancel;
            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            // Fensterhöhe anpassen
            ClientSize = new Size(ClientSize.Width, y + 44);
        }

        private void AddLabel(string text, int x, int y, Font font)
        {
            Controls.Add(new Label { Text = text, Location = new Point(x, y), AutoSize = true, Font = font });
        }

        private void LoadValues()
        {
            _heightSpinner.Value   = _config.ClampedBarHeight;
            _positionCombo.Text    = _config.IsBottom ? S.PositionBottom : S.PositionTop;
            _opacitySlider.Value   = (int)Math.Round(_config.Opacity * 100);
            _opacityLabel.Text     = $"{_opacitySlider.Value}%";
            _quick1Spinner.Value   = Math.Max(1, Math.Min(240, _config.QuickStart1));
            _quick2Spinner.Value   = Math.Max(1, Math.Min(240, _config.QuickStart2));
            _hotkeyBox.Text        = FormatHotkey(_hotkeyModifier, _hotkeyKey);
            _soundCheck.Checked    = _config.PlaySound;
            _languageCombo.Text    = _config.Language == "en" ? S.LangEnglish
                                   : _config.Language == "de" ? S.LangGerman
                                   : S.LangAuto;
        }

        private void SaveValues()
        {
            _config.BarHeight      = (int)_heightSpinner.Value;
            _config.Position       = _positionCombo.Text == S.PositionBottom ? "Bottom" : "Top";
            _config.Opacity        = _opacitySlider.Value / 100.0;
            _config.QuickStart1   = (int)_quick1Spinner.Value;
            _config.QuickStart2   = (int)_quick2Spinner.Value;
            _config.HotkeyModifier = _hotkeyModifier;
            _config.HotkeyKey      = _hotkeyKey;
            _config.PlaySound      = _soundCheck.Checked;

            _config.Language = _languageCombo.Text == S.LangEnglish ? "en"
                             : _languageCombo.Text == S.LangGerman  ? "de"
                             : "auto";
        }

        private void OnHotkeyKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled         = true;
            e.SuppressKeyPress = true;

            if (e.KeyCode == Keys.Escape)
            {
                _hotkeyBox.Text = FormatHotkey(_hotkeyModifier, _hotkeyKey);
                return;
            }

            // Reine Modifier-Tasten ignorieren
            if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey ||
                e.KeyCode == Keys.Menu || e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin)
                return;

            uint mod = 0;
            if (e.Control) mod |= 0x0002; // MOD_CONTROL
            if (e.Shift)   mod |= 0x0004; // MOD_SHIFT
            if (e.Alt)     mod |= 0x0001; // MOD_ALT

            _hotkeyModifier = mod;
            _hotkeyKey      = (uint)e.KeyCode;
            _hotkeyBox.Text = FormatHotkey(mod, _hotkeyKey);
        }

        private static string FormatHotkey(uint mod, uint key)
        {
            var parts = new List<string>();
            if ((mod & 0x0002) != 0) parts.Add("Ctrl");
            if ((mod & 0x0004) != 0) parts.Add("Shift");
            if ((mod & 0x0001) != 0) parts.Add("Alt");
            if ((mod & 0x0008) != 0) parts.Add("Win");
            if (key != 0) parts.Add(((Keys)key).ToString());
            return parts.Count > 0 ? string.Join(" + ", parts) : "(kein)";
        }
    }
}
