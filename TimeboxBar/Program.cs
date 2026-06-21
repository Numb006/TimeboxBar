using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using TimeboxBar.Core;
using TimeboxBar.UI;

namespace TimeboxBar
{
    static class Program
    {
        [DllImport("user32.dll")] static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr w, IntPtr l);
        [DllImport("user32.dll")] static extern int RegisterWindowMessage(string msg);

        public static readonly int WM_SHOW_INSTANCE = RegisterWindowMessage("TimeboxBar_ShowInstance");

        [STAThread]
        static void Main()
        {
            Logger.Init();
            Logger.Log("Main() entered");

            bool createdNew;
            using (var mutex = new Mutex(true, "TimeboxBar_SingleInstance", out createdNew))
            {
                Logger.Log($"Mutex acquired, createdNew={createdNew}");

                if (!createdNew)
                {
                    Logger.Log("Second instance — broadcasting WM_SHOW_INSTANCE");
                    PostMessage(new IntPtr(0xFFFF), WM_SHOW_INSTANCE, IntPtr.Zero, IntPtr.Zero);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Logger.Log("Visual styles enabled");

                Application.ThreadException += (s, e) =>
                {
                    Logger.LogEx("ThreadException", e.Exception);
                    MessageBox.Show("Fehler: " + e.Exception.Message + "\n\n" + e.Exception.StackTrace,
                        "TimeboxBar", MessageBoxButtons.OK, MessageBoxIcon.Error);
                };
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    var ex = e.ExceptionObject as Exception;
                    Logger.Log($"UnhandledException: {e.ExceptionObject}");
                    MessageBox.Show("Schwerer Fehler: " + e.ExceptionObject,
                        "TimeboxBar", MessageBoxButtons.OK, MessageBoxIcon.Error);
                };

                try
                {
                    Logger.Log("Loading AppConfig...");
                    var config = AppConfig.Load();
                    Logger.Log($"Config loaded: Height={config.BarHeight}, Pos={config.Position}, Opacity={config.Opacity}");

                    Logger.Log("Creating TimeboxTimer...");
                    var timer = new TimeboxTimer();

                    Logger.Log("Creating HotkeyManager...");
                    var hotkey = new HotkeyManager();

                    Logger.Log("Creating BarForm...");
                    var form = new BarForm(config, timer, hotkey);
                    Logger.Log("BarForm created — calling Application.Run()");

                    Application.Run(form);
                    Logger.Log("Application.Run() returned — app exiting");
                }
                catch (Exception ex)
                {
                    Logger.LogEx("Main startup", ex);
                    MessageBox.Show("Startfehler: " + ex.Message + "\n\n" + ex.StackTrace,
                        "TimeboxBar", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            Logger.Log("Main() exiting");
        }
    }
}
