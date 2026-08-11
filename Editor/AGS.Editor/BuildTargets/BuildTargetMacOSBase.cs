using AGS.Types;
using System.Collections.Generic;
using System.IO;

namespace AGS.Editor
{
    /// <summary>
    /// Shared machinery for the two macOS export targets: the Xcode-project
    /// target (<see cref="BuildTargetMacOS"/>) and the prebuilt app-bundle
    /// target (<see cref="BuildTargetMacOSApp"/>). Both copy a template from the
    /// Editor directory, fill a Resources folder with the game data, and carry
    /// the game's identity into a Mac-side config; they differ only in the
    /// template shape and the identity file they emit. Everything common lives
    /// here; subclasses supply the template probe, the copy, and Build().
    /// </summary>
    public abstract class BuildTargetMacOSBase : BuildTargetBase
    {
        public const string MACOS_RESOURCES_DIR = "Resources";

        // The name every file and identifier in the shipped template carries.
        // On export it is swapped for the game's own name (see GetProjectName).
        public const string MACOS_TEMPLATE_BASE = MacOSNaming.TEMPLATE_BASE;

        /// <summary>
        /// The template directory shipped inside the Editor. It shares its name
        /// with the target's output directory.
        /// </summary>
        protected string GetEditorTemplateDir()
        {
            return Path.Combine(Factory.AGSEditor.EditorDirectory, OutputDirectory);
        }

        /// <summary>
        /// The game's name, sanitized for use as a macOS project/app name.
        /// </summary>
        protected string GetProjectName()
        {
            return MacOSNaming.GetProjectName(Factory.AGSEditor.BaseGameFileName);
        }

        /// <summary>
        /// The Resources folder that receives the game data, relative to the
        /// target's output directory. The Xcode target nests it under the
        /// project folder; the app-bundle target keeps it at the root.
        /// </summary>
        protected virtual string GetResourcesRelativePath()
        {
            return MACOS_RESOURCES_DIR;
        }

        public override string[] GetPlatformStandardSubfolders()
        {
            return new string[] { GetCompiledPath(GetResourcesRelativePath()) };
        }

        public override void DeleteMainGameData(string name, CompileMessages errors)
        {
            string resourcesDir = Path.Combine(OutputDirectoryFullPath, GetResourcesRelativePath());
            DeleteCommonGameFiles(resourcesDir, name, errors);
        }

        /// <summary>
        /// Copies the compiled game data into the given Resources directory and
        /// regenerates acsetup.cfg there. Editor-only files (.dll, winsetup.exe,
        /// the source acsetup.cfg) are skipped; the config is rewritten from the
        /// game's current parameters afterwards.
        /// </summary>
        protected void CopyGameData(string resourcesDir)
        {
            if (!Directory.Exists(Utilities.ResolveSourcePath(resourcesDir)))
                Directory.CreateDirectory(Utilities.ResolveSourcePath(resourcesDir));

            foreach (string fileName in Directory.GetFiles(Path.Combine(AGSEditor.OUTPUT_DIRECTORY, AGSEditor.DATA_OUTPUT_DIRECTORY)))
            {
                if ((File.GetAttributes(fileName) & (FileAttributes.Hidden | FileAttributes.System | FileAttributes.Temporary)) != 0)
                    continue;
                if ((!fileName.EndsWith(".dll", System.StringComparison.OrdinalIgnoreCase)) &&
                    (!Path.GetFileName(fileName).Equals("winsetup.exe", System.StringComparison.OrdinalIgnoreCase)) &&
                    (!Path.GetFileName(fileName).Equals(AGSEditor.CONFIG_FILE_NAME, System.StringComparison.OrdinalIgnoreCase)))
                {
                    Utilities.HardlinkOrCopy(Path.Combine(resourcesDir, Path.GetFileName(fileName)), fileName, true);
                }
            }

            // Regenerate acsetup.cfg next to the game data, with current parameters.
            GenerateConfigFile(resourcesDir);
        }

        /// <summary>
        /// The per-plugin warning shown when a game has plugins: neither macOS
        /// target can build a Windows plugin, and each offers a different route.
        /// </summary>
        protected abstract string GetPluginWarning(Plugin plugin);

        protected void WarnAboutPlugins(CompileMessages errors)
        {
            foreach (Plugin plugin in Factory.AGSEditor.CurrentGame.Plugins)
            {
                errors.Add(new CompileWarning(GetPluginWarning(plugin)));
            }
        }

        public override RuntimeSetup FixInvalidSettings(RuntimeSetup setup)
        {
            setup.GraphicsDriver = setup.GraphicsDriver == GraphicsDriver.D3D9 ? GraphicsDriver.OpenGL : setup.GraphicsDriver;
            return setup;
        }
    }
}
