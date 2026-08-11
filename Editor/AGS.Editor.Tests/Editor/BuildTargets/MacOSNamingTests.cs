using AGS.Editor;
using NUnit.Framework;

namespace AGS.Editor.BuildTargets
{
    [TestFixture]
    public class MacOSNamingTests
    {
        [Test]
        public void GetProjectName_StripsSpacesAndKeepsCase()
        {
            Assert.That(MacOSNaming.GetProjectName("AGS 363 Demo Game"), Is.EqualTo("AGS363DemoGame"));
        }

        [Test]
        public void GetProjectName_DropsPunctuation()
        {
            Assert.That(MacOSNaming.GetProjectName("My Game: The Sequel!"), Is.EqualTo("MyGameTheSequel"));
        }

        [Test]
        public void GetProjectName_FallsBackWhenEmptyOrUnusable()
        {
            Assert.That(MacOSNaming.GetProjectName(""), Is.EqualTo("AGSGame"));
            Assert.That(MacOSNaming.GetProjectName("***"), Is.EqualTo("AGSGame"));
        }
    }
}
