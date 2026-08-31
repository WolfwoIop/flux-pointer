using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("Flux Pointer")]
[assembly: AssemblyDescription("Animated global pointer effects for Windows")]
[assembly: AssemblyCompany("Flux Pointer")]
[assembly: AssemblyProduct("Flux Pointer for Windows")]
[assembly: AssemblyCopyright("Copyright (c) 2026 WolfwoIop")]
[assembly: AssemblyVersion("1.5.0.0")]
[assembly: AssemblyFileVersion("1.5.0.0")]

namespace FluxPointer
{
    internal static class Program
    {
        private const string SingleInstanceName = "FluxPointer-4F6B2A68-55F8-45C3-9A14-11D6386CF341";

        [STAThread]
        private static void Main()
        {
            bool ownsMutex;
            using (Mutex mutex = new Mutex(true, SingleInstanceName, out ownsMutex))
            {
                if (!ownsMutex)
                {
                    MessageBox.Show("Flux Pointer is already running. Look for its colorful icon in the system tray.",
                        "Flux Pointer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                SystemCursorController.Restore();
                EnableBestDpiAwareness();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs eventArgs)
                {
                    SystemCursorController.Restore();
                    MessageBox.Show("Flux Pointer stopped unexpectedly, so the normal Windows cursor was restored.\n\n" +
                        eventArgs.Exception.Message, "Flux Pointer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Application.Exit();
                };
                AppDomain.CurrentDomain.UnhandledException += delegate
                {
                    SystemCursorController.Restore();
                };
                AppDomain.CurrentDomain.ProcessExit += delegate
                {
                    SystemCursorController.Restore();
                };
                Application.Run(new FluxApplicationContext());
                GC.KeepAlive(mutex);
            }
        }

        private static void EnableBestDpiAwareness()
        {
            try
            {
                if (NativeMethods.SetProcessDpiAwarenessContext(
                    NativeMethods.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2))
                    return;
            }
            catch (EntryPointNotFoundException)
            {
            }

            NativeMethods.SetProcessDPIAware();
        }
    }

    internal enum FluxSkin
    {
        SpectrumDrift = 0,
        AcidGhost = 1,
        SolarFlare = 2,
        IceSignal = 3,
        MonoPulse = 4
    }

    internal enum FluxIntensity
    {
        Calm = 0,
        Balanced = 1,
        Vivid = 2
    }

    internal sealed class FluxApplicationContext : ApplicationContext
    {
        private const string StartupKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string StartupValueName = "FluxPointer";
        private const string SettingsKeyPath = @"Software\FluxPointer";
        private const string SkinValueName = "Skin";
        private const string IntensityValueName = "Intensity";
        private const string HideCursorValueName = "HideNormalCursor";
        private const string WelcomeShownValueName = "WelcomeShown";

        private readonly FluxOverlay overlay;
        private readonly NotifyIcon trayIcon;
        private readonly ContextMenuStrip trayMenu;
        private readonly ToolStripLabel statusItem;
        private readonly ToolStripMenuItem enabledItem;
        private readonly ToolStripMenuItem hideCursorItem;
        private readonly ToolStripMenuItem startupItem;
        private readonly Dictionary<FluxSkin, ToolStripMenuItem> skinItems;
        private readonly Dictionary<FluxIntensity, ToolStripMenuItem> intensityItems;
        private readonly List<Image> ownedMenuImages;
        private Icon ownedIcon;

        public FluxApplicationContext()
        {
            overlay = new FluxOverlay();
            FluxSkin selectedSkin = LoadSkin();
            FluxIntensity selectedIntensity = LoadIntensity();
            overlay.Skin = selectedSkin;
            overlay.Intensity = selectedIntensity;
            overlay.Show();
            skinItems = new Dictionary<FluxSkin, ToolStripMenuItem>();
            intensityItems = new Dictionary<FluxIntensity, ToolStripMenuItem>();
            ownedMenuImages = new List<Image>();

            statusItem = new ToolStripLabel();
            statusItem.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
            statusItem.Padding = new Padding(5, 3, 5, 4);

            enabledItem = new ToolStripMenuItem("Pause effects", null, ToggleEffects);
            enabledItem.CheckOnClick = false;

            hideCursorItem = new ToolStripMenuItem("Hide normal Windows cursor", null, ToggleNormalCursor);
            bool shouldHideCursor = LoadBooleanSetting(HideCursorValueName, true);
            hideCursorItem.Checked = shouldHideCursor && SystemCursorController.Hide();
            hideCursorItem.CheckOnClick = false;

            startupItem = new ToolStripMenuItem("Start with Windows", null, ToggleStartup);
            startupItem.Checked = IsStartupEnabled();
            startupItem.CheckOnClick = false;

            trayMenu = new ContextMenuStrip();
            trayMenu.Renderer = new ToolStripProfessionalRenderer(new FluxColorTable());
            trayMenu.BackColor = Color.FromArgb(20, 23, 31);
            trayMenu.ForeColor = Color.FromArgb(242, 245, 255);
            trayMenu.Font = new Font("Segoe UI", 9.5f);
            trayMenu.ShowCheckMargin = true;
            trayMenu.ShowImageMargin = true;
            trayMenu.Padding = new Padding(5);
            trayMenu.Items.Add(statusItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(enabledItem);
            trayMenu.Items.Add(hideCursorItem);

            ToolStripMenuItem skinMenu = new ToolStripMenuItem("Choose skin");
            AddSkinItem(skinMenu, "Spectrum Drift", FluxSkin.SpectrumDrift, selectedSkin);
            AddSkinItem(skinMenu, "Acid Ghost", FluxSkin.AcidGhost, selectedSkin);
            AddSkinItem(skinMenu, "Solar Flare", FluxSkin.SolarFlare, selectedSkin);
            AddSkinItem(skinMenu, "Ice Signal", FluxSkin.IceSignal, selectedSkin);
            AddSkinItem(skinMenu, "Mono Pulse", FluxSkin.MonoPulse, selectedSkin);
            trayMenu.Items.Add(skinMenu);

            ToolStripMenuItem intensityMenu = new ToolStripMenuItem("Visual intensity");
            AddIntensityItem(intensityMenu, "Calm — best performance", FluxIntensity.Calm,
                selectedIntensity);
            AddIntensityItem(intensityMenu, "Balanced", FluxIntensity.Balanced,
                selectedIntensity);
            AddIntensityItem(intensityMenu, "Vivid — maximum particles", FluxIntensity.Vivid,
                selectedIntensity);
            trayMenu.Items.Add(intensityMenu);
            trayMenu.Items.Add(startupItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(new ToolStripMenuItem("Open web demo", null, OpenDemo));
            trayMenu.Items.Add(new ToolStripMenuItem("About Flux Pointer", null, ShowAbout));
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(new ToolStripMenuItem("Exit Flux Pointer", null, ExitApplication));
            trayMenu.Opening += delegate
            {
                startupItem.Checked = IsStartupEnabled();
                UpdateMenuState();
            };

            ownedIcon = CreateFluxIcon();
            trayIcon = new NotifyIcon();
            trayIcon.Icon = ownedIcon;
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.MouseClick += delegate(object sender, MouseEventArgs eventArgs)
            {
                if (eventArgs.Button == MouseButtons.Left)
                    trayMenu.Show(Cursor.Position);
            };
            trayIcon.MouseDoubleClick += delegate(object sender, MouseEventArgs eventArgs)
            {
                if (eventArgs.Button == MouseButtons.Left)
                    ToggleEffects(sender, EventArgs.Empty);
            };
            UpdateMenuState();

            trayIcon.BalloonTipTitle = "Flux Pointer is active";
            trayIcon.BalloonTipText = "Move and click to disturb the field. Open the tray icon to choose a skin and intensity.";
            trayIcon.BalloonTipIcon = ToolTipIcon.None;
            if (!LoadBooleanSetting(WelcomeShownValueName, false))
            {
                trayIcon.ShowBalloonTip(3000);
                SaveBooleanSetting(WelcomeShownValueName, true);
            }

            SystemEvents.SessionSwitch += OnSessionSwitch;
        }

        private void AddSkinItem(ToolStripMenuItem parent, string label, FluxSkin skin,
            FluxSkin selectedSkin)
        {
            Image preview = CreateSkinPreview(skin);
            ownedMenuImages.Add(preview);
            ToolStripMenuItem item = new ToolStripMenuItem(label, preview);
            item.Tag = skin;
            item.Checked = skin == selectedSkin;
            item.Click += SelectSkin;
            skinItems.Add(skin, item);
            parent.DropDownItems.Add(item);
        }

        private void AddIntensityItem(ToolStripMenuItem parent, string label,
            FluxIntensity intensity, FluxIntensity selectedIntensity)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(label);
            item.Tag = intensity;
            item.Checked = intensity == selectedIntensity;
            item.Click += SelectIntensity;
            intensityItems.Add(intensity, item);
            parent.DropDownItems.Add(item);
        }

        private void SelectSkin(object sender, EventArgs eventArgs)
        {
            ToolStripMenuItem selectedItem = sender as ToolStripMenuItem;
            if (selectedItem == null || !(selectedItem.Tag is FluxSkin))
                return;

            FluxSkin skin = (FluxSkin)selectedItem.Tag;
            overlay.Skin = skin;
            foreach (KeyValuePair<FluxSkin, ToolStripMenuItem> item in skinItems)
                item.Value.Checked = item.Key == skin;
            SaveSkin(skin);
            UpdateMenuState();
        }

        private void SelectIntensity(object sender, EventArgs eventArgs)
        {
            ToolStripMenuItem selectedItem = sender as ToolStripMenuItem;
            if (selectedItem == null || !(selectedItem.Tag is FluxIntensity))
                return;

            FluxIntensity intensity = (FluxIntensity)selectedItem.Tag;
            overlay.Intensity = intensity;
            foreach (KeyValuePair<FluxIntensity, ToolStripMenuItem> item in intensityItems)
                item.Value.Checked = item.Key == intensity;
            SaveIntegerSetting(IntensityValueName, (int)intensity);
            UpdateMenuState();
        }

        private static FluxSkin LoadSkin()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(SettingsKeyPath, false))
                {
                    object value = key == null ? null : key.GetValue(SkinValueName);
                    int number;
                    if (value != null && int.TryParse(value.ToString(), out number) &&
                        Enum.IsDefined(typeof(FluxSkin), number))
                        return (FluxSkin)number;
                }
            }
            catch
            {
            }
            return FluxSkin.SpectrumDrift;
        }

        private static void SaveSkin(FluxSkin skin)
        {
            SaveIntegerSetting(SkinValueName, (int)skin);
        }

        private static FluxIntensity LoadIntensity()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(SettingsKeyPath, false))
                {
                    object value = key == null ? null : key.GetValue(IntensityValueName);
                    int number;
                    if (value != null && int.TryParse(value.ToString(), out number) &&
                        Enum.IsDefined(typeof(FluxIntensity), number))
                        return (FluxIntensity)number;
                }
            }
            catch
            {
            }
            return FluxIntensity.Balanced;
        }

        private static bool LoadBooleanSetting(string name, bool defaultValue)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(SettingsKeyPath, false))
                {
                    object value = key == null ? null : key.GetValue(name);
                    int number;
                    if (value != null && int.TryParse(value.ToString(), out number))
                        return number != 0;
                }
            }
            catch
            {
            }
            return defaultValue;
        }

        private static void SaveBooleanSetting(string name, bool value)
        {
            SaveIntegerSetting(name, value ? 1 : 0);
        }

        private static void SaveIntegerSetting(string name, int value)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(SettingsKeyPath))
                {
                    if (key != null)
                        key.SetValue(name, value, RegistryValueKind.DWord);
                }
            }
            catch
            {
            }
        }

        private void UpdateMenuState()
        {
            string skinName = GetSkinName(overlay.Skin);
            string intensityName = overlay.Intensity.ToString().ToUpperInvariant();
            statusItem.Text = overlay.EffectsEnabled
                ? "●  ACTIVE   ·   " + skinName + "   ·   " + intensityName
                : "○  PAUSED   ·   " + skinName;
            statusItem.ForeColor = overlay.EffectsEnabled
                ? Color.FromArgb(117, 243, 221)
                : Color.FromArgb(164, 170, 187);
            enabledItem.Text = overlay.EffectsEnabled ? "Pause effects" : "Resume effects";
            enabledItem.Checked = overlay.EffectsEnabled;
            trayIcon.Text = overlay.EffectsEnabled
                ? "Flux Pointer — " + skinName
                : "Flux Pointer — paused";
        }

        private static string GetSkinName(FluxSkin value)
        {
            if (value == FluxSkin.AcidGhost) return "Acid Ghost";
            if (value == FluxSkin.SolarFlare) return "Solar Flare";
            if (value == FluxSkin.IceSignal) return "Ice Signal";
            if (value == FluxSkin.MonoPulse) return "Mono Pulse";
            return "Spectrum Drift";
        }

        private void ToggleEffects(object sender, EventArgs eventArgs)
        {
            overlay.EffectsEnabled = !overlay.EffectsEnabled;
            if (overlay.EffectsEnabled && hideCursorItem.Checked)
                hideCursorItem.Checked = SystemCursorController.Hide();
            else
                SystemCursorController.Restore();
            UpdateMenuState();
        }

        private void ToggleNormalCursor(object sender, EventArgs eventArgs)
        {
            bool shouldHide = !hideCursorItem.Checked;
            hideCursorItem.Checked = shouldHide && overlay.EffectsEnabled
                ? SystemCursorController.Hide()
                : shouldHide;

            if (!shouldHide || !overlay.EffectsEnabled)
                SystemCursorController.Restore();

            SaveBooleanSetting(HideCursorValueName, shouldHide);
            UpdateMenuState();
        }

        private void OnSessionSwitch(object sender, SessionSwitchEventArgs eventArgs)
        {
            if (eventArgs.Reason == SessionSwitchReason.SessionLock ||
                eventArgs.Reason == SessionSwitchReason.SessionLogoff)
            {
                SystemCursorController.Restore();
                return;
            }

            if ((eventArgs.Reason == SessionSwitchReason.SessionUnlock ||
                eventArgs.Reason == SessionSwitchReason.SessionLogon) &&
                overlay.EffectsEnabled && hideCursorItem.Checked)
            {
                if (overlay.IsHandleCreated && !overlay.IsDisposed)
                {
                    overlay.BeginInvoke((MethodInvoker)delegate
                    {
                        hideCursorItem.Checked = SystemCursorController.Hide();
                        overlay.Wake();
                        UpdateMenuState();
                    });
                }
            }
        }

        private void ToggleStartup(object sender, EventArgs eventArgs)
        {
            bool shouldEnable = !IsStartupEnabled();
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(StartupKeyPath, true))
                {
                    if (key == null)
                        throw new InvalidOperationException("Windows startup settings are unavailable.");

                    if (shouldEnable)
                        key.SetValue(StartupValueName, "\"" + Application.ExecutablePath + "\"");
                    else
                        key.DeleteValue(StartupValueName, false);
                }

                startupItem.Checked = shouldEnable;
            }
            catch (Exception exception)
            {
                MessageBox.Show("Flux Pointer could not change the startup setting.\n\n" + exception.Message,
                    "Flux Pointer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                startupItem.Checked = IsStartupEnabled();
            }
        }

        private static bool IsStartupEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(StartupKeyPath, false))
                {
                    object value = key == null ? null : key.GetValue(StartupValueName);
                    return value != null && value.ToString().IndexOf(Application.ExecutablePath,
                        StringComparison.OrdinalIgnoreCase) >= 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private static void OpenDemo(object sender, EventArgs eventArgs)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://wolfwoiop.github.io/flux-pointer/")
                {
                    UseShellExecute = true
                });
            }
            catch (Exception exception)
            {
                MessageBox.Show("The demo could not be opened.\n\n" + exception.Message,
                    "Flux Pointer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static void ShowAbout(object sender, EventArgs eventArgs)
        {
            MessageBox.Show(
                "FLUX POINTER / WINDOWS\n\n" +
                "A living pointer field shaped by speed, direction, time, and clicks.\n\n" +
                "The normal Windows cursor is hidden while the Flux field is active.\n\n" +
                "No network connection, installer, or administrator access is required.\n\n" +
                "Version 1.5.0\n\n" +
                "Tip: double-click the tray icon to pause or resume instantly.",
                "About Flux Pointer", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ExitApplication(object sender, EventArgs eventArgs)
        {
            SystemCursorController.Restore();
            trayIcon.Visible = false;
            overlay.Close();
            ExitThread();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SystemEvents.SessionSwitch -= OnSessionSwitch;
                SystemCursorController.Restore();
                trayIcon.Dispose();
                trayMenu.Dispose();
                overlay.Dispose();
                for (int index = 0; index < ownedMenuImages.Count; index++)
                    ownedMenuImages[index].Dispose();
                if (ownedIcon != null)
                {
                    ownedIcon.Dispose();
                    ownedIcon = null;
                }
            }
            base.Dispose(disposing);
        }

        private static Icon CreateFluxIcon()
        {
            using (Bitmap bitmap = new Bitmap(32, 32, PixelFormat.Format32bppArgb))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.Clear(Color.Transparent);
                using (Pen cyan = new Pen(Color.FromArgb(255, 62, 238, 255), 3.0f))
                using (Pen violet = new Pen(Color.FromArgb(255, 195, 82, 255), 2.0f))
                using (SolidBrush acid = new SolidBrush(Color.FromArgb(255, 216, 255, 62)))
                {
                    graphics.DrawArc(cyan, 4, 4, 24, 24, -60, 205);
                    graphics.DrawArc(violet, 7, 7, 18, 18, 125, 190);
                    PointF[] diamond =
                    {
                        new PointF(16, 9), new PointF(20, 16),
                        new PointF(16, 23), new PointF(12, 16)
                    };
                    graphics.FillPolygon(acid, diamond);
                }

                IntPtr handle = bitmap.GetHicon();
                try
                {
                    using (Icon temporary = Icon.FromHandle(handle))
                    {
                        return (Icon)temporary.Clone();
                    }
                }
                finally
                {
                    NativeMethods.DestroyIcon(handle);
                }
            }
        }

        private static Image CreateSkinPreview(FluxSkin skin)
        {
            Bitmap bitmap = new Bitmap(22, 22, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.Clear(Color.Transparent);
                using (SolidBrush backdrop = new SolidBrush(Color.FromArgb(255, 31, 36, 48)))
                    graphics.FillEllipse(backdrop, 1, 1, 20, 20);

                if (skin == FluxSkin.AcidGhost)
                {
                    using (Pen acid = new Pen(Color.FromArgb(235, 188, 255, 58), 1.7f))
                    {
                        acid.DashPattern = new float[] { 2.0f, 1.3f };
                        graphics.DrawArc(acid, 4, 4, 14, 14, -40, 280);
                    }
                    using (SolidBrush bubble = new SolidBrush(Color.FromArgb(240, 100, 255, 189)))
                    {
                        graphics.FillEllipse(bubble, 8, 8, 6, 6);
                        graphics.FillEllipse(bubble, 15, 5, 2, 2);
                    }
                }
                else if (skin == FluxSkin.SolarFlare)
                {
                    using (Pen ray = new Pen(Color.FromArgb(245, 255, 116, 33), 1.4f))
                    {
                        graphics.DrawLine(ray, 11, 2, 11, 6);
                        graphics.DrawLine(ray, 11, 16, 11, 20);
                        graphics.DrawLine(ray, 2, 11, 6, 11);
                        graphics.DrawLine(ray, 16, 11, 20, 11);
                    }
                    using (SolidBrush sun = new SolidBrush(Color.FromArgb(255, 255, 215, 74)))
                        graphics.FillEllipse(sun, 6, 6, 10, 10);
                }
                else if (skin == FluxSkin.IceSignal)
                {
                    using (Pen ice = new Pen(Color.FromArgb(245, 107, 226, 255), 1.5f))
                    {
                        PointF[] diamond = { new PointF(11, 3), new PointF(19, 11),
                            new PointF(11, 19), new PointF(3, 11) };
                        graphics.DrawPolygon(ice, diamond);
                        graphics.DrawLine(ice, 7, 7, 15, 15);
                        graphics.DrawLine(ice, 15, 7, 7, 15);
                    }
                }
                else if (skin == FluxSkin.MonoPulse)
                {
                    using (Pen shadow = new Pen(Color.Black, 3.4f))
                    using (Pen signal = new Pen(Color.White, 1.3f))
                    {
                        graphics.DrawLine(shadow, 3, 11, 19, 11);
                        graphics.DrawLine(shadow, 11, 3, 11, 19);
                        graphics.DrawLine(signal, 3, 11, 19, 11);
                        graphics.DrawLine(signal, 11, 3, 11, 19);
                    }
                }
                else
                {
                    using (Pen cyan = new Pen(Color.FromArgb(245, 54, 238, 255), 1.8f))
                    using (Pen violet = new Pen(Color.FromArgb(235, 214, 74, 255), 1.4f))
                    {
                        graphics.DrawArc(cyan, 3, 3, 16, 16, -55, 220);
                        graphics.DrawArc(violet, 6, 6, 10, 10, 115, 210);
                    }
                }
            }
            return bitmap;
        }
    }

    internal sealed class FluxColorTable : ProfessionalColorTable
    {
        public FluxColorTable()
        {
            UseSystemColors = false;
        }

        public override Color ToolStripDropDownBackground
        {
            get { return Color.FromArgb(20, 23, 31); }
        }

        public override Color ImageMarginGradientBegin
        {
            get { return Color.FromArgb(25, 29, 39); }
        }

        public override Color ImageMarginGradientMiddle
        {
            get { return Color.FromArgb(25, 29, 39); }
        }

        public override Color ImageMarginGradientEnd
        {
            get { return Color.FromArgb(25, 29, 39); }
        }

        public override Color MenuItemSelected
        {
            get { return Color.FromArgb(48, 54, 73); }
        }

        public override Color MenuItemBorder
        {
            get { return Color.FromArgb(92, 230, 222); }
        }

        public override Color MenuBorder
        {
            get { return Color.FromArgb(69, 76, 98); }
        }

        public override Color SeparatorDark
        {
            get { return Color.FromArgb(55, 61, 78); }
        }

        public override Color SeparatorLight
        {
            get { return Color.FromArgb(24, 27, 36); }
        }

        public override Color CheckBackground
        {
            get { return Color.FromArgb(44, 192, 180); }
        }

        public override Color CheckSelectedBackground
        {
            get { return Color.FromArgb(61, 219, 207); }
        }

        public override Color CheckPressedBackground
        {
            get { return Color.FromArgb(34, 160, 153); }
        }
    }

    internal sealed class FluxOverlay : Form
    {
        private const int ActiveFrameInterval = 16;
        private const int IdleFrameInterval = 50;
        private const int SurfaceGridSize = 64;
        private const long TopMostRefreshMilliseconds = 150;
        private const long IdleDelayMilliseconds = 3000;
        private readonly System.Windows.Forms.Timer frameTimer;
        private readonly Stopwatch clock;
        private readonly Random random;
        private readonly List<Particle> particles;
        private readonly List<Ripple> ripples;

        private PointF target;
        private PointF previousTarget;
        private PointF eased;
        private bool pendingLeftClick;
        private bool pendingRightClick;
        private bool pendingInputActivity;
        private bool effectsEnabled;
        private FluxSkin skin;
        private FluxIntensity intensity;
        private float clickFlash;
        private float pointerOpacity;
        private long lastActivityTime;
        private long previousFrameTime;
        private long nextTopMostRefresh;
        private Bitmap renderSurface;
        private Graphics renderGraphics;

        public FluxOverlay()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            Bounds = new Rectangle(0, 0, 1, 1);

            particles = new List<Particle>();
            ripples = new List<Ripple>();
            random = new Random();
            clock = Stopwatch.StartNew();

            NativeMethods.POINT point;
            NativeMethods.GetCursorPos(out point);
            target = new PointF(point.X, point.Y);
            previousTarget = target;
            eased = target;

            effectsEnabled = true;
            skin = FluxSkin.SpectrumDrift;
            intensity = FluxIntensity.Balanced;
            pointerOpacity = 1.0f;
            lastActivityTime = clock.ElapsedMilliseconds;
            frameTimer = new System.Windows.Forms.Timer();
            frameTimer.Interval = ActiveFrameInterval;
            frameTimer.Tick += OnFrame;
            frameTimer.Start();
        }

        public bool EffectsEnabled
        {
            get { return effectsEnabled; }
            set
            {
                effectsEnabled = value;
                if (value)
                {
                    previousTarget = ReadCursor();
                    target = previousTarget;
                    eased = target;
                    pointerOpacity = 1.0f;
                    lastActivityTime = clock.ElapsedMilliseconds;
                    previousFrameTime = 0;
                    frameTimer.Interval = ActiveFrameInterval;
                    Show();
                    EnsureTopMost();
                }
                else
                {
                    particles.Clear();
                    ripples.Clear();
                    frameTimer.Interval = IdleFrameInterval;
                    Hide();
                }
            }
        }

        public FluxSkin Skin
        {
            get { return skin; }
            set
            {
                bool changed = skin != value;
                skin = value;
                pointerOpacity = 1.0f;
                lastActivityTime = clock.ElapsedMilliseconds;
                frameTimer.Interval = ActiveFrameInterval;
                if (changed)
                {
                    particles.Clear();
                    ripples.Clear();
                    PointF location = ReadCursor();
                    target = location;
                    previousTarget = location;
                    eased = location;
                    AddBurst(location, lastActivityTime, false);
                }
                if (effectsEnabled && !Visible)
                    Show();
            }
        }

        public FluxIntensity Intensity
        {
            get { return intensity; }
            set
            {
                intensity = value;
                TrimParticles();
                Wake();
            }
        }

        public void Wake()
        {
            lastActivityTime = clock.ElapsedMilliseconds;
            pointerOpacity = Math.Max(pointerOpacity, 0.35f);
            if (effectsEnabled)
            {
                frameTimer.Interval = ActiveFrameInterval;
                if (!Visible)
                    Show();
                EnsureTopMost();
            }
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override void OnHandleCreated(EventArgs eventArgs)
        {
            base.OnHandleCreated(eventArgs);
            NativeMethods.RAWINPUTDEVICE[] devices =
            {
                new NativeMethods.RAWINPUTDEVICE
                {
                    UsagePage = NativeMethods.HID_USAGE_PAGE_GENERIC,
                    Usage = NativeMethods.HID_USAGE_GENERIC_MOUSE,
                    Flags = NativeMethods.RIDEV_INPUTSINK,
                    Target = Handle
                }
            };
            NativeMethods.RegisterRawInputDevices(devices, 1,
                (uint)Marshal.SizeOf(typeof(NativeMethods.RAWINPUTDEVICE)));
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;
                parameters.ExStyle |= NativeMethods.WS_EX_LAYERED;
                parameters.ExStyle |= NativeMethods.WS_EX_TRANSPARENT;
                parameters.ExStyle |= NativeMethods.WS_EX_TOOLWINDOW;
                parameters.ExStyle |= NativeMethods.WS_EX_NOACTIVATE;
                parameters.ExStyle |= NativeMethods.WS_EX_TOPMOST;
                return parameters;
            }
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == NativeMethods.WM_INPUT)
                ProcessRawMouseInput(message.LParam);

            if (message.Msg == NativeMethods.WM_NCHITTEST)
            {
                message.Result = (IntPtr)NativeMethods.HTTRANSPARENT;
                return;
            }
            if (message.Msg == NativeMethods.WM_MOUSEACTIVATE)
            {
                message.Result = (IntPtr)NativeMethods.MA_NOACTIVATE;
                return;
            }
            base.WndProc(ref message);
        }

        private void ProcessRawMouseInput(IntPtr inputHandle)
        {
            uint size = (uint)Marshal.SizeOf(typeof(NativeMethods.RAWINPUT));
            NativeMethods.RAWINPUT input;
            uint read = NativeMethods.GetRawInputData(inputHandle, NativeMethods.RID_INPUT,
                out input, ref size, (uint)Marshal.SizeOf(typeof(NativeMethods.RAWINPUTHEADER)));

            if (read == uint.MaxValue || input.Header.Type != NativeMethods.RIM_TYPEMOUSE)
                return;

            ushort flags = input.Mouse.ButtonFlags;
            if (input.Mouse.LastX != 0 || input.Mouse.LastY != 0 || flags != 0)
            {
                pendingInputActivity = true;
                if (effectsEnabled)
                    frameTimer.Interval = ActiveFrameInterval;
            }
            if ((flags & NativeMethods.RI_MOUSE_LEFT_BUTTON_DOWN) != 0)
                pendingLeftClick = true;
            if ((flags & (NativeMethods.RI_MOUSE_RIGHT_BUTTON_DOWN |
                NativeMethods.RI_MOUSE_MIDDLE_BUTTON_DOWN)) != 0)
                pendingRightClick = true;
        }

        private void OnFrame(object sender, EventArgs eventArgs)
        {
            if (!effectsEnabled || !IsHandleCreated)
                return;

            long now = clock.ElapsedMilliseconds;
            float frameScale = previousFrameTime == 0 ? 1.0f : Math.Max(0.35f,
                Math.Min(2.5f, (now - previousFrameTime) / 16.6667f));
            previousFrameTime = now;

            target = ReadCursor();
            float dx = target.X - previousTarget.X;
            float dy = target.Y - previousTarget.Y;
            float movement = Distance(previousTarget, target);

            if (pendingInputActivity || movement > 0.6f)
                lastActivityTime = now;
            pendingInputActivity = false;

            if (movement > 1.25f)
                AddTrail(previousTarget, target, dx, dy, movement, now);

            eased.X += (target.X - eased.X) * Math.Min(1.0f, 0.29f * frameScale);
            eased.Y += (target.Y - eased.Y) * Math.Min(1.0f, 0.29f * frameScale);

            if (pendingLeftClick || pendingRightClick)
            {
                lastActivityTime = now;
                AddBurst(target, now, pendingRightClick);
                pendingLeftClick = false;
                pendingRightClick = false;
            }
            previousTarget = target;

            UpdateParticles(frameScale);
            UpdateRipples(frameScale);
            clickFlash = Math.Max(0.0f, clickFlash - 0.055f * frameScale);

            if (now - lastActivityTime < IdleDelayMilliseconds)
                pointerOpacity = Math.Min(1.0f, pointerOpacity + 0.18f * frameScale);
            else
                pointerOpacity = Math.Max(0.0f, pointerOpacity - 0.045f * frameScale);

            if (pointerOpacity <= 0.001f && particles.Count == 0 && ripples.Count == 0)
            {
                if (Visible)
                    Hide();
                frameTimer.Interval = IdleFrameInterval;
                return;
            }

            if (frameTimer.Interval != ActiveFrameInterval)
                frameTimer.Interval = ActiveFrameInterval;
            if (!Visible)
                Show();

            if (now >= nextTopMostRefresh)
            {
                EnsureTopMost();
                nextTopMostRefresh = now + TopMostRefreshMilliseconds;
            }

            Render(now, dx, dy);
        }

        private void EnsureTopMost()
        {
            NativeMethods.SetWindowPos(Handle, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE |
                NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
        }

        private static PointF ReadCursor()
        {
            NativeMethods.POINT point;
            NativeMethods.GetCursorPos(out point);
            return new PointF(point.X, point.Y);
        }

        private void AddTrail(PointF from, PointF to, float dx, float dy, float distance, long now)
        {
            int amount = Math.Min(7, Math.Max(1, (int)(distance / 11.0f)));
            if (intensity == FluxIntensity.Calm)
                amount = Math.Max(1, (amount + 1) / 2);
            else if (intensity == FluxIntensity.Vivid)
                amount = Math.Min(10, amount + 2);
            float hueBase = GetBaseHue(now);
            float hueSpread = GetHueSpread();

            for (int index = 0; index < amount; index++)
            {
                float fraction = amount == 1 ? 0.65f : (index + 1.0f) / (amount + 1.0f);
                float jitterX = (float)(random.NextDouble() - 0.5) * 8.0f;
                float jitterY = (float)(random.NextDouble() - 0.5) * 8.0f;
                float life = 22.0f + (float)random.NextDouble() * 23.0f;
                particles.Add(new Particle
                {
                    X = from.X + (to.X - from.X) * fraction + jitterX,
                    Y = from.Y + (to.Y - from.Y) * fraction + jitterY,
                    PreviousX = from.X,
                    PreviousY = from.Y,
                    VelocityX = -dx * 0.018f + (float)(random.NextDouble() - 0.5) * 0.8f,
                    VelocityY = -dy * 0.018f + (float)(random.NextDouble() - 0.5) * 0.8f,
                    Life = life,
                    MaxLife = life,
                    Size = 1.1f + (float)random.NextDouble() * 2.6f,
                    Hue = (hueBase + (float)random.NextDouble() * hueSpread) % 360.0f,
                    Rotation = (float)random.NextDouble() * 6.28318f,
                    Burst = false
                });
            }

            TrimParticles();
        }

        private void AddBurst(PointF origin, long now, bool alternate)
        {
            float hueBase = GetBaseHue(now);
            float hueStep = GetBurstHueStep(alternate);
            int count = alternate ? 38 : 30;
            if (intensity == FluxIntensity.Calm)
                count = alternate ? 26 : 20;
            else if (intensity == FluxIntensity.Vivid)
                count = alternate ? 48 : 38;

            for (int index = 0; index < count; index++)
            {
                double angle = Math.PI * 2.0 * index / count + (random.NextDouble() - 0.5) * 0.18;
                float force = 2.8f + (float)random.NextDouble() * (alternate ? 8.0f : 6.2f);
                float life = 31.0f + (float)random.NextDouble() * 24.0f;
                particles.Add(new Particle
                {
                    X = origin.X,
                    Y = origin.Y,
                    PreviousX = origin.X,
                    PreviousY = origin.Y,
                    VelocityX = (float)Math.Cos(angle) * force,
                    VelocityY = (float)Math.Sin(angle) * force,
                    Life = life,
                    MaxLife = life,
                    Size = 1.4f + (float)random.NextDouble() * 3.0f,
                    Hue = (hueBase + index * hueStep) % 360.0f,
                    Rotation = (float)angle,
                    Burst = true
                });
            }

            ripples.Add(new Ripple
            {
                X = origin.X,
                Y = origin.Y,
                Life = alternate ? 42.0f : 35.0f,
                MaxLife = alternate ? 42.0f : 35.0f,
                Hue = hueBase,
                Alternate = alternate
            });
            clickFlash = 1.0f;
            TrimParticles();
        }

        private void TrimParticles()
        {
            int overflow = particles.Count - GetMaxParticles();
            if (overflow > 0)
                particles.RemoveRange(0, overflow);
        }

        private int GetMaxParticles()
        {
            if (intensity == FluxIntensity.Calm)
                return 140;
            if (intensity == FluxIntensity.Vivid)
                return 300;
            return 220;
        }

        private void UpdateParticles(float frameScale)
        {
            for (int index = particles.Count - 1; index >= 0; index--)
            {
                Particle particle = particles[index];
                particle.PreviousX = particle.X;
                particle.PreviousY = particle.Y;
                particle.X += particle.VelocityX * frameScale;
                particle.Y += particle.VelocityY * frameScale;
                float damping = (float)Math.Pow(particle.Burst ? 0.945 : 0.91, frameScale);
                particle.VelocityX *= damping;
                particle.VelocityY = particle.VelocityY * damping + 0.012f * frameScale;
                particle.Rotation += (particle.Burst ? 0.12f : 0.036f) * frameScale;
                particle.Life -= frameScale;

                if (particle.Life <= 0)
                    particles.RemoveAt(index);
            }
        }

        private void UpdateRipples(float frameScale)
        {
            for (int index = ripples.Count - 1; index >= 0; index--)
            {
                ripples[index].Life -= frameScale;
                if (ripples[index].Life <= 0)
                    ripples.RemoveAt(index);
            }
        }

        private void Render(long now, float dx, float dy)
        {
            Rectangle renderBounds = CalculateRenderBounds();
            if (renderBounds.Width < 1 || renderBounds.Height < 1)
                return;

            EnsureRenderSurface(renderBounds.Width, renderBounds.Height);
            renderGraphics.CompositingMode = CompositingMode.SourceCopy;
            renderGraphics.Clear(Color.Transparent);
            renderGraphics.CompositingMode = CompositingMode.SourceOver;

            DrawParticles(renderGraphics, renderBounds);
            DrawRipples(renderGraphics, renderBounds, now);
            DrawPointer(renderGraphics, renderBounds, now, dx, dy);
            Present(renderSurface, renderBounds);
        }

        private void EnsureRenderSurface(int width, int height)
        {
            if (renderSurface != null && renderSurface.Width == width &&
                renderSurface.Height == height)
                return;

            if (renderGraphics != null)
            {
                renderGraphics.Dispose();
                renderGraphics = null;
            }
            if (renderSurface != null)
            {
                renderSurface.Dispose();
                renderSurface = null;
            }

            renderSurface = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            renderGraphics = Graphics.FromImage(renderSurface);
            renderGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            renderGraphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        }

        private Rectangle CalculateRenderBounds()
        {
            float minX = Math.Min(target.X, eased.X) - 110.0f;
            float minY = Math.Min(target.Y, eased.Y) - 110.0f;
            float maxX = Math.Max(target.X, eased.X) + 110.0f;
            float maxY = Math.Max(target.Y, eased.Y) + 110.0f;

            for (int index = 0; index < particles.Count; index++)
            {
                Particle particle = particles[index];
                float padding = particle.Size * 4.0f + 10.0f;
                minX = Math.Min(minX, Math.Min(particle.X, particle.PreviousX) - padding);
                minY = Math.Min(minY, Math.Min(particle.Y, particle.PreviousY) - padding);
                maxX = Math.Max(maxX, Math.Max(particle.X, particle.PreviousX) + padding);
                maxY = Math.Max(maxY, Math.Max(particle.Y, particle.PreviousY) + padding);
            }

            for (int index = 0; index < ripples.Count; index++)
            {
                Ripple ripple = ripples[index];
                float progress = 1.0f - ripple.Life / ripple.MaxLife;
                float radius = 18.0f + progress * (ripple.Alternate ? 122.0f : 92.0f) + 12.0f;
                minX = Math.Min(minX, ripple.X - radius);
                minY = Math.Min(minY, ripple.Y - radius);
                maxX = Math.Max(maxX, ripple.X + radius);
                maxY = Math.Max(maxY, ripple.Y + radius);
            }

            Rectangle virtualScreen = SystemInformation.VirtualScreen;
            int left = Math.Max(virtualScreen.Left, AlignDown((int)Math.Floor(minX),
                SurfaceGridSize));
            int top = Math.Max(virtualScreen.Top, AlignDown((int)Math.Floor(minY),
                SurfaceGridSize));
            int right = Math.Min(virtualScreen.Right, AlignUp((int)Math.Ceiling(maxX),
                SurfaceGridSize));
            int bottom = Math.Min(virtualScreen.Bottom, AlignUp((int)Math.Ceiling(maxY),
                SurfaceGridSize));

            return Rectangle.FromLTRB(left, top, Math.Max(left + 1, right), Math.Max(top + 1, bottom));
        }

        private static int AlignDown(int value, int alignment)
        {
            int remainder = value % alignment;
            if (remainder < 0)
                remainder += alignment;
            return value - remainder;
        }

        private static int AlignUp(int value, int alignment)
        {
            int down = AlignDown(value, alignment);
            return down == value ? value : down + alignment;
        }

        private void DrawParticles(Graphics graphics, Rectangle bounds)
        {
            float saturation = GetSkinSaturation();
            for (int index = 0; index < particles.Count; index++)
            {
                Particle particle = particles[index];
                float alpha = Math.Max(0.0f, particle.Life / particle.MaxLife);
                float x = particle.X - bounds.Left;
                float y = particle.Y - bounds.Top;

                if (skin == FluxSkin.AcidGhost)
                    DrawGhostParticle(graphics, bounds, particle, x, y, alpha, saturation);
                else if (skin == FluxSkin.SolarFlare)
                    DrawSolarParticle(graphics, bounds, particle, x, y, alpha, saturation);
                else if (skin == FluxSkin.IceSignal)
                    DrawIceParticle(graphics, bounds, particle, x, y, alpha, saturation);
                else if (skin == FluxSkin.MonoPulse)
                    DrawMonoParticle(graphics, bounds, particle, x, y, alpha);
                else if (particle.Burst)
                    DrawStar(graphics, x, y, particle.Size, particle.Rotation,
                        Hsl(particle.Hue, saturation, 0.72f, alpha * 0.95f));
                else
                    DrawSpectrumTrail(graphics, bounds, particle, x, y, alpha, saturation);
            }
        }

        private static void DrawSpectrumTrail(Graphics graphics, Rectangle bounds,
            Particle particle, float x, float y, float alpha, float saturation)
        {
            using (Pen line = new Pen(Hsl(particle.Hue, saturation, 0.68f, alpha * 0.50f),
                Math.Max(0.8f, particle.Size)))
            using (SolidBrush dot = new SolidBrush(Hsl(particle.Hue, saturation, 0.78f, alpha)))
            {
                line.StartCap = LineCap.Round;
                line.EndCap = LineCap.Round;
                graphics.DrawLine(line, particle.PreviousX - bounds.Left,
                    particle.PreviousY - bounds.Top, x, y);
                float radius = Math.Max(0.5f, particle.Size * alpha);
                graphics.FillEllipse(dot, x - radius, y - radius, radius * 2.0f, radius * 2.0f);
            }
        }

        private static void DrawGhostParticle(Graphics graphics, Rectangle bounds,
            Particle particle, float x, float y, float alpha, float saturation)
        {
            float radius = particle.Size * (particle.Burst ? 2.8f : 1.8f);
            using (Pen ghost = new Pen(Hsl(particle.Hue, saturation, 0.70f,
                alpha * (particle.Burst ? 0.92f : 0.65f)), Math.Max(0.8f, particle.Size * 0.65f)))
            {
                ghost.DashPattern = new float[] { 1.0f, 2.2f };
                if (!particle.Burst)
                    graphics.DrawLine(ghost, particle.PreviousX - bounds.Left,
                        particle.PreviousY - bounds.Top, x, y);
                graphics.DrawArc(ghost, x - radius, y - radius, radius * 2.0f,
                    radius * 2.0f, particle.Rotation * 57.29578f, particle.Burst ? 245.0f : 175.0f);
            }
        }

        private static void DrawSolarParticle(Graphics graphics, Rectangle bounds,
            Particle particle, float x, float y, float alpha, float saturation)
        {
            using (Pen ember = new Pen(Hsl(particle.Hue, saturation, 0.63f,
                alpha * 0.75f), Math.Max(1.0f, particle.Size * 1.35f)))
            {
                ember.StartCap = LineCap.Round;
                ember.EndCap = LineCap.Round;
                if (!particle.Burst)
                {
                    graphics.DrawLine(ember, particle.PreviousX - bounds.Left,
                        particle.PreviousY - bounds.Top, x, y);
                    using (SolidBrush heat = new SolidBrush(Hsl(particle.Hue + 18.0f,
                        saturation, 0.82f, alpha)))
                    {
                        float radius = particle.Size * 1.25f;
                        graphics.FillEllipse(heat, x - radius, y - radius,
                            radius * 2.0f, radius * 2.0f);
                    }
                }
                else
                {
                    GraphicsState saved = graphics.Save();
                    graphics.TranslateTransform(x, y);
                    graphics.RotateTransform(particle.Rotation * 57.29578f);
                    PointF[] spark =
                    {
                        new PointF(particle.Size * 4.6f, 0),
                        new PointF(-particle.Size * 1.4f, particle.Size * 0.9f),
                        new PointF(-particle.Size * 0.4f, 0),
                        new PointF(-particle.Size * 1.4f, -particle.Size * 0.9f)
                    };
                    using (SolidBrush flame = new SolidBrush(Hsl(particle.Hue + 16.0f,
                        saturation, 0.76f, alpha * 0.96f)))
                        graphics.FillPolygon(flame, spark);
                    graphics.Restore(saved);
                }
            }
        }

        private static void DrawIceParticle(Graphics graphics, Rectangle bounds,
            Particle particle, float x, float y, float alpha, float saturation)
        {
            using (Pen ice = new Pen(Hsl(particle.Hue, saturation, 0.82f,
                alpha * 0.90f), Math.Max(0.8f, particle.Size * 0.55f)))
            {
                if (!particle.Burst)
                {
                    graphics.DrawLine(ice, particle.PreviousX - bounds.Left,
                        particle.PreviousY - bounds.Top, x, y);
                    float radius = particle.Size * 1.7f;
                    PointF[] diamond =
                    {
                        new PointF(x, y - radius), new PointF(x + radius, y),
                        new PointF(x, y + radius), new PointF(x - radius, y)
                    };
                    graphics.DrawPolygon(ice, diamond);
                }
                else
                {
                    GraphicsState saved = graphics.Save();
                    graphics.TranslateTransform(x, y);
                    graphics.RotateTransform(particle.Rotation * 57.29578f);
                    float radius = particle.Size * 3.0f;
                    for (int arm = 0; arm < 6; arm++)
                    {
                        double angle = Math.PI * arm / 3.0;
                        graphics.DrawLine(ice, 0, 0, (float)Math.Cos(angle) * radius,
                            (float)Math.Sin(angle) * radius);
                    }
                    graphics.Restore(saved);
                }
            }
        }

        private static void DrawMonoParticle(Graphics graphics, Rectangle bounds,
            Particle particle, float x, float y, float alpha)
        {
            using (Pen shadow = new Pen(Color.FromArgb((int)(alpha * 150), 0, 0, 0),
                particle.Burst ? 3.2f : 2.4f))
            using (Pen white = new Pen(Color.FromArgb((int)(alpha * 245), 255, 255, 255),
                particle.Burst ? 1.1f : 0.9f))
            {
                if (!particle.Burst)
                {
                    graphics.DrawLine(shadow, particle.PreviousX - bounds.Left,
                        particle.PreviousY - bounds.Top, x, y);
                    graphics.DrawLine(white, particle.PreviousX - bounds.Left,
                        particle.PreviousY - bounds.Top, x, y);
                    float side = Math.Max(1.5f, particle.Size * 1.8f);
                    using (SolidBrush block = new SolidBrush(Color.FromArgb((int)(alpha * 245),
                        255, 255, 255)))
                        graphics.FillRectangle(block, x - side / 2.0f, y - side / 2.0f, side, side);
                }
                else
                {
                    float radius = particle.Size * 3.0f;
                    graphics.DrawLine(shadow, x - radius, y, x + radius, y);
                    graphics.DrawLine(shadow, x, y - radius, x, y + radius);
                    graphics.DrawLine(white, x - radius, y, x + radius, y);
                    graphics.DrawLine(white, x, y - radius, x, y + radius);
                }
            }
        }

        private void DrawRipples(Graphics graphics, Rectangle bounds, long now)
        {
            float saturation = GetSkinSaturation();
            for (int index = 0; index < ripples.Count; index++)
            {
                Ripple ripple = ripples[index];
                float progress = 1.0f - ripple.Life / ripple.MaxLife;
                float radius = 14.0f + progress * (ripple.Alternate ? 122.0f : 92.0f);
                float alpha = (1.0f - progress) * 0.80f;

                using (Pen pen = new Pen(Hsl(ripple.Hue + progress * GetHueSpread(),
                    saturation, 0.72f, alpha),
                    Math.Max(1.0f, 2.2f - progress)))
                {
                    pen.DashOffset = (float)(-now * 0.025);
                    float x = ripple.X - bounds.Left;
                    float y = ripple.Y - bounds.Top;
                    if (skin == FluxSkin.AcidGhost)
                    {
                        pen.DashPattern = new float[] { 1.0f, 2.2f };
                        graphics.DrawArc(pen, x - radius, y - radius, radius * 2.0f,
                            radius * 2.0f, -30.0f + progress * 120.0f, 125.0f);
                        graphics.DrawArc(pen, x - radius, y - radius, radius * 2.0f,
                            radius * 2.0f, 155.0f + progress * 90.0f, 100.0f);
                    }
                    else if (skin == FluxSkin.SolarFlare)
                    {
                        pen.DashStyle = DashStyle.Solid;
                        graphics.DrawEllipse(pen, x - radius, y - radius,
                            radius * 2.0f, radius * 2.0f);
                        float inner = radius * 0.76f;
                        graphics.DrawEllipse(pen, x - inner, y - inner,
                            inner * 2.0f, inner * 2.0f);
                    }
                    else if (skin == FluxSkin.IceSignal)
                    {
                        pen.DashPattern = new float[] { 3.0f, 3.0f };
                        PointF[] diamond =
                        {
                            new PointF(x, y - radius), new PointF(x + radius, y),
                            new PointF(x, y + radius), new PointF(x - radius, y)
                        };
                        graphics.DrawPolygon(pen, diamond);
                    }
                    else if (skin == FluxSkin.MonoPulse)
                    {
                        pen.DashPattern = new float[] { 1.0f, 5.0f };
                        graphics.DrawRectangle(pen, x - radius, y - radius,
                            radius * 2.0f, radius * 2.0f);
                    }
                    else
                    {
                        pen.DashPattern = ripple.Alternate
                            ? new float[] { 1.0f, 3.0f, 6.0f, 3.0f }
                            : new float[] { 4.0f + progress * 5.0f, 5.5f };
                        graphics.DrawEllipse(pen, x - radius, y - radius,
                            radius * 2.0f, radius * 2.0f);
                    }
                }
            }
        }

        private void DrawPointer(Graphics graphics, Rectangle bounds, long now, float dx, float dy)
        {
            float visibility = Clamp01(pointerOpacity);
            if (visibility <= 0.001f)
                return;

            float x = eased.X - bounds.Left;
            float y = eased.Y - bounds.Top;
            float speed = (float)Math.Sqrt(dx * dx + dy * dy);
            float direction = speed > 0.2f ? (float)(Math.Atan2(dy, dx) * 180.0 / Math.PI) : 0.0f;
            float hue = GetBaseHue(now);
            float saturation = GetSkinSaturation();
            float pulse = 1.0f + (float)Math.Sin(now * 0.0065) * 0.07f;
            float stretch = 1.0f + Math.Min(speed / 75.0f, 0.48f);

            GraphicsState saved = graphics.Save();
            graphics.TranslateTransform(x, y);
            graphics.RotateTransform(direction);
            graphics.ScaleTransform(stretch, 1.0f / (1.0f + (stretch - 1.0f) * 0.42f));

            for (int glow = 4; glow >= 1; glow--)
            {
                float radius = (17.0f + glow * 3.4f) * pulse;
                using (Pen glowPen = new Pen(Hsl(hue + glow * 8.0f, saturation, 0.66f,
                    0.045f * (5 - glow) * visibility), 2.6f + glow * 1.5f))
                    graphics.DrawEllipse(glowPen, -radius, -radius, radius * 2.0f, radius * 2.0f);
            }

            graphics.Restore(saved);

            DrawSkinCore(graphics, x, y, hue, saturation, visibility, pulse, now);
            DrawSkinOrbit(graphics, x, y, hue, saturation, visibility, now);

            if (clickFlash > 0.0f)
            {
                float flashRadius = 9.0f + (1.0f - clickFlash) * 24.0f;
                using (Pen flash = new Pen(Hsl(hue + 90.0f, saturation, 0.82f,
                    clickFlash * 0.9f * visibility),
                    1.0f + clickFlash * 2.5f))
                    graphics.DrawEllipse(flash, x - flashRadius, y - flashRadius,
                        flashRadius * 2.0f, flashRadius * 2.0f);
            }
        }

        private void DrawSkinCore(Graphics graphics, float x, float y, float hue,
            float saturation, float visibility, float pulse, long now)
        {
            if (skin == FluxSkin.AcidGhost)
            {
                using (Pen broken = new Pen(Hsl(hue, saturation, 0.75f,
                    0.94f * visibility), 2.1f))
                using (Pen bubble = new Pen(Hsl(hue + 16.0f, saturation, 0.84f,
                    0.72f * visibility), 1.0f))
                using (SolidBrush nucleus = new SolidBrush(Hsl(hue + 10.0f, saturation,
                    0.82f, 0.90f * visibility)))
                {
                    broken.DashPattern = new float[] { 4.0f, 2.0f, 1.0f, 3.0f };
                    graphics.DrawArc(broken, x - 20.0f * pulse, y - 20.0f * pulse,
                        40.0f * pulse, 40.0f * pulse, (float)(-now * 0.08 % 360.0), 285.0f);
                    graphics.DrawEllipse(bubble, x - 10.5f, y - 10.5f, 21.0f, 21.0f);
                    graphics.FillEllipse(nucleus, x - 3.0f, y - 3.0f, 6.0f, 6.0f);
                    graphics.FillEllipse(nucleus, x + 8.0f, y - 6.0f, 2.5f, 2.5f);
                    graphics.FillEllipse(nucleus, x - 9.0f, y + 5.0f, 2.0f, 2.0f);
                }
                return;
            }

            if (skin == FluxSkin.SolarFlare)
            {
                GraphicsState saved = graphics.Save();
                graphics.TranslateTransform(x, y);
                graphics.RotateTransform((float)(now * 0.045 % 360.0));
                using (Pen rays = new Pen(Hsl(hue + 20.0f, saturation, 0.78f,
                    0.88f * visibility), 1.7f))
                using (SolidBrush corona = new SolidBrush(Hsl(hue, saturation, 0.58f,
                    0.24f * visibility)))
                using (SolidBrush sun = new SolidBrush(Hsl(hue + 28.0f, saturation, 0.82f,
                    0.98f * visibility)))
                {
                    graphics.FillEllipse(corona, -18.0f * pulse, -18.0f * pulse,
                        36.0f * pulse, 36.0f * pulse);
                    for (int ray = 0; ray < 8; ray++)
                    {
                        double angle = Math.PI * ray / 4.0;
                        float inner = 15.0f;
                        float outer = 22.0f + (ray % 2) * 3.0f;
                        graphics.DrawLine(rays, (float)Math.Cos(angle) * inner,
                            (float)Math.Sin(angle) * inner, (float)Math.Cos(angle) * outer,
                            (float)Math.Sin(angle) * outer);
                    }
                    float core = 7.0f + clickFlash * 3.0f;
                    graphics.FillEllipse(sun, -core, -core, core * 2.0f, core * 2.0f);
                }
                graphics.Restore(saved);
                return;
            }

            if (skin == FluxSkin.IceSignal)
            {
                GraphicsState saved = graphics.Save();
                graphics.TranslateTransform(x, y);
                graphics.RotateTransform(45.0f + (float)(now * 0.032 % 360.0));
                using (Pen outer = new Pen(Hsl(hue, saturation, 0.84f,
                    0.92f * visibility), 1.7f))
                using (Pen inner = new Pen(Hsl(hue + 24.0f, saturation, 0.90f,
                    0.72f * visibility), 1.0f))
                using (SolidBrush crystal = new SolidBrush(Hsl(hue + 15.0f, saturation,
                    0.86f, 0.90f * visibility)))
                {
                    PointF[] large =
                    {
                        new PointF(0, -19.0f * pulse), new PointF(19.0f * pulse, 0),
                        new PointF(0, 19.0f * pulse), new PointF(-19.0f * pulse, 0)
                    };
                    PointF[] small =
                    {
                        new PointF(0, -11), new PointF(11, 0),
                        new PointF(0, 11), new PointF(-11, 0)
                    };
                    graphics.DrawPolygon(outer, large);
                    graphics.DrawPolygon(inner, small);
                    float center = 4.0f + clickFlash * 2.0f;
                    PointF[] centerDiamond =
                    {
                        new PointF(0, -center), new PointF(center, 0),
                        new PointF(0, center), new PointF(-center, 0)
                    };
                    graphics.FillPolygon(crystal, centerDiamond);
                }
                graphics.Restore(saved);
                return;
            }

            if (skin == FluxSkin.MonoPulse)
            {
                using (Pen shadow = new Pen(Color.FromArgb((int)(190 * visibility), 0, 0, 0), 4.0f))
                using (Pen signal = new Pen(Color.FromArgb((int)(245 * visibility), 255, 255, 255), 1.4f))
                using (SolidBrush center = new SolidBrush(Color.FromArgb((int)(250 * visibility),
                    255, 255, 255)))
                {
                    float reach = 20.0f * pulse;
                    graphics.DrawLine(shadow, x - reach, y, x + reach, y);
                    graphics.DrawLine(shadow, x, y - reach, x, y + reach);
                    graphics.DrawEllipse(shadow, x - 12.0f, y - 12.0f, 24.0f, 24.0f);
                    graphics.DrawLine(signal, x - reach, y, x + reach, y);
                    graphics.DrawLine(signal, x, y - reach, x, y + reach);
                    graphics.DrawEllipse(signal, x - 12.0f, y - 12.0f, 24.0f, 24.0f);
                    float side = 4.0f + clickFlash * 3.0f;
                    graphics.FillRectangle(center, x - side / 2.0f, y - side / 2.0f, side, side);
                }
                return;
            }

            using (Pen shell = new Pen(Hsl(hue, saturation, 0.73f,
                0.94f * visibility), 1.8f))
            using (Pen innerRing = new Pen(Hsl(hue + GetHueSpread(), saturation, 0.78f,
                0.78f * visibility), 1.1f))
            {
                graphics.DrawArc(shell, x - 19.0f * pulse, y - 19.0f * pulse,
                    38.0f * pulse, 38.0f * pulse, -38.0f, 238.0f);
                graphics.DrawArc(innerRing, x - 13.0f * pulse, y - 13.0f * pulse,
                    26.0f * pulse, 26.0f * pulse, 145.0f, 160.0f);
            }
            float diamondSize = 4.2f + clickFlash * 4.5f;
            DrawStar(graphics, x, y, diamondSize, (float)(now * 0.003),
                Hsl(hue + 35.0f, saturation, 0.80f, 0.96f * visibility));
        }

        private void DrawSkinOrbit(Graphics graphics, float x, float y, float hue,
            float saturation, float visibility, long now)
        {
            GraphicsState saved = graphics.Save();
            graphics.TranslateTransform(x, y);
            float rotation = skin == FluxSkin.AcidGhost
                ? (float)(-now * 0.085 % 360.0)
                : (float)(now * 0.10 % 360.0);
            graphics.RotateTransform(rotation);

            using (Pen orbit = new Pen(Hsl(hue + GetHueSpread(), saturation, 0.74f,
                0.72f * visibility), 1.15f))
            using (SolidBrush satellite = new SolidBrush(Hsl(hue + GetHueSpread() * 1.4f,
                saturation, 0.80f, 0.96f * visibility)))
            {
                if (skin == FluxSkin.AcidGhost)
                {
                    orbit.DashPattern = new float[] { 1.0f, 3.2f };
                    graphics.DrawArc(orbit, -25.0f, -25.0f, 50.0f, 50.0f, -25.0f, 128.0f);
                    graphics.DrawArc(orbit, -25.0f, -25.0f, 50.0f, 50.0f, 160.0f, 112.0f);
                    graphics.FillEllipse(satellite, -2.4f, -28.0f, 4.8f, 4.8f);
                }
                else if (skin == FluxSkin.SolarFlare)
                {
                    orbit.DashStyle = DashStyle.Solid;
                    graphics.DrawEllipse(orbit, -22.0f, -22.0f, 44.0f, 44.0f);
                    for (int ray = 0; ray < 10; ray++)
                    {
                        double angle = Math.PI * 2.0 * ray / 10.0;
                        float innerRadius = 25.0f;
                        float outerRadius = 30.0f + (ray % 2) * 4.0f;
                        graphics.DrawLine(orbit,
                            (float)Math.Cos(angle) * innerRadius,
                            (float)Math.Sin(angle) * innerRadius,
                            (float)Math.Cos(angle) * outerRadius,
                            (float)Math.Sin(angle) * outerRadius);
                    }
                    graphics.FillEllipse(satellite, 27.0f, -2.5f, 5.0f, 5.0f);
                }
                else if (skin == FluxSkin.IceSignal)
                {
                    orbit.DashPattern = new float[] { 3.0f, 3.0f };
                    PointF[] diamond =
                    {
                        new PointF(0, -29), new PointF(29, 0),
                        new PointF(0, 29), new PointF(-29, 0)
                    };
                    graphics.DrawPolygon(orbit, diamond);
                    graphics.DrawRectangle(orbit, -15.0f, -15.0f, 30.0f, 30.0f);
                    graphics.FillEllipse(satellite, -2.3f, -31.5f, 4.6f, 4.6f);
                }
                else if (skin == FluxSkin.MonoPulse)
                {
                    orbit.DashPattern = new float[] { 1.0f, 5.0f };
                    graphics.DrawEllipse(orbit, -25.0f, -25.0f, 50.0f, 50.0f);
                    graphics.DrawEllipse(orbit, -17.0f, -17.0f, 34.0f, 34.0f);
                    graphics.FillEllipse(satellite, 22.0f, -2.0f, 4.0f, 4.0f);
                }
                else
                {
                    orbit.DashPattern = new float[] { 2.0f, 4.5f };
                    graphics.DrawEllipse(orbit, -27.0f, -15.0f, 54.0f, 30.0f);
                    graphics.FillEllipse(satellite, 24.0f, -2.2f, 4.4f, 4.4f);
                }
            }
            graphics.Restore(saved);
        }

        private float GetBaseHue(long now)
        {
            if (skin == FluxSkin.AcidGhost)
                return 82.0f + (float)Math.Sin(now * 0.0011) * 14.0f;
            if (skin == FluxSkin.SolarFlare)
                return 18.0f + (float)Math.Sin(now * 0.0010) * 18.0f;
            if (skin == FluxSkin.IceSignal)
                return 198.0f + (float)Math.Sin(now * 0.0009) * 18.0f;
            if (skin == FluxSkin.MonoPulse)
                return 0.0f;
            return (float)((now * 0.045) % 360.0);
        }

        private float GetSkinSaturation()
        {
            if (skin == FluxSkin.MonoPulse)
                return 0.0f;
            if (skin == FluxSkin.IceSignal)
                return 0.88f;
            if (skin == FluxSkin.AcidGhost)
                return 0.96f;
            return 1.0f;
        }

        private float GetHueSpread()
        {
            if (skin == FluxSkin.AcidGhost)
                return 20.0f;
            if (skin == FluxSkin.SolarFlare)
                return 34.0f;
            if (skin == FluxSkin.IceSignal)
                return 28.0f;
            if (skin == FluxSkin.MonoPulse)
                return 0.0f;
            return 115.0f;
        }

        private float GetBurstHueStep(bool alternate)
        {
            if (skin == FluxSkin.AcidGhost)
                return alternate ? 1.1f : 0.8f;
            if (skin == FluxSkin.SolarFlare)
                return alternate ? 1.8f : 1.3f;
            if (skin == FluxSkin.IceSignal)
                return alternate ? 1.5f : 1.1f;
            if (skin == FluxSkin.MonoPulse)
                return 0.0f;
            return alternate ? 7.5f : 4.8f;
        }

        private static void DrawStar(Graphics graphics, float x, float y, float size,
            float rotation, Color color)
        {
            GraphicsState saved = graphics.Save();
            graphics.TranslateTransform(x, y);
            graphics.RotateTransform(rotation * 57.29578f);
            PointF[] points =
            {
                new PointF(0, -size * 2.35f),
                new PointF(size * 0.62f, -size * 0.62f),
                new PointF(size * 2.35f, 0),
                new PointF(size * 0.62f, size * 0.62f),
                new PointF(0, size * 2.35f),
                new PointF(-size * 0.62f, size * 0.62f),
                new PointF(-size * 2.35f, 0),
                new PointF(-size * 0.62f, -size * 0.62f)
            };
            using (SolidBrush brush = new SolidBrush(color))
                graphics.FillPolygon(brush, points);
            graphics.Restore(saved);
        }

        private void Present(Bitmap surface, Rectangle bounds)
        {
            IntPtr screenDc = NativeMethods.GetDC(IntPtr.Zero);
            IntPtr memoryDc = NativeMethods.CreateCompatibleDC(screenDc);
            IntPtr bitmapHandle = IntPtr.Zero;
            IntPtr oldBitmap = IntPtr.Zero;

            try
            {
                bitmapHandle = surface.GetHbitmap(Color.FromArgb(0));
                oldBitmap = NativeMethods.SelectObject(memoryDc, bitmapHandle);

                NativeMethods.POINT destination = new NativeMethods.POINT(bounds.Left, bounds.Top);
                NativeMethods.SIZE size = new NativeMethods.SIZE(bounds.Width, bounds.Height);
                NativeMethods.POINT source = new NativeMethods.POINT(0, 0);
                NativeMethods.BLENDFUNCTION blend = new NativeMethods.BLENDFUNCTION();
                blend.BlendOp = NativeMethods.AC_SRC_OVER;
                blend.SourceConstantAlpha = 255;
                blend.AlphaFormat = NativeMethods.AC_SRC_ALPHA;

                NativeMethods.UpdateLayeredWindow(Handle, screenDc, ref destination, ref size,
                    memoryDc, ref source, 0, ref blend, NativeMethods.ULW_ALPHA);
            }
            finally
            {
                if (oldBitmap != IntPtr.Zero)
                    NativeMethods.SelectObject(memoryDc, oldBitmap);
                if (bitmapHandle != IntPtr.Zero)
                    NativeMethods.DeleteObject(bitmapHandle);
                NativeMethods.DeleteDC(memoryDc);
                NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
            }
        }

        private static float Distance(PointF first, PointF second)
        {
            float dx = second.X - first.X;
            float dy = second.Y - first.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private static Color Hsl(float hue, float saturation, float lightness, float alpha)
        {
            hue = hue % 360.0f;
            if (hue < 0) hue += 360.0f;
            saturation = Clamp01(saturation);
            lightness = Clamp01(lightness);
            alpha = Clamp01(alpha);

            float chroma = (1.0f - Math.Abs(2.0f * lightness - 1.0f)) * saturation;
            float section = hue / 60.0f;
            float secondary = chroma * (1.0f - Math.Abs(section % 2.0f - 1.0f));
            float red = 0, green = 0, blue = 0;

            if (section < 1) { red = chroma; green = secondary; }
            else if (section < 2) { red = secondary; green = chroma; }
            else if (section < 3) { green = chroma; blue = secondary; }
            else if (section < 4) { green = secondary; blue = chroma; }
            else if (section < 5) { red = secondary; blue = chroma; }
            else { red = chroma; blue = secondary; }

            float match = lightness - chroma / 2.0f;
            return Color.FromArgb((int)(alpha * 255.0f),
                (int)((red + match) * 255.0f),
                (int)((green + match) * 255.0f),
                (int)((blue + match) * 255.0f));
        }

        private static float Clamp01(float value)
        {
            return Math.Max(0.0f, Math.Min(1.0f, value));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && frameTimer != null)
            {
                frameTimer.Stop();
                frameTimer.Dispose();
                if (renderGraphics != null)
                {
                    renderGraphics.Dispose();
                    renderGraphics = null;
                }
                if (renderSurface != null)
                {
                    renderSurface.Dispose();
                    renderSurface = null;
                }
            }
            base.Dispose(disposing);
        }
    }

    internal sealed class Particle
    {
        public float X;
        public float Y;
        public float PreviousX;
        public float PreviousY;
        public float VelocityX;
        public float VelocityY;
        public float Life;
        public float MaxLife;
        public float Size;
        public float Hue;
        public float Rotation;
        public bool Burst;
    }

    internal sealed class Ripple
    {
        public float X;
        public float Y;
        public float Life;
        public float MaxLife;
        public float Hue;
        public bool Alternate;
    }

    internal static class SystemCursorController
    {
        private static readonly uint[] CursorIds =
        {
            32512, 32513, 32514, 32515, 32516,
            32640, 32641, 32642, 32643, 32644,
            32645, 32646, 32648, 32649, 32650,
            32651, 32671, 32672
        };

        private static bool hidden;

        internal static bool Hide()
        {
            if (hidden)
                return true;

            byte[] andPlane = new byte[128];
            byte[] xorPlane = new byte[128];
            for (int index = 0; index < andPlane.Length; index++)
                andPlane[index] = 0xFF;

            bool normalCursorHidden = false;
            for (int index = 0; index < CursorIds.Length; index++)
            {
                IntPtr cursor = NativeMethods.CreateCursor(IntPtr.Zero, 0, 0, 32, 32,
                    andPlane, xorPlane);
                if (cursor == IntPtr.Zero)
                    continue;

                bool replaced = NativeMethods.SetSystemCursor(cursor, CursorIds[index]);
                if (!replaced)
                    NativeMethods.DestroyCursor(cursor);
                else if (CursorIds[index] == NativeMethods.OCR_NORMAL)
                    normalCursorHidden = true;
            }

            hidden = normalCursorHidden;
            if (!hidden)
                Restore();
            return hidden;
        }

        internal static void Restore()
        {
            NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETCURSORS, 0,
                IntPtr.Zero, 0);
            hidden = false;
        }
    }

    internal static class NativeMethods
    {
        internal const int WS_EX_LAYERED = 0x00080000;
        internal const int WS_EX_TRANSPARENT = 0x00000020;
        internal const int WS_EX_TOOLWINDOW = 0x00000080;
        internal const int WS_EX_NOACTIVATE = 0x08000000;
        internal const int WS_EX_TOPMOST = 0x00000008;
        internal const int WM_NCHITTEST = 0x0084;
        internal const int WM_MOUSEACTIVATE = 0x0021;
        internal const int WM_INPUT = 0x00FF;
        internal const int HTTRANSPARENT = -1;
        internal const int MA_NOACTIVATE = 3;
        internal const int ULW_ALPHA = 0x00000002;
        internal const byte AC_SRC_OVER = 0x00;
        internal const byte AC_SRC_ALPHA = 0x01;
        internal const uint RID_INPUT = 0x10000003;
        internal const uint RIM_TYPEMOUSE = 0;
        internal const ushort HID_USAGE_PAGE_GENERIC = 0x01;
        internal const ushort HID_USAGE_GENERIC_MOUSE = 0x02;
        internal const uint RIDEV_INPUTSINK = 0x00000100;
        internal const ushort RI_MOUSE_LEFT_BUTTON_DOWN = 0x0001;
        internal const ushort RI_MOUSE_RIGHT_BUTTON_DOWN = 0x0004;
        internal const ushort RI_MOUSE_MIDDLE_BUTTON_DOWN = 0x0010;
        internal const uint SPI_SETCURSORS = 0x0057;
        internal const uint OCR_NORMAL = 32512;
        internal const uint SWP_NOSIZE = 0x0001;
        internal const uint SWP_NOMOVE = 0x0002;
        internal const uint SWP_NOACTIVATE = 0x0010;
        internal const uint SWP_SHOWWINDOW = 0x0040;
        internal static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        internal static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 =
            new IntPtr(-4);

        [StructLayout(LayoutKind.Sequential)]
        internal struct POINT
        {
            public int X;
            public int Y;
            public POINT(int x, int y) { X = x; Y = y; }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SIZE
        {
            public int Width;
            public int Height;
            public SIZE(int width, int height) { Width = width; Height = height; }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RAWINPUTDEVICE
        {
            public ushort UsagePage;
            public ushort Usage;
            public uint Flags;
            public IntPtr Target;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RAWINPUTHEADER
        {
            public uint Type;
            public uint Size;
            public IntPtr Device;
            public IntPtr WParam;
        }

        [StructLayout(LayoutKind.Explicit, Size = 24)]
        internal struct RAWMOUSE
        {
            [FieldOffset(0)] public ushort Flags;
            [FieldOffset(4)] public uint Buttons;
            [FieldOffset(4)] public ushort ButtonFlags;
            [FieldOffset(6)] public ushort ButtonData;
            [FieldOffset(8)] public uint RawButtons;
            [FieldOffset(12)] public int LastX;
            [FieldOffset(16)] public int LastY;
            [FieldOffset(20)] public uint ExtraInformation;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RAWINPUT
        {
            public RAWINPUTHEADER Header;
            public RAWMOUSE Mouse;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(out POINT point);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] devices,
            uint deviceCount, uint deviceSize);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint GetRawInputData(IntPtr rawInput, uint command,
            out RAWINPUT data, ref uint size, uint headerSize);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr GetDC(IntPtr windowHandle);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int ReleaseDC(IntPtr windowHandle, IntPtr deviceContext);

        [DllImport("gdi32.dll", SetLastError = true)]
        internal static extern IntPtr CreateCompatibleDC(IntPtr deviceContext);

        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteDC(IntPtr deviceContext);

        [DllImport("gdi32.dll", SetLastError = true)]
        internal static extern IntPtr SelectObject(IntPtr deviceContext, IntPtr graphicsObject);

        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject(IntPtr graphicsObject);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UpdateLayeredWindow(IntPtr windowHandle, IntPtr destinationDc,
            ref POINT destinationPoint, ref SIZE size, IntPtr sourceDc, ref POINT sourcePoint,
            int colorKey, ref BLENDFUNCTION blend, int flags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DestroyIcon(IntPtr iconHandle);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr CreateCursor(IntPtr instance, int hotSpotX, int hotSpotY,
            int width, int height, byte[] andPlane, byte[] xorPlane);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetSystemCursor(IntPtr cursor, uint cursorId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DestroyCursor(IntPtr cursor);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SystemParametersInfo(uint action, uint parameter,
            IntPtr data, uint updateFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetWindowPos(IntPtr windowHandle, IntPtr insertAfter,
            int x, int y, int width, int height, uint flags);
    }
}
