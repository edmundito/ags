using AGS.Types;
using NUnit.Framework;
using System;

namespace AGS.Editor.Types
{
    [TestFixture]
    public class SettingsTests
    {
        [Test]
        public void MacOSBundleIdentifier_NormalizesCaseAndSpaces()
        {
            Settings settings = new Settings();
            settings.MacOSBundleIdentifier = "Com.MyStudio. MyGame";
            Assert.That(settings.MacOSBundleIdentifier, Is.EqualTo("com.mystudio.mygame"));
        }

        [Test]
        public void MacOSBundleIdentifier_RejectsIllegalCharacters()
        {
            Settings settings = new Settings();
            Assert.Throws<ArgumentException>(() => settings.MacOSBundleIdentifier = "com.my-studio/mygame");
        }

        [Test]
        public void MacOSBundleIdentifier_RejectsEmpty()
        {
            Settings settings = new Settings();
            Assert.Throws<ArgumentException>(() => settings.MacOSBundleIdentifier = "");
        }

        [Test]
        public void MacOSBundleIdentifier_DefaultsToPlaceholder()
        {
            Settings settings = new Settings();
            Assert.That(settings.MacOSBundleIdentifier, Is.EqualTo("com.mystudio.mygame"));
        }

        [Test]
        public void MacOSAppVersion_DefaultsToOnePointZero()
        {
            Settings settings = new Settings();
            Assert.That(settings.MacOSAppVersion, Is.EqualTo("1.0"));
        }

        [Test]
        public void MacOSAppVersion_RejectsEmpty()
        {
            Settings settings = new Settings();
            Assert.Throws<ArgumentException>(() => settings.MacOSAppVersion = "");
        }

        [Test]
        public void MacOSAppVersion_TrimsWhitespace()
        {
            Settings settings = new Settings();
            settings.MacOSAppVersion = "  2.3.1  ";
            Assert.That(settings.MacOSAppVersion, Is.EqualTo("2.3.1"));
        }
    }
}
