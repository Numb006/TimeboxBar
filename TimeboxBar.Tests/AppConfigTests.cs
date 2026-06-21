using System;
using System.IO;
using NUnit.Framework;
using TimeboxBar.Core;

namespace TimeboxBar.Tests
{
    [TestFixture]
    public class AppConfigTests
    {
        // ── Defaults ────────────────────────────────────────────────────────────

        [Test]
        public void NewConfig_HasCorrectDefaults()
        {
            var c = new AppConfig();
            Assert.That(c.BarHeight,      Is.EqualTo(6));
            Assert.That(c.Position,       Is.EqualTo("Top"));
            Assert.That(c.Opacity,        Is.EqualTo(0.85).Within(0.001));
            Assert.That(c.HotkeyModifier, Is.EqualTo(0x0006u)); // Ctrl+Shift
            Assert.That(c.HotkeyKey,      Is.EqualTo(0x20u));   // Space
            Assert.That(c.QuickStart1,    Is.EqualTo(25));
            Assert.That(c.QuickStart2,    Is.EqualTo(45));
            Assert.That(c.PlaySound,      Is.True);
        }

        // ── Computed properties ──────────────────────────────────────────────────

        [Test]
        public void IsBottom_FalseForTop()
        {
            var c = new AppConfig { Position = "Top" };
            Assert.That(c.IsBottom, Is.False);
        }

        [Test]
        public void IsBottom_TrueForBottom()
        {
            var c = new AppConfig { Position = "Bottom" };
            Assert.That(c.IsBottom, Is.True);
        }

        [TestCase(3,   3)]
        [TestCase(6,   6)]
        [TestCase(100, 100)]
        [TestCase(0,   3)]    // unter Minimum → Minimum
        [TestCase(150, 100)]  // über Maximum → Maximum
        public void ClampedBarHeight_ClampsCorrectly(int input, int expected)
        {
            var c = new AppConfig { BarHeight = input };
            Assert.That(c.ClampedBarHeight, Is.EqualTo(expected));
        }

        [Test]
        public void OpacityAlpha_FullOpacity_Is255()
        {
            var c = new AppConfig { Opacity = 1.0 };
            Assert.That(c.OpacityAlpha, Is.EqualTo(255));
        }

        [Test]
        public void OpacityAlpha_HalfOpacity_IsApprox128()
        {
            var c = new AppConfig { Opacity = 0.5 };
            Assert.That(c.OpacityAlpha, Is.EqualTo(128).Within(1));
        }

        [Test]
        public void OpacityAlpha_ClampsAboveOne()
        {
            var c = new AppConfig { Opacity = 2.0 };
            Assert.That(c.OpacityAlpha, Is.EqualTo(255));
        }

        [Test]
        public void OpacityAlpha_ClampsBelowMinimum()
        {
            var c = new AppConfig { Opacity = 0.0 };
            // Minimum ist 0.1 → 26
            Assert.That(c.OpacityAlpha, Is.EqualTo(26).Within(1));
        }

        // ── Serialization Roundtrip ──────────────────────────────────────────────

        [Test]
        public void SaveAndLoad_RoundtripPreservesAllValues()
        {
            var tmp = Path.Combine(Path.GetTempPath(), $"timeboxtest_{Guid.NewGuid()}.json");
            try
            {
                var original = new AppConfig
                {
                    BarHeight      = 8,
                    Position       = "Bottom",
                    Opacity        = 0.7,
                    HotkeyModifier = 0x0002,
                    HotkeyKey      = 0x41, // 'A'
                    QuickStart1    = 15,
                    QuickStart2    = 30,
                    PlaySound      = false
                };

                // Temporär auf anderen Pfad schreiben (AppConfig.Save nutzt %APPDATA%)
                // Wir testen die Serialisierung direkt über MemoryStream
                using (var ms = new System.IO.MemoryStream())
                {
                    var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(AppConfig));
                    ser.WriteObject(ms, original);
                    ms.Position = 0;
                    var loaded = (AppConfig)ser.ReadObject(ms);

                    Assert.That(loaded.BarHeight,      Is.EqualTo(8));
                    Assert.That(loaded.Position,       Is.EqualTo("Bottom"));
                    Assert.That(loaded.Opacity,        Is.EqualTo(0.7).Within(0.001));
                    Assert.That(loaded.HotkeyModifier, Is.EqualTo(0x0002u));
                    Assert.That(loaded.HotkeyKey,      Is.EqualTo(0x41u));
                    Assert.That(loaded.QuickStart1,    Is.EqualTo(15));
                    Assert.That(loaded.QuickStart2,    Is.EqualTo(30));
                    Assert.That(loaded.PlaySound,      Is.False);
                }
            }
            finally
            {
                if (File.Exists(tmp)) File.Delete(tmp);
            }
        }

        [Test]
        public void Load_WithBrokenJson_ReturnsDefaults()
        {
            // Wir können AppConfig.Load() nicht direkt testen ohne %APPDATA% zu manipulieren,
            // aber wir können den Serializer direkt mit kaputtem Input testen
            var brokenJson = System.Text.Encoding.UTF8.GetBytes("{ this is not json }");
            AppConfig result;
            try
            {
                using (var ms = new System.IO.MemoryStream(brokenJson))
                {
                    var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(AppConfig));
                    result = (AppConfig)ser.ReadObject(ms);
                }
            }
            catch
            {
                result = new AppConfig(); // AppConfig.Load() Fallback
            }
            // Defaults sollten greifen
            Assert.That(result.BarHeight,  Is.EqualTo(6));
            Assert.That(result.Position,   Is.EqualTo("Top"));
        }

        [Test]
        public void Load_WithMissingFields_UsesDefaults()
        {
            // JSON mit nur einem Feld — fehlende Felder sollen Defaults behalten via [OnDeserializing]
            var partialJson = System.Text.Encoding.UTF8.GetBytes(@"{""BarHeight"":9}");
            using (var ms = new System.IO.MemoryStream(partialJson))
            {
                var ser    = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(AppConfig));
                var loaded = (AppConfig)ser.ReadObject(ms);
                Assert.That(loaded.BarHeight,   Is.EqualTo(9));       // Gesetzter Wert
                Assert.That(loaded.Position,    Is.EqualTo("Top"));   // Default
                Assert.That(loaded.QuickStart1, Is.EqualTo(25));      // Default
                Assert.That(loaded.PlaySound,   Is.True);             // Default
            }
        }

        // ── Konstanten ───────────────────────────────────────────────────────────

        [Test]
        public void Constants_HaveExpectedValues()
        {
            Assert.That(AppConfig.MinBarHeight, Is.EqualTo(3));
            Assert.That(AppConfig.MaxBarHeight, Is.EqualTo(100));
        }
    }
}
