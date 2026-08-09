using AGS.Editor;
using NUnit.Framework;

namespace AGS.Editor.BuildTargets
{
    [TestFixture]
    public class BuildTargetMacOSTests
    {
        [Test]
        public void XCConfig_ContainsGameIdentity()
        {
            string config = BuildTargetMacOS.BuildXCConfigText("My Game", "com.mystudio.mygame", "2.3");

            Assert.That(config, Does.Contain("PRODUCT_NAME = My Game"));
            Assert.That(config, Does.Contain("PRODUCT_BUNDLE_IDENTIFIER = com.mystudio.mygame"));
            Assert.That(config, Does.Contain("MARKETING_VERSION = 2.3"));
            Assert.That(config, Does.Contain("DEVELOPMENT_TEAM ="));
        }

        [Test]
        public void XCConfig_UsesUnixLineEndings()
        {
            string config = BuildTargetMacOS.BuildXCConfigText("My Game", "com.mystudio.mygame", "1.0");

            Assert.That(config, Does.Not.Contain("\r\n"));
        }

        [Test]
        public void NameAndOutputDirectory_AreMacOS()
        {
            BuildTargetMacOS target = new BuildTargetMacOS();

            Assert.That(target.Name, Is.EqualTo("macOS"));
            Assert.That(target.OutputDirectory, Is.EqualTo("macOS"));
        }

        [Test]
        public void RequiredLibraryNames_IncludeProjectAndFrameworks()
        {
            BuildTargetMacOS target = new BuildTargetMacOS();
            string[] names = target.GetRequiredLibraryNames();

            Assert.That(names, Has.Some.EqualTo(System.IO.Path.Combine("mygame.xcodeproj", "project.pbxproj")));
            Assert.That(names, Has.Some.EqualTo("mygame.xcconfig"));
            Assert.That(names, Has.Some.EqualTo(System.IO.Path.Combine("Frameworks", "AGSKit.xcframework", "Info.plist")));
            Assert.That(names, Has.Some.EqualTo(System.IO.Path.Combine("Frameworks", "SDL2.framework", "SDL2")));
        }
    }
}
