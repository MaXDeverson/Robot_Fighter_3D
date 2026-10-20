using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Game.AutoBuilder
{
    internal static class AutoBuilderPaths
    {
        public const string DefaultOutputDirectory = "build/aab";

        public static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        /// <summary>A folder from the settings, relative to the project root unless it is absolute.</summary>
        public static string ResolveDirectory(string directory) =>
            Path.GetFullPath(Path.Combine(ProjectRoot,
                string.IsNullOrWhiteSpace(directory) ? DefaultOutputDirectory : directory.Trim()));

        /// <summary>
        /// <c>PortalQuest_v0.0.2_c3_2026-10-03_1530.aab</c>, with <c>_dev</c> after the code for development builds —
        /// every build gets its own file, and the name alone says which upload it is.
        /// </summary>
        public static string AabFileName(string productName, string version, int versionCode, AabBuildKind kind,
            DateTime time)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}_v{1}_c{2}{3}_{4:yyyy-MM-dd_HHmm}.aab",
                FileSafe(productName), FileSafe(version), versionCode,
                kind == AabBuildKind.Development ? "_dev" : string.Empty, time);
        }

        private static string FileSafe(string text)
        {
            var safe = new StringBuilder();
            foreach (char c in text ?? string.Empty)
            {
                if (char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_')
                    safe.Append(c);
            }

            return safe.Length > 0 ? safe.ToString() : "Game";
        }
    }
}
