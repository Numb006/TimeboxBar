using System;
using System.Drawing;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using TimeboxBar.Core;
using S = TimeboxBar.Core.Strings;

namespace TimeboxBar.UI
{
    public class BarForm : Form
    {
        // ── P/Invoke ─────────────────────────────────────────────────────────────

        [DllImport("user32.dll")]
        static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
        [DllImport("user32.dll")]
        static extern bool DestroyIcon(IntPtr handle);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        struct MINMAXINFO
        {
            public Point ptReserved, ptMaxSize, ptMaxPosition, ptMinTrackSize, ptMaxTrackSize;
        }

        // ── Felder ───────────────────────────────────────────────────────────────

        private readonly AppConfig     _config;
        private readonly TimeboxTimer  _timer;
        private readonly HotkeyManager _hotkey;

        private NotifyIcon _trayIcon;
        private Timer      _uiTimer;
        private bool       _realExit;

        private bool _blinking;
        private int  _blinkCount;

        private ToolStripMenuItem _itemQuick1;
        private ToolStripMenuItem _itemQuick2;
        private ToolStripMenuItem _itemCustomTime;
        private ToolStripMenuItem _itemPause;
        private ToolStripMenuItem _itemStop;
        private ToolStripMenuItem _itemSettings;
        private ToolStripMenuItem _itemExit;

        private Icon _cachedIcon;

        // ── Konstruktor ──────────────────────────────────────────────────────────

        public BarForm(AppConfig config, TimeboxTimer timer, HotkeyManager hotkey)
        {
            Logger.Log("BarForm ctor start");
            _config = config;
            _timer  = timer;
            _hotkey = hotkey;

            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint  |
                     ControlStyles.UserPaint, true);

            FormBorderStyle = FormBorderStyle.None;
            TopMost         = true;
            ShowInTaskbar   = false;

            _timer.Completed    += OnTimerCompleted;
            _timer.StateChanged += OnTimerStateChanged;

            Logger.Log("Building tray icon...");
            try { BuildTrayIcon(); Logger.Log("Tray icon OK"); }
            catch (Exception ex) { Logger.LogEx("BuildTrayIcon", ex); throw; }

            _uiTimer = new Timer { Interval = 200 };
            _uiTimer.Tick += (s, e) =>
            {
                try { UpdateTrayText(); Invalidate(); }
                catch (Exception ex) { Logger.LogEx("uiTimer.Tick", ex); }
            };
            _uiTimer.Start();

            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
            Logger.Log("BarForm ctor done");
        }

        // ── Window-Style: Click-Through via WS_EX_LAYERED + WS_EX_TRANSPARENT ──

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x00080000; // WS_EX_LAYERED    — Alpha + ColorKey
                cp.ExStyle |= 0x00000020; // WS_EX_TRANSPARENT — Click-Through
                return cp;
            }
        }

        // Application.Run(form) ruft Show() auf — wir unterdrücken das bis ein Timer läuft.
        protected override void SetVisibleCore(bool value)
        {
            Logger.Log($"SetVisibleCore({value}), IsHandleCreated={IsHandleCreated}");
            if (!IsHandleCreated)
            {
                CreateHandle();
                base.SetVisibleCore(false);
                return;
            }
            base.SetVisibleCore(value);
        }

        // ── Handle-Lebenszyklus ──────────────────────────────────────────────────

        protected override void OnHandleCreated(EventArgs e)
        {
            Logger.Log($"OnHandleCreated, Handle={Handle}");
            base.OnHandleCreated(e);

            try
            {
                RepositionBar();
                Logger.Log($"RepositionBar done: Bounds={Bounds}");
            }
            catch (Exception ex) { Logger.LogEx("RepositionBar", ex); }

            try
            {
                ApplyOpacity();
                Logger.Log("ApplyOpacity done");
            }
            catch (Exception ex) { Logger.LogEx("ApplyOpacity", ex); }

            bool hotkeyOk = _hotkey.Register(Handle, _config.HotkeyModifier, _config.HotkeyKey);
            Logger.Log($"RegisterHotKey: modifier=0x{_config.HotkeyModifier:X}, key=0x{_config.HotkeyKey:X}, success={hotkeyOk}");

            if (!hotkeyOk)
                _trayIcon.ShowBalloonTip(4000, "TimeboxBar", S.HotkeyConflict, ToolTipIcon.Warning);

            if (_config.FirstRun)
            {
                _config.FirstRun = false;
                _config.Save();
                BeginInvoke(new System.Action(ShowQuickStartPopup));
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Logger.Log("OnFormClosed");
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            _hotkey.UnregisterAll();
            _uiTimer.Stop();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            DisposeIcon(ref _cachedIcon);
            base.OnFormClosed(e);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Logger.Log($"OnFormClosing, _realExit={_realExit}, TimerState={_timer.State}");
            if (!_realExit) { e.Cancel = true; return; }

            if (_timer.State != TimerState.Idle)
            {
                var rem    = _timer.Remaining;
                var result = MessageBox.Show(
                    S.ExitConfirm($"{rem:mm\\:ss}"),
                    "TimeboxBar", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (result == DialogResult.No) { e.Cancel = true; _realExit = false; return; }
            }

            base.OnFormClosing(e);
        }

        // ── WndProc ──────────────────────────────────────────────────────────────

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY        = 0x0312;
            const int WM_GETMINMAXINFO = 0x0024;

            if (m.Msg == WM_HOTKEY)
            {
                if (_timer.State == TimerState.Idle)
                {
                    Logger.Log("WM_HOTKEY received → idle, showing QuickStartPopup");
                    ShowQuickStartPopup();
                }
                else
                {
                    Logger.Log("WM_HOTKEY received → TogglePause");
                    _timer.TogglePause();
                }
            }
            else if (m.Msg == Program.WM_SHOW_INSTANCE)
            {
                Logger.Log("WM_SHOW_INSTANCE received");
                _trayIcon.ShowBalloonTip(1500, "TimeboxBar", S.AlreadyRunning, ToolTipIcon.Info);
            }
            else if (m.Msg == WM_GETMINMAXINFO)
            {
                // Windows erzwingt eine Mindesthöhe (~39px) — wir erlauben 1px Minimum
                var info = (MINMAXINFO)System.Runtime.InteropServices.Marshal.PtrToStructure(
                    m.LParam, typeof(MINMAXINFO));
                info.ptMinTrackSize = new Point(1, 1);
                System.Runtime.InteropServices.Marshal.StructureToPtr(info, m.LParam, true);
                m.Result = IntPtr.Zero;
                return;
            }

            base.WndProc(ref m);
        }

        // ── Positionierung ───────────────────────────────────────────────────────

        private void RepositionBar()
        {
            if (!IsHandleCreated) return;
            var screen  = Screen.FromHandle(Handle);
            var area    = screen.WorkingArea;
            int desiredH = _config.ClampedBarHeight;

            Logger.Log($"RepositionBar: screen={screen.Bounds}, workArea={area}, desiredH={desiredH}");

            if (_config.IsBottom)
                Bounds = new Rectangle(area.Left, area.Bottom - desiredH, area.Width, desiredH);
            else
                Bounds = new Rectangle(area.Left, area.Top, area.Width, desiredH);

            int actualH = Height;
            Logger.Log($"RepositionBar done: desiredH={desiredH}, actualH={actualH}, Bounds={Bounds}");

            if (actualH > desiredH && _config.IsBottom)
            {
                Top = area.Bottom - actualH;
                Logger.Log($"RepositionBar Bottom-adjust: Top={Top}");
            }
        }

        private void ApplyOpacity()
        {
            if (!IsHandleCreated) return;
            byte alpha = _config.OpacityAlpha;
            // LWA_COLORKEY (0x01): Schwarz = visuell durchsichtig
            // LWA_ALPHA   (0x02): restliche Pixel mit konfigurierbarer Transparenz
            bool ok = SetLayeredWindowAttributes(Handle, 0x00000000, alpha, 0x01 | 0x02);
            Logger.Log($"SetLayeredWindowAttributes: alpha={alpha}, ok={ok}");
        }

        public void ApplyConfig()
        {
            Logger.Log("ApplyConfig called");
            RepositionBar();
            ApplyOpacity();
            UpdateTrayMenu();
            _hotkey.UnregisterAll();
            bool ok = _hotkey.Register(Handle, _config.HotkeyModifier, _config.HotkeyKey);
            Logger.Log($"ApplyConfig: RegisterHotKey ok={ok}");
            if (!ok)
                _trayIcon.ShowBalloonTip(3000, "TimeboxBar", S.HotkeyConflictShort, ToolTipIcon.Warning);
        }

        private void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            Logger.Log("DisplaySettingsChanged → RepositionBar");
            RepositionBar();
        }

        // ── Timer-Events ─────────────────────────────────────────────────────────

        private void OnTimerCompleted()
        {
            Logger.Log("OnTimerCompleted");
            if (_config.PlaySound)
                try { SystemSounds.Exclamation.Play(); } catch { }
            _trayIcon.ShowBalloonTip(5000, "TimeboxBar", S.TimerCompleted, ToolTipIcon.Info);
            _blinking   = true;
            _blinkCount = 0;
            _uiTimer.Interval = 100;
        }

        private void OnTimerStateChanged(TimerState state)
        {
            Logger.Log($"OnTimerStateChanged: {state}, Visible wird={state != TimerState.Idle}");
            bool showing = state != TimerState.Idle;
            Visible = showing;
            if (showing)
                RepositionBar(); // Höhe korrekt setzen sobald Fenster sichtbar ist
            if (state == TimerState.Idle)
            {
                _blinking = false;
                _uiTimer.Interval = 200;
            }
            UpdateTrayMenu();
        }

        // ── Rendering ─────────────────────────────────────────────────────────────

        protected override void OnPaint(PaintEventArgs e)
        {
            try { OnPaintCore(e); } catch (Exception ex) { Logger.LogEx("OnPaint", ex); }
        }

        private void OnPaintCore(PaintEventArgs e)
        {
            var g = e.Graphics;
            // Schwarz = transparent via LWA_COLORKEY; Windows erzwingt ggf. mehr Höhe als
            // gewünscht — deshalb explizit nur _config.BarHeight px einfärben, Rest bleibt
            // schwarz (= unsichtbar durch ColorKey)
            g.Clear(Color.Black);

            if (_timer.State == TimerState.Idle) return;

            double progress   = _timer.Progress;
            int    fillWidth  = (int)(ClientSize.Width * progress);
            int    barH       = Math.Min(_config.ClampedBarHeight, ClientSize.Height);
            int    barY       = _config.IsBottom ? ClientSize.Height - barH : 0;

            if (fillWidth <= 0) return;

            Color barColor;
            if (_blinking)
            {
                _blinkCount++;
                if (_blinkCount >= 30) { _blinking = false; _uiTimer.Interval = 200; }
                barColor = (_blinkCount % 2 == 0) ? Color.OrangeRed : Color.Black;
            }
            else if (_timer.State == TimerState.Paused)
                barColor = Color.DimGray;
            else
                barColor = ProgressColor(progress);

            using (var b = new SolidBrush(barColor))
                g.FillRectangle(b, 0, barY, fillWidth, barH);
        }

        private static Color ProgressColor(double p)
        {
            if (p > 0.5)  return Color.MediumSeaGreen;
            if (p > 0.25) return Color.Gold;
            return Color.OrangeRed;
        }

        // ── Tray ──────────────────────────────────────────────────────────────────

        private void BuildTrayIcon()
        {
            _cachedIcon = GenerateIcon(Color.DimGray);
            _trayIcon = new NotifyIcon
            {
                Icon    = _cachedIcon,
                Text    = "TimeboxBar",
                Visible = true
            };

            var menu = new ContextMenuStrip();

            _itemQuick1 = new ToolStripMenuItem();
            _itemQuick1.Click += (s, e) => StartTimer(_config.QuickStart1);

            _itemQuick2 = new ToolStripMenuItem();
            _itemQuick2.Click += (s, e) => StartTimer(_config.QuickStart2);

            _itemCustomTime = new ToolStripMenuItem(S.MenuCustomTime);
            _itemCustomTime.Click += (s, e) => ShowCustomTimeDialog();

            _itemPause = new ToolStripMenuItem(S.MenuPause) { Enabled = false };
            _itemPause.Click += (s, e) => _timer.TogglePause();

            _itemStop = new ToolStripMenuItem(S.MenuStop) { Enabled = false };
            _itemStop.Click += (s, e) =>
            {
                Logger.Log("Stopp clicked");
                _timer.Stop();
            };

            _itemSettings = new ToolStripMenuItem(S.MenuSettings);
            _itemSettings.Click += (s, e) => ShowSettings();

            _itemExit = new ToolStripMenuItem(S.MenuExit);
            _itemExit.Click += (s, e) => { Logger.Log("Beenden clicked"); _realExit = true; Close(); };

            menu.Items.Add(_itemQuick1);
            menu.Items.Add(_itemQuick2);
            menu.Items.Add(_itemCustomTime);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_itemPause);
            menu.Items.Add(_itemStop);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_itemSettings);
            menu.Items.Add(_itemExit);

            _trayIcon.ContextMenuStrip = menu;
            UpdateTrayMenu();
        }

        private void StartTimer(int minutes)
        {
            Logger.Log($"StartTimer({minutes}), current state={_timer.State}");
            if (_timer.State != TimerState.Idle)
            {
                var rem    = _timer.Remaining;
                var result = MessageBox.Show(
                    S.TimerRunningRestart($"{rem:mm\\:ss}", minutes),
                    "TimeboxBar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes) return;
            }
            _timer.Stop();
            _timer.Start(TimeSpan.FromMinutes(minutes));
        }

        private void ShowCustomTimeDialog()
        {
            Logger.Log("ShowCustomTimeDialog");
            using (var dlg = new CustomTimeDialog())
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    StartTimer(dlg.Minutes);
            }
        }

        private void ShowQuickStartPopup()
        {
            using (var popup = new QuickStartPopup(_config.QuickStart1, _config.QuickStart2))
            {
                popup.ShowAtCursor();
                if (popup.ShowDialog(this) == DialogResult.OK)
                {
                    if (popup.SelectedMinutes == -1)
                        ShowCustomTimeDialog();
                    else
                        StartTimer(popup.SelectedMinutes);
                }
            }
        }

        private void ShowSettings()
        {
            Logger.Log("ShowSettings");
            _hotkey.UnregisterAll();
            string prevLang = _config.Language;
            try
            {
                using (var dlg = new SettingsForm(_config))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        _config.Save();
                        ApplyLanguage(prevLang);
                        ApplyConfig();
                    }
                    else
                    {
                        _hotkey.Register(Handle, _config.HotkeyModifier, _config.HotkeyKey);
                    }
                }
            }
            finally
            {
                if (!_hotkey.IsRegistered)
                    _hotkey.Register(Handle, _config.HotkeyModifier, _config.HotkeyKey);
            }
        }

        private void ApplyLanguage(string prevLang)
        {
            if (_config.Language == prevLang) return;

            var culture = _config.Language == "auto"
                ? System.Globalization.CultureInfo.InstalledUICulture
                : new System.Globalization.CultureInfo(_config.Language);

            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
            Logger.Log($"Language changed to: {culture.Name}");
            RefreshMenuStrings();
        }

        private void RefreshMenuStrings()
        {
            _itemCustomTime.Text = S.MenuCustomTime;
            _itemSettings.Text   = S.MenuSettings;
            _itemExit.Text       = S.MenuExit;
            UpdateTrayMenu();
        }

        private void UpdateTrayText()
        {
            string text;
            Color  iconColor;

            switch (_timer.State)
            {
                case TimerState.Running:
                    var rem = _timer.Remaining;
                    text      = S.TrayRunning($"{rem:mm\\:ss}");
                    iconColor = ProgressColor(_timer.Progress);
                    break;
                case TimerState.Paused:
                    text      = S.TrayPaused($"{_timer.Remaining:mm\\:ss}");
                    iconColor = Color.DimGray;
                    break;
                default:
                    text      = "TimeboxBar";
                    iconColor = Color.DimGray;
                    break;
            }

            _trayIcon.Text = text.Length > 63 ? text.Substring(0, 63) : text;

            var newIcon = GenerateIcon(iconColor);
            _trayIcon.Icon = newIcon;
            DisposeIcon(ref _cachedIcon);
            _cachedIcon = newIcon;
        }

        private void UpdateTrayMenu()
        {
            bool running   = _timer.State != TimerState.Idle;
            _itemPause.Enabled = running;
            _itemStop.Enabled  = running;
            _itemPause.Text    = _timer.State == TimerState.Paused ? S.MenuResume : S.MenuPause;

            if (running)
            {
                var rem = _timer.Remaining;
                _itemQuick1.Text = S.MenuQuickStartReplace(_config.QuickStart1, $"{rem:mm\\:ss}");
                _itemQuick2.Text = S.MenuQuickStartReplace(_config.QuickStart2, $"{rem:mm\\:ss}");
            }
            else
            {
                _itemQuick1.Text = S.MenuQuickStart(_config.QuickStart1);
                _itemQuick2.Text = S.MenuQuickStart(_config.QuickStart2);
            }
        }

        // ── Icon-Generierung ─────────────────────────────────────────────────────

        private static Icon GenerateIcon(Color color)
        {
            var bmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                using (var b = new SolidBrush(color))
                    g.FillRectangle(b, 1, 10, 14, 4);
            }
            var handle = bmp.GetHicon();
            bmp.Dispose();
            return Icon.FromHandle(handle);
        }

        private static void DisposeIcon(ref Icon icon)
        {
            if (icon == null) return;
            DestroyIcon(icon.Handle);
            icon.Dispose();
            icon = null;
        }
    }
}
