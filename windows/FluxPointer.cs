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
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

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

                NativeMethods.SetProcessDPIAware();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new FluxApplicationContext());
                GC.KeepAlive(mutex);
            }
        }
    }

    internal sealed class FluxApplicationContext : ApplicationContext
    {
        private const string StartupKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string StartupValueName = "FluxPointer";

        private readonly FluxOverlay overlay;
        private readonly NotifyIcon trayIcon;
        private readonly ToolStripMenuItem enabledItem;
        private readonly ToolStripMenuItem startupItem;
        private Icon ownedIcon;

        public FluxApplicationContext()
        {
            overlay = new FluxOverlay();
            overlay.Show();

            enabledItem = new ToolStripMenuItem("Effects enabled", null, ToggleEffects);
            enabledItem.Checked = true;
            enabledItem.CheckOnClick = false;

            startupItem = new ToolStripMenuItem("Start with Windows", null, ToggleStartup);
            startupItem.Checked = IsStartupEnabled();
            startupItem.CheckOnClick = false;

            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add(enabledItem);
            menu.Items.Add(startupItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem("Open web demo", null, OpenDemo));
            menu.Items.Add(new ToolStripMenuItem("About Flux Pointer", null, ShowAbout));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem("Exit", null, ExitApplication));

            ownedIcon = CreateFluxIcon();
            trayIcon = new NotifyIcon();
            trayIcon.Icon = ownedIcon;
            trayIcon.Text = "Flux Pointer — effects enabled";
            trayIcon.ContextMenuStrip = menu;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += ToggleEffects;
            trayIcon.BalloonTipTitle = "Flux Pointer is active";
            trayIcon.BalloonTipText = "Move and click to disturb the field. Right-click the tray icon for controls.";
            trayIcon.BalloonTipIcon = ToolTipIcon.None;
            trayIcon.ShowBalloonTip(2600);
        }

        private void ToggleEffects(object sender, EventArgs eventArgs)
        {
            overlay.EffectsEnabled = !overlay.EffectsEnabled;
            enabledItem.Checked = overlay.EffectsEnabled;
            trayIcon.Text = overlay.EffectsEnabled
                ? "Flux Pointer — effects enabled"
                : "Flux Pointer — effects paused";
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
                "No network connection, installer, or administrator access is required.\n\n" +
                "Version 1.0.0",
                "About Flux Pointer", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ExitApplication(object sender, EventArgs eventArgs)
        {
            trayIcon.Visible = false;
            overlay.Close();
            ExitThread();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                trayIcon.Dispose();
                overlay.Dispose();
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
    }

    internal sealed class FluxOverlay : Form
    {
        private const int MaxParticles = 220;
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
        private bool effectsEnabled;
        private float clickFlash;
        private long previousFrameTime;

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
            frameTimer = new System.Windows.Forms.Timer();
            frameTimer.Interval = 16;
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
                    Show();
                }
                else
                {
                    particles.Clear();
                    ripples.Clear();
                    Hide();
                }
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
            if ((flags & NativeMethods.RI_MOUSE_LEFT_BUTTON_DOWN) != 0)
                pendingLeftClick = true;
            if ((flags & NativeMethods.RI_MOUSE_RIGHT_BUTTON_DOWN) != 0)
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

            if (movement > 1.25f)
                AddTrail(previousTarget, target, dx, dy, movement, now);

            eased.X += (target.X - eased.X) * Math.Min(1.0f, 0.29f * frameScale);
            eased.Y += (target.Y - eased.Y) * Math.Min(1.0f, 0.29f * frameScale);

            if (pendingLeftClick || pendingRightClick)
            {
                AddBurst(target, now, pendingRightClick);
                pendingLeftClick = false;
                pendingRightClick = false;
            }
            previousTarget = target;

            UpdateParticles(frameScale);
            UpdateRipples(frameScale);
            clickFlash = Math.Max(0.0f, clickFlash - 0.055f * frameScale);

            Render(now, dx, dy);
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
            float hueBase = (float)((now * 0.045) % 360.0);

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
                    Hue = (hueBase + (float)random.NextDouble() * 85.0f) % 360.0f,
                    Rotation = (float)random.NextDouble() * 6.28318f,
                    Burst = false
                });
            }

            TrimParticles();
        }

        private void AddBurst(PointF origin, long now, bool alternate)
        {
            float hueBase = (float)((now * 0.045) % 360.0);
            int count = alternate ? 38 : 30;

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
                    Hue = (hueBase + index * (alternate ? 7.5f : 4.8f)) % 360.0f,
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
            int overflow = particles.Count - MaxParticles;
            if (overflow > 0)
                particles.RemoveRange(0, overflow);
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

            using (Bitmap surface = new Bitmap(renderBounds.Width, renderBounds.Height,
                PixelFormat.Format32bppArgb))
            using (Graphics graphics = Graphics.FromImage(surface))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.CompositingMode = CompositingMode.SourceOver;
                graphics.Clear(Color.Transparent);

                DrawParticles(graphics, renderBounds);
                DrawRipples(graphics, renderBounds, now);
                DrawPointer(graphics, renderBounds, now, dx, dy);
                Present(surface, renderBounds);
            }
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
            int left = Math.Max(virtualScreen.Left, (int)Math.Floor(minX));
            int top = Math.Max(virtualScreen.Top, (int)Math.Floor(minY));
            int right = Math.Min(virtualScreen.Right, (int)Math.Ceiling(maxX));
            int bottom = Math.Min(virtualScreen.Bottom, (int)Math.Ceiling(maxY));

            return Rectangle.FromLTRB(left, top, Math.Max(left + 1, right), Math.Max(top + 1, bottom));
        }

        private void DrawParticles(Graphics graphics, Rectangle bounds)
        {
            for (int index = 0; index < particles.Count; index++)
            {
                Particle particle = particles[index];
                float alpha = Math.Max(0.0f, particle.Life / particle.MaxLife);
                float x = particle.X - bounds.Left;
                float y = particle.Y - bounds.Top;

                if (particle.Burst)
                {
                    DrawStar(graphics, x, y, particle.Size, particle.Rotation,
                        Hsl(particle.Hue, 1.0f, 0.72f, alpha * 0.95f));
                }
                else
                {
                    using (Pen line = new Pen(Hsl(particle.Hue, 1.0f, 0.68f, alpha * 0.50f),
                        Math.Max(0.8f, particle.Size)))
                    using (SolidBrush dot = new SolidBrush(Hsl(particle.Hue, 1.0f, 0.78f, alpha)))
                    {
                        line.StartCap = LineCap.Round;
                        line.EndCap = LineCap.Round;
                        graphics.DrawLine(line, particle.PreviousX - bounds.Left,
                            particle.PreviousY - bounds.Top, x, y);
                        float radius = Math.Max(0.5f, particle.Size * alpha);
                        graphics.FillEllipse(dot, x - radius, y - radius, radius * 2.0f, radius * 2.0f);
                    }
                }
            }
        }

        private void DrawRipples(Graphics graphics, Rectangle bounds, long now)
        {
            for (int index = 0; index < ripples.Count; index++)
            {
                Ripple ripple = ripples[index];
                float progress = 1.0f - ripple.Life / ripple.MaxLife;
                float radius = 14.0f + progress * (ripple.Alternate ? 122.0f : 92.0f);
                float alpha = (1.0f - progress) * 0.80f;

                using (Pen pen = new Pen(Hsl(ripple.Hue + progress * 75.0f, 1.0f, 0.72f, alpha),
                    Math.Max(1.0f, 2.2f - progress)))
                {
                    pen.DashStyle = DashStyle.Custom;
                    pen.DashPattern = ripple.Alternate
                        ? new float[] { 1.0f, 3.0f, 6.0f, 3.0f }
                        : new float[] { 4.0f + progress * 5.0f, 5.5f };
                    pen.DashOffset = (float)(-now * 0.025);
                    graphics.DrawEllipse(pen, ripple.X - bounds.Left - radius,
                        ripple.Y - bounds.Top - radius, radius * 2.0f, radius * 2.0f);
                }
            }
        }

        private void DrawPointer(Graphics graphics, Rectangle bounds, long now, float dx, float dy)
        {
            float x = eased.X - bounds.Left;
            float y = eased.Y - bounds.Top;
            float speed = (float)Math.Sqrt(dx * dx + dy * dy);
            float direction = speed > 0.2f ? (float)(Math.Atan2(dy, dx) * 180.0 / Math.PI) : 0.0f;
            float hue = (float)((now * 0.045) % 360.0);
            float pulse = 1.0f + (float)Math.Sin(now * 0.0065) * 0.07f;
            float stretch = 1.0f + Math.Min(speed / 75.0f, 0.48f);

            GraphicsState saved = graphics.Save();
            graphics.TranslateTransform(x, y);
            graphics.RotateTransform(direction);
            graphics.ScaleTransform(stretch, 1.0f / (1.0f + (stretch - 1.0f) * 0.42f));

            for (int glow = 4; glow >= 1; glow--)
            {
                float radius = (17.0f + glow * 3.4f) * pulse;
                using (Pen glowPen = new Pen(Hsl(hue + glow * 8.0f, 1.0f, 0.66f,
                    0.045f * (5 - glow)), 2.6f + glow * 1.5f))
                    graphics.DrawEllipse(glowPen, -radius, -radius, radius * 2.0f, radius * 2.0f);
            }

            using (Pen shell = new Pen(Hsl(hue, 1.0f, 0.73f, 0.94f), 1.8f))
            using (Pen inner = new Pen(Hsl(hue + 72.0f, 1.0f, 0.78f, 0.78f), 1.1f))
            {
                graphics.DrawArc(shell, -19.0f * pulse, -19.0f * pulse, 38.0f * pulse,
                    38.0f * pulse, -38.0f, 238.0f);
                graphics.DrawArc(inner, -13.0f * pulse, -13.0f * pulse, 26.0f * pulse,
                    26.0f * pulse, 145.0f, 160.0f);
            }
            graphics.Restore(saved);

            float orbitAngle = (float)(now * 0.12 % 360.0);
            saved = graphics.Save();
            graphics.TranslateTransform(x, y);
            graphics.RotateTransform(orbitAngle);
            using (Pen orbit = new Pen(Hsl(hue + 155.0f, 1.0f, 0.72f, 0.68f), 1.1f))
            {
                orbit.DashPattern = new float[] { 2.0f, 4.5f };
                graphics.DrawEllipse(orbit, -27.0f, -15.0f, 54.0f, 30.0f);
            }
            using (SolidBrush satellite = new SolidBrush(Hsl(hue + 190.0f, 1.0f, 0.78f, 0.95f)))
                graphics.FillEllipse(satellite, 24.0f, -2.2f, 4.4f, 4.4f);
            graphics.Restore(saved);

            float diamondSize = 4.2f + clickFlash * 4.5f;
            DrawStar(graphics, x, y, diamondSize, (float)(now * 0.003),
                Hsl(hue + 35.0f, 1.0f, 0.80f, 0.96f));

            if (clickFlash > 0.0f)
            {
                float flashRadius = 9.0f + (1.0f - clickFlash) * 24.0f;
                using (Pen flash = new Pen(Hsl(hue + 90.0f, 1.0f, 0.82f, clickFlash * 0.9f),
                    1.0f + clickFlash * 2.5f))
                    graphics.DrawEllipse(flash, x - flashRadius, y - flashRadius,
                        flashRadius * 2.0f, flashRadius * 2.0f);
            }
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

    internal static class NativeMethods
    {
        internal const int WS_EX_LAYERED = 0x00080000;
        internal const int WS_EX_TRANSPARENT = 0x00000020;
        internal const int WS_EX_TOOLWINDOW = 0x00000080;
        internal const int WS_EX_NOACTIVATE = 0x08000000;
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
    }
}
