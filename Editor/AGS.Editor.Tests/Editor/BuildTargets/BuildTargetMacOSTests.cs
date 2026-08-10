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

            Assert.That(names, Has.Some.EqualTo(System.IO.Path.Combine("AGSGame.xcodeproj", "project.pbxproj")));
            Assert.That(names, Has.Some.EqualTo("AGSGame.xcconfig"));
            Assert.That(names, Has.Some.EqualTo(System.IO.Path.Combine("Frameworks", "AGSKit.xcframework.zip")));
            Assert.That(names, Has.Some.EqualTo(System.IO.Path.Combine("Frameworks", "SDL2.framework.zip")));
        }

        [Test]
        public void GetProjectName_StripsSpacesAndKeepsCase()
        {
            Assert.That(BuildTargetMacOS.GetProjectName("AGS 363 Demo Game"), Is.EqualTo("AGS363DemoGame"));
        }

        [Test]
        public void GetProjectName_DropsPunctuation()
        {
            Assert.That(BuildTargetMacOS.GetProjectName("My Game: The Sequel!"), Is.EqualTo("MyGameTheSequel"));
        }

        [Test]
        public void GetProjectName_FallsBackWhenEmptyOrUnusable()
        {
            Assert.That(BuildTargetMacOS.GetProjectName(""), Is.EqualTo("AGSGame"));
            Assert.That(BuildTargetMacOS.GetProjectName("***"), Is.EqualTo("AGSGame"));
        }
    }
}
