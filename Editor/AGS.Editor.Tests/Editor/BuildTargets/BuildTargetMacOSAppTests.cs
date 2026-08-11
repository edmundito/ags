using AGS.Editor;
using NUnit.Framework;

namespace AGS.Editor.BuildTargets
{
    [TestFixture]
    public class BuildTargetMacOSAppTests
    {
        [Test]
        public void GameEnv_ContainsIdentity()
        {
            string env = BuildTargetMacOSApp.BuildGameEnvText(
                "My Game", "MyGame", "com.mystudio.mygame", "2.3");

            Assert.That(env, Does.Contain("GAME_NAME=\"My Game\""));
            Assert.That(env, Does.Contain("APP_NAME=\"MyGame\""));
            Assert.That(env, Does.Contain("BUNDLE_ID=\"com.mystudio.mygame\""));
            Assert.That(env, Does.Contain("APP_VERSION=\"2.3\""));
        }

        [Test]
        public void GameEnv_UsesUnixLineEndings()
        {
            string env = BuildTargetMacOSApp.BuildGameEnvText("My Game", "MyGame", "com.x.y", "1.0");

            Assert.That(env, Does.Not.Contain("\r\n"));
        }

        [Test]
        public void NameAndOutputDirectory_AreMacOS()
        {
            BuildTargetMacOSApp target = new BuildTargetMacOSApp();

            Assert.That(target.Name, Is.EqualTo("macOS"));
            Assert.That(target.OutputDirectory, Is.EqualTo("macOS"));
        }

        [Test]
        public void RequiredLibraryNames_IncludeAppZipAndSignScript()
        {
            BuildTargetMacOSApp target = new BuildTargetMacOSApp();
            string[] names = target.GetRequiredLibraryNames();

            Assert.That(names, Has.Some.EqualTo("AGSGame.app.zip"));
            Assert.That(names, Has.Some.EqualTo("make-app.sh"));
            Assert.That(names, Has.Some.EqualTo("AGSGame.entitlements"));
        }
    }
}
