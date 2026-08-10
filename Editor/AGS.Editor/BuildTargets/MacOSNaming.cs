using System.Text;

namespace AGS.Editor
{
    /// <summary>
    /// Naming shared by the two macOS build targets: the base name for the
    /// exported project folder, .app, and bundle files. Derived from the game's
    /// file name and reduced to ASCII letters and digits so it needs no quoting
    /// in a shell script or an Xcode project. Falls back to the template's own
    /// name when nothing usable is set.
    /// </summary>
    public static class MacOSNaming
    {
        public const string TEMPLATE_BASE = "AGSGame";

        public static string GetProjectName(string baseGameFileName)
        {
            if (string.IsNullOrEmpty(baseGameFileName)) return TEMPLATE_BASE;
            StringBuilder sb = new StringBuilder(baseGameFileName.Length);
            foreach (char c in baseGameFileName)
            {
                if (c < 128 && char.IsLetterOrDigit(c)) sb.Append(c);
            }
            return sb.Length > 0 ? sb.ToString() : TEMPLATE_BASE;
        }
    }
}
