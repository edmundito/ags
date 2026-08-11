using AGS.Types;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AGS.Editor
{
    /// <summary>
    /// Exports the game as a prebuilt, unsigned macOS .app plus a Mac-side
    /// make-app.sh. The engine executable is identical for every game, so the
    /// .app is built once on CI and shipped zipped (its internal symlinks cannot
    /// survive assembly on Windows). This target stays pure file-copy: it drops
    /// the template, fills a flat Resources/, and writes game.env. make-app.sh
    /// does the rest on the Mac (unpack, inject, patch identity, sign, notarize).
    /// </summary>
    public class BuildTargetMacOSApp : BuildTargetMacOSBase
    {
        public const string MACOS_APP_DIR = "macOS";

        /// <summary>
        /// The identity file make-app.sh sources on the Mac. Written with UNIX
        /// line endings; only ever read on macOS.
        /// </summary>
        public static string BuildGameEnvText(string gameName, string appName, string bundleId, string version)
        {
            string text =
@"# Written by the AGS Editor. Rebuilding the game overwrites this file.
# make-app.sh reads these; put your signing identity in make-app.sh (or signing.env).
GAME_NAME=""" + gameName + @"""
APP_NAME=""" + appName + @"""
BUNDLE_ID=""" + bundleId + @"""
APP_VERSION=""" + version + @"""
";
            return text.Replace("\r\n", "\n");
        }

        /// <summary>
        /// Presence probe for IsAvailable and the "missing file" error. The whole
        /// template is copied in Build(); this only lists a few must-exist files.
        /// The prebuilt app ships zipped because its internal symlinks do not
        /// survive assembly on Windows, so the probe looks for the archive.
        /// </summary>
        public override IDictionary<string, string> GetRequiredLibraryPaths()
        {
            Dictionary<string, string> paths = new Dictionary<string, string>();
            string templateDir = GetEditorTemplateDir();
            string[] probes = { "AGSGame.app.zip", "make-app.sh", "AGSGame.entitlements" };
            foreach (string probe in probes) paths.Add(probe, templateDir);
            return paths;
        }

        /// <summary>
        /// Copies the whole template (zipped app, entitlements, scripts, README)
        /// into the compiled output. The template has no Resources placeholder,
        /// so everything is copied.
        /// </summary>
        private void CopyTemplate(string templateDir)
        {
            foreach (string sourceFile in Directory.GetFiles(templateDir, "*", SearchOption.AllDirectories))
            {
                string relative = sourceFile.Substring(templateDir.Length).TrimStart(Path.DirectorySeparatorChar);
                string destFile = GetCompiledPath(relative);
                string destDir = Path.GetDirectoryName(Utilities.ResolveSourcePath(destFile));
                if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
                File.Copy(Utilities.ResolveSourcePath(sourceFile), Utilities.ResolveSourcePath(destFile), true);
            }
        }

        protected override string GetPluginWarning(Plugin plugin)
        {
            return "macOS: plugin " + plugin.FileName +
                " has no macOS build. Place a lib<name>.dylib next to make-app.sh and it " +
                "will be copied into the app bundle and signed with your identity.";
        }

        public override bool Build(CompileMessages errors, bool forceRebuild)
        {
            if (!base.Build(errors, forceRebuild)) return false;
            WarnAboutPlugins(errors);

            CopyTemplate(GetEditorTemplateDir());

            // Name the shipped app archive after the game so the output folder
            // reads as the game's deliverable. The bundle inside stays
            // AGSGame.app; make-app.sh renames it to <APP_NAME>.app on the Mac.
            string projectName = GetProjectName();
            if (projectName != MACOS_TEMPLATE_BASE)
            {
                string oldZip = Utilities.ResolveSourcePath(GetCompiledPath("AGSGame.app.zip"));
                string newZip = Utilities.ResolveSourcePath(GetCompiledPath(projectName + ".app.zip"));
                if (File.Exists(oldZip))
                {
                    if (File.Exists(newZip)) File.Delete(newZip);
                    File.Move(oldZip, newZip);
                }
            }

            CopyGameData(GetCompiledPath(MACOS_RESOURCES_DIR));

            Settings settings = Factory.AGSEditor.CurrentGame.Settings;
            string gameEnv = BuildGameEnvText(settings.GameName, projectName,
                settings.MacOSBundleIdentifier, settings.MacOSAppVersion);
            string gameEnvPath = Utilities.ResolveSourcePath(GetCompiledPath("game.env"));
            File.WriteAllBytes(gameEnvPath, Encoding.UTF8.GetBytes(gameEnv));

            errors.Add(new CompileWarning("macOS: app written to " + GetCompiledPath() +
                ". Copy the folder to a Mac and run: sh make-app.sh"));
            return true;
        }

        public override string Name
        {
            get { return MACOS_APP_DIR; }
        }

        public override string OutputDirectory
        {
            get { return MACOS_APP_DIR; }
        }
    }
}
