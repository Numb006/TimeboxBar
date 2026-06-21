using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace TimeboxBar.Core
{
    [DataContract]
    public class AppConfig
    {
        public const int    MinBarHeight  = 3;
        public const int    MaxBarHeight  = 100;
        public const double MinOpacity    = 0.1;
        public const double MaxOpacity    = 1.0;

        [DataMember] public int    BarHeight      { get; set; }
        [DataMember] public string Position       { get; set; }
        [DataMember] public double Opacity        { get; set; }
        [DataMember] public uint   HotkeyModifier { get; set; }
        [DataMember] public uint   HotkeyKey      { get; set; }
        [DataMember] public int    QuickStart1    { get; set; }
        [DataMember] public int    QuickStart2    { get; set; }
        [DataMember] public bool   PlaySound      { get; set; }
        [DataMember] public string Language       { get; set; } // "auto", "de", "en"
        [DataMember] public bool   FirstRun       { get; set; }

        public bool IsBottom          => Position == "Bottom";
        public int  ClampedBarHeight  => Math.Max(MinBarHeight, Math.Min(MaxBarHeight, BarHeight));
        public byte OpacityAlpha      => (byte)Math.Round(Math.Max(MinOpacity, Math.Min(MaxOpacity, Opacity)) * 255);

        // Defaults werden vor der Deserialisierung gesetzt, fehlende JSON-Felder behalten sie
        [OnDeserializing]
        private void SetDefaults(StreamingContext ctx) => ApplyDefaults();

        public AppConfig() => ApplyDefaults();

        private void ApplyDefaults()
        {
            BarHeight      = 6;
            Position       = "Top";
            Opacity        = 0.85;
            HotkeyModifier = 0x0006; // MOD_CONTROL | MOD_SHIFT
            HotkeyKey      = 0x20;   // VK_SPACE
            QuickStart1    = 25;
            QuickStart2    = 45;
            PlaySound      = true;
            Language       = "auto";
            FirstRun       = true;
        }

        private static string ConfigPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TimeboxBar", "config.json");

        public static AppConfig Load()
        {
            try
            {
                if (!File.Exists(ConfigPath)) return new AppConfig();
                var bytes = File.ReadAllBytes(ConfigPath);
                using (var ms = new MemoryStream(bytes))
                {
                    var ser = new DataContractJsonSerializer(typeof(AppConfig));
                    return (AppConfig)ser.ReadObject(ms) ?? new AppConfig();
                }
            }
            catch (Exception ex) { Logger.LogEx("AppConfig.Load", ex); return new AppConfig(); }
        }

        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                using (var ms = new MemoryStream())
                {
                    var ser = new DataContractJsonSerializer(typeof(AppConfig));
                    ser.WriteObject(ms, this);
                    File.WriteAllText(ConfigPath, Encoding.UTF8.GetString(ms.ToArray()),
                        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                }
            }
            catch (Exception ex) { Logger.LogEx("AppConfig.Save", ex); }
        }
    }
}
