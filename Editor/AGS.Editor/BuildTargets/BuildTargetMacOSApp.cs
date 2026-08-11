using AGS.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AGS.Editor
{
    /// <summary>
    /// Exports the game as a prebuilt, unsigned macOS .app plus a Mac-side
    /// sign.sh. The engine executable is identical for every game, so the .app
    /// is built once on CI and shipped zipped (its internal symlinks cannot
    /// survive assembly on Windows). This target stays pure file-copy: it drops
    /// the template, fills a flat Resources/, and writes game.env. sign.sh does
    /// the rest on the Mac (unpack, inject, patch identity, sign, notarize).
    /// </summary>
    public class BuildTargetMacOSApp : BuildTargetBase
    {
        public const string MACOS_APP_DIR = "macOS";
        public const string MACOS_APP_RESOURCES_DIR = "Resources";

        private string GetEditorMacOSAppTemplateDir()
        {
            return Path.Combine(Factory.AGSEditor.EditorDirectory, MACOS_APP_DIR);
        }

        private string GetProjectName()
        {
            return MacOSNaming.GetProjectName(Factory.AGSEditor.BaseGameFileName);
        }

        /// <summary>
        /// The identity file sign.sh sources on the Mac. Written with UNIX line
        /// endings; only ever read on macOS.
        /// </summary>
        public static string BuildGameEnvText(string gameName, string appName, string bundleId, string version)
        {
            string text =
@"# Written by the AGS Editor. Rebuilding the game overwrites this file.
# sign.sh reads these; put your signing identity in sign.sh (or signing.env).
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
            string templateDir = GetEditorMacOSAppTemplateDir();
            string[] probes = { "AGSGame.app.zip", "make-app.sh", "AGSGame.entitlements" };
            foreach (string probe in probes) paths.Add(probe, templateDir);
            return paths;
        }

        public override string[] GetPlatformStandardSubfolders()
        {
            return new string[] { GetCompiledPath(MACOS_APP_RESOURCES_DIR) };
        }

        public override void DeleteMainGameData(string name, CompileMessages errors)
        {
            string resourcesDir = Path.Combine(OutputDirectoryFullPath, MACOS_APP_RESOURCES_DIR);
            DeleteCommonGameFiles(resourcesDir, name, errors);
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

        private void WarnAboutPlugins(CompileMessages errors)
        {
            foreach (Plugin plugin in Factory.AGSEditor.CurrentGame.Plugins)
            {
                errors.Add(new CompileWarning("macOS: plugin " + plugin.FileName +
                    " has no macOS build. Place a lib<name>.dylib next to make-app.sh and it " +
                    "will be copied into the app bundle and signed with your identity."));
            }
        }

        public override bool Build(CompileMessages errors, bool forceRebuild)
        {
            if (!base.Build(errors, forceRebuild)) return false;
            WarnAboutPlugins(errors);

            CopyTemplate(GetEditorMacOSAppTemplateDir());

            // Name the shipped app archive after the game so the output folder
            // reads as the game's deliverable. The bundle inside stays
            // AGSGame.app; sign.sh renames it to <APP_NAME>.app on the Mac.
            string projectName = GetProjectName();
            if (projectName != MacOSNaming.TEMPLATE_BASE)
            {
                string oldZip = Utilities.ResolveSourcePath(GetCompiledPath("AGSGame.app.zip"));
                string newZip = Utilities.ResolveSourcePath(GetCompiledPath(projectName + ".app.zip"));
                if (File.Exists(oldZip))
                {
                    if (File.Exists(newZip)) File.Delete(newZip);
                    File.Move(oldZip, newZip);
                }
            }

            string resourcesDir = GetCompiledPath(MACOS_APP_RESOURCES_DIR);
            if (!Directory.Exists(Utilities.ResolveSourcePath(resourcesDir)))
                Directory.CreateDirectory(Utilities.ResolveSourcePath(resourcesDir));

            foreach (string fileName in Directory.GetFiles(Path.Combine(AGSEditor.OUTPUT_DIRECTORY, AGSEditor.DATA_OUTPUT_DIRECTORY)))
            {
                if ((File.GetAttributes(fileName) & (FileAttributes.Hidden | FileAttributes.System | FileAttributes.Temporary)) != 0)
                    continue;
                if ((!fileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) &&
                    (!Path.GetFileName(fileName).Equals("winsetup.exe", StringComparison.OrdinalIgnoreCase)) &&
                    (!Path.GetFileName(fileName).Equals(AGSEditor.CONFIG_FILE_NAME, StringComparison.OrdinalIgnoreCase)))
                {
                    Utilities.HardlinkOrCopy(Path.Combine(resourcesDir, Path.GetFileName(fileName)), fileName, true);
                }
            }

            // Regenerate acsetup.cfg next to the game data, with current parameters.
            GenerateConfigFile(resourcesDir);

            Settings settings = Factory.AGSEditor.CurrentGame.Settings;
            string gameEnv = BuildGameEnvText(settings.GameName, GetProjectName(),
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

        public override RuntimeSetup FixInvalidSettings(RuntimeSetup setup)
        {
            setup.GraphicsDriver = setup.GraphicsDriver == GraphicsDriver.D3D9 ? GraphicsDriver.OpenGL : setup.GraphicsDriver;

            return setup;
        }
    }
}
