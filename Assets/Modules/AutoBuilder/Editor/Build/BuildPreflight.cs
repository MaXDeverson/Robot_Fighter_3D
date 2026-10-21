using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.SceneManagement;

namespace Game.AutoBuilder
{
    internal enum PreflightSeverity
    {
        Error,
        Warning,
    }

    internal enum PreflightFix
    {
        None,
        SwitchToAndroid,
        OpenPlayerSettings,
        OpenBuildSettings,
    }

    internal sealed class PreflightIssue
    {
        public readonly PreflightSeverity Severity;
        public readonly string Message;
        public readonly PreflightFix Fix;

        public PreflightIssue(PreflightSeverity severity, string message, PreflightFix fix = PreflightFix.None)
        {
            Severity = severity;
            Message = message;
            Fix = fix;
        }
    }

    /// <summary>
    /// What is checked before a build starts. An error stops the build; a warning is shown and the build goes on.
    /// </summary>
    internal static class BuildPreflight
    {
        public static List<PreflightIssue> Run(AabBuildRequest request, GitInfo git, AabBuildHistory history)
        {
            var issues = new List<PreflightIssue>();
            bool release = request.Kind == AabBuildKind.Release;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                issues.Add(Error("Exit Play Mode to build."));

            if (EditorApplication.isCompiling)
                issues.Add(Error("Scripts are compiling."));

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                issues.Add(Error($"The active platform is {EditorUserBuildSettings.activeBuildTarget}, not Android.",
                    PreflightFix.SwitchToAndroid));

            if (!AppVersion.TryParse(PlayerSettings.bundleVersion, out _))
                issues.Add(Error($"Version '{PlayerSettings.bundleVersion}' is not major.minor.patch.",
                    PreflightFix.OpenPlayerSettings));

            if (BuildScenes().Length == 0)
                issues.Add(Error("No scene is enabled in the build list.", PreflightFix.OpenBuildSettings));

            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP)
                issues.Add(Issue(release, "The scripting backend is not IL2CPP — Google Play needs 64-bit, which needs IL2CPP.",
                    PreflightFix.OpenPlayerSettings));

            if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0)
                issues.Add(Issue(release, "ARM64 is not among the target architectures — Google Play requires it.",
                    PreflightFix.OpenPlayerSettings));

            CheckSigning(request, issues);

            if (Enumerable.Range(0, EditorSceneManager.sceneCount)
                .Any(i => EditorSceneManager.GetSceneAt(i).isDirty))
                issues.Add(Warning("An open scene has unsaved changes — the build takes scenes from disk. " +
                                   "You will be asked to save."));

            if (git.IsAvailable && git.ChangedFiles > 0)
                issues.Add(Warning($"{git.ChangedFiles} uncommitted change(s): the build will not match commit {git.Commit}."));

            AabBuildRecord last = history.LastSucceeded();
            if (last != null && last.VersionCode >= PlayerSettings.Android.bundleVersionCode)
                issues.Add(Warning($"Version code {last.VersionCode} was already built on {last.StartedAt}; " +
                                   $"Google Play rejects a code that is not higher than an uploaded one.",
                    PreflightFix.OpenPlayerSettings));

            if (!release)
                issues.Add(Warning("Development build: for our phones only, do not upload it to Google Play."));

            return issues;
        }

        public static bool HasErrors(IEnumerable<PreflightIssue> issues) =>
            issues.Any(issue => issue.Severity == PreflightSeverity.Error);

        public static string[] BuildScenes() =>
            EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();

        /// <summary>
        /// The keystore as Player Settings names it. A keystore in the dedicated location (Preferences > External Tools)
        /// is stored as <c>{dedicated}: name</c> and resolved against that folder, but
        /// <see cref="PlayerSettings.Android.keystoreName"/> drops the prefix, so it is read from the asset itself.
        /// Any other relative name is resolved against the project root.
        /// </summary>
        public static string KeystorePath()
        {
            string name = PlayerSettings.Android.keystoreName;
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            string root = RawKeystoreName().StartsWith(DedicatedKeystorePrefix, StringComparison.Ordinal)
                ? AndroidExternalToolsSettings.keystoresDedicatedLocation
                : AutoBuilderPaths.ProjectRoot;
            return Path.GetFullPath(Path.Combine(root, name));
        }

        private const string DedicatedKeystorePrefix = "{dedicated}:";

        private static string RawKeystoreName()
        {
            PlayerSettings settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")
                .OfType<PlayerSettings>().FirstOrDefault();
            return settings == null
                ? string.Empty
                : new SerializedObject(settings).FindProperty("AndroidKeystoreName")?.stringValue ?? string.Empty;
        }

        private static void CheckSigning(AabBuildRequest request, List<PreflightIssue> issues)
        {
            bool release = request.Kind == AabBuildKind.Release;

            if (!PlayerSettings.Android.useCustomKeystore)
            {
                issues.Add(Issue(release, "Signed with Unity's debug key: Google Play accepts only the upload key " +
                                          "(Player Settings > Publishing Settings > Custom Keystore).",
                    PreflightFix.OpenPlayerSettings));
                return;
            }

            string keystore = KeystorePath();
            if (string.IsNullOrEmpty(keystore) || !File.Exists(keystore))
                issues.Add(Error($"Keystore not found: {(string.IsNullOrEmpty(keystore) ? "none is chosen" : keystore)}",
                    PreflightFix.OpenPlayerSettings));

            if (string.IsNullOrEmpty(PlayerSettings.Android.keyaliasName))
                issues.Add(Error("No key alias is chosen in Publishing Settings.", PreflightFix.OpenPlayerSettings));

            if (string.IsNullOrEmpty(request.KeystorePass))
                issues.Add(Error("Enter the keystore password."));

            if (string.IsNullOrEmpty(request.KeyaliasPass))
                issues.Add(Error("Enter the key alias password."));
        }

        /// <summary>An error for release builds, a warning for development ones.</summary>
        private static PreflightIssue Issue(bool release, string message, PreflightFix fix) =>
            new PreflightIssue(release ? PreflightSeverity.Error : PreflightSeverity.Warning, message, fix);

        private static PreflightIssue Error(string message, PreflightFix fix = PreflightFix.None) =>
            new PreflightIssue(PreflightSeverity.Error, message, fix);

        private static PreflightIssue Warning(string message, PreflightFix fix = PreflightFix.None) =>
            new PreflightIssue(PreflightSeverity.Warning, message, fix);
    }
}
