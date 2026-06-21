using System;
using System.IO;

namespace TimeboxBar.Core
{
    public static class Logger
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TimeboxBar", "timebox.log");

        private static readonly object _lock = new object();

        public static void Init()
        {
            try
            {
                var dir = Path.GetDirectoryName(LogPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                // Letzte 3 Logfiles rotieren (alte umbenennen)
                if (File.Exists(LogPath))
                {
                    var bak = LogPath + ".bak";
                    if (File.Exists(bak)) File.Delete(bak);
                    File.Move(LogPath, bak);
                }
                Log("=== TimeboxBar Start ===");
                Log($"OS: {Environment.OSVersion}");
                Log($"CLR: {Environment.Version}");
                Log($"Screen: {System.Windows.Forms.Screen.PrimaryScreen.Bounds} | DPI: {GetDpi()}");
            }
            catch { /* Logger darf niemals crashen */ }
        }

        public static void Log(string message)
        {
            try
            {
                lock (_lock)
                {
                    File.AppendAllText(LogPath,
                        $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}");
                }
            }
            catch { }
        }

        public static void LogEx(string context, Exception ex)
        {
            Log($"EXCEPTION in {context}: {ex.GetType().Name}: {ex.Message}");
            Log($"  Stack: {ex.StackTrace?.Replace(Environment.NewLine, " | ")}");
            if (ex.InnerException != null)
                Log($"  Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
        }

        private static string GetDpi()
        {
            try
            {
                using (var g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
                    return $"{g.DpiX}x{g.DpiY}";
            }
            catch { return "unknown"; }
        }
    }
}
