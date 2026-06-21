using System.Globalization;
using System.Resources;

namespace TimeboxBar.Core
{
    public static class Strings
    {
        private static readonly ResourceManager _rm = new ResourceManager(
            "TimeboxBar.Properties.Resources",
            typeof(Strings).Assembly);

        private static string Get(string key) =>
            _rm.GetString(key, CultureInfo.CurrentUICulture) ?? key;

        // Tray menu
        public static string MenuCustomTime        => Get("MenuCustomTime");
        public static string MenuPause             => Get("MenuPause");
        public static string MenuResume            => Get("MenuResume");
        public static string MenuStop              => Get("MenuStop");
        public static string MenuSettings          => Get("MenuSettings");
        public static string MenuExit              => Get("MenuExit");
        public static string PositionTop           => Get("PositionTop");
        public static string PositionBottom        => Get("PositionBottom");

        // Tray tooltip
        public static string TrayRunning(string remaining)  => string.Format(Get("TrayRunning"), remaining);
        public static string TrayPaused(string remaining)   => string.Format(Get("TrayPaused"), remaining);

        // Menu items with params
        public static string MenuQuickStart(int minutes)                       => string.Format(Get("MenuQuickStart"), minutes);
        public static string MenuQuickStartReplace(int minutes, string rem)    => string.Format(Get("MenuQuickStartReplace"), minutes, rem);

        // Notifications
        public static string AlreadyRunning        => Get("AlreadyRunning");
        public static string TimerCompleted        => Get("TimerCompleted");
        public static string HotkeyConflict        => Get("HotkeyConflict");
        public static string HotkeyConflictShort   => Get("HotkeyConflictShort");

        // Dialogs
        public static string TimerRunningRestart(string rem, int minutes) => string.Format(Get("TimerRunningRestart"), rem, minutes);
        public static string ExitConfirm(string remaining)                => string.Format(Get("ExitConfirm"), remaining);

        // Settings form
        public static string SettingsTitle         => Get("SettingsTitle");
        public static string LabelBarHeight        => Get("LabelBarHeight");
        public static string LabelPosition         => Get("LabelPosition");
        public static string LabelOpacity          => Get("LabelOpacity");
        public static string LabelQuickStart1      => Get("LabelQuickStart1");
        public static string LabelQuickStart2      => Get("LabelQuickStart2");
        public static string LabelHotkey           => Get("LabelHotkey");
        public static string LabelSound            => Get("LabelSound");
        public static string HotkeyPrompt          => Get("HotkeyPrompt");

        // Custom time dialog
        public static string CustomTimeTitle       => Get("CustomTimeTitle");
        public static string LabelMinutes          => Get("LabelMinutes");
        public static string ButtonStart           => Get("ButtonStart");

        // Language selector
        public static string LabelLanguage         => Get("LabelLanguage");
        public static string LangAuto              => Get("LangAuto");
        public static string LangEnglish           => Get("LangEnglish");
        public static string LangGerman            => Get("LangGerman");
    }
}
