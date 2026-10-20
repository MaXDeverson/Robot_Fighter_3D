using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;

namespace Game.AutoBuilder
{
    /// <summary>
    /// Sets the game version and the Android version code, in memory and in ProjectSettings/ProjectSettings.asset.
    /// </summary>
    /// <remarks>
    /// Unity has no call that saves only the player settings: <c>AssetDatabase.SaveAssetIfDirty</c> skips them and
    /// <c>AssetDatabase.SaveAssets</c> would also save every other unsaved asset in the editor. So the two lines are
    /// rewritten in the file directly, next to the same values set through <see cref="PlayerSettings"/> — memory and disk
    /// agree, and the diff is exactly those two lines.
    /// </remarks>
    internal static class ProjectSettingsVersionFile
    {
        public const string FilePath = "ProjectSettings/ProjectSettings.asset";

        private static readonly Regex VersionLine = new Regex(@"^(  bundleVersion: )[^\r\n]*", RegexOptions.Multiline);
        private static readonly Regex CodeLine = new Regex(@"^(  AndroidBundleVersionCode: )[^\r\n]*", RegexOptions.Multiline);

        public static void Write(string version, int versionCode)
        {
            PlayerSettings.bundleVersion = version;
            PlayerSettings.Android.bundleVersionCode = versionCode;

            string path = Path.Combine(AutoBuilderPaths.ProjectRoot, FilePath);
            string yaml = File.ReadAllText(path);
            File.WriteAllText(path, Apply(yaml, version, versionCode), new UTF8Encoding(false));
        }

        /// <summary>The settings file's text with both lines replaced; throws if either line is not there exactly once.</summary>
        public static string Apply(string yaml, string version, int versionCode)
        {
            if (string.IsNullOrWhiteSpace(version) || version.IndexOfAny(new[] { '\r', '\n', ':', '#' }) >= 0)
                throw new ArgumentException($"'{version}' cannot be written as a version.", nameof(version));

            yaml = ReplaceOnce(yaml, VersionLine, version, "bundleVersion");
            return ReplaceOnce(yaml, CodeLine, versionCode.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "AndroidBundleVersionCode");
        }

        private static string ReplaceOnce(string yaml, Regex line, string value, string key)
        {
            int count = line.Matches(yaml).Count;
            if (count != 1)
                throw new InvalidOperationException($"{FilePath} has {count} '{key}' lines, expected one.");

            return line.Replace(yaml, match => match.Groups[1].Value + value);
        }
    }
}
