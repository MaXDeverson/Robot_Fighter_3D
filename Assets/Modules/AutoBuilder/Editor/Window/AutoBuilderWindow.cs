using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Game.AutoBuilder
{
    /// <summary>Tools > AutoBuilder: build the Android App Bundle, raise the version, keep a history of builds.</summary>
    internal sealed class AutoBuilderWindow : EditorWindow
    {
        private static readonly string[] KindLabels = { "Release", "Development" };

        [SerializeField] private Vector2 _scroll;
        [SerializeField] private bool _showHistory = true;

        private GitInfo _git = GitInfo.Unavailable;
        private List<PreflightIssue> _issues = new List<PreflightIssue>();

        [MenuItem("Tools/AutoBuilder", priority = 0)]
        public static void Open()
        {
            var window = GetWindow<AutoBuilderWindow>();
            window.titleContent = new GUIContent("AutoBuilder", EditorGUIUtility.IconContent("BuildSettings.Android.Small").image);
            window.minSize = new Vector2(420f, 480f);
            window.Show();
        }

        [MainToolbarElement("Project/AutoBuilder", defaultDockPosition = MainToolbarDockPosition.Left)]
        public static MainToolbarElement ToolbarButton()
        {
            var icon = EditorGUIUtility.IconContent("BuildSettings.Android.Small").image as Texture2D;
            return new MainToolbarButton(new MainToolbarContent(icon, "AutoBuilder"), Open);
        }

        private void OnEnable() => RefreshGit();

        private void OnFocus() => RefreshGit();

        private void RefreshGit() => _git = GitInfo.Read();

        private void OnGUI()
        {
            AutoBuilderUserSettings settings = AutoBuilderUserSettings.instance;
            if (Event.current.type == EventType.Layout)
                _issues = BuildPreflight.Run(settings.ToRequest(), _git, AabBuildHistory.instance);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUI.BeginChangeCheck();

            DrawHeader();
            DrawVersion(settings);
            DrawBuildOptions(settings);
            DrawSigning(settings);
            DrawReleaseNotes(settings);

            if (EditorGUI.EndChangeCheck())
                settings.Save();

            DrawChecks();
            DrawBuildButton(settings);
            DrawLastBuild();
            DrawHistory();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField($"{PlayerSettings.productName}  ·  {PlayerSettings.applicationIdentifier}",
                EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                string git = _git.IsAvailable
                    ? $"{_git.Branch} @ {_git.Commit}" + (_git.ChangedFiles > 0 ? $"  ·  {_git.ChangedFiles} uncommitted" : "  ·  clean")
                    : "git is not available";
                EditorGUILayout.LabelField(git, EditorStyles.miniLabel);
                if (GUILayout.Button(EditorGUIUtility.IconContent("Refresh"), EditorStyles.iconButton, GUILayout.Width(20f)))
                    RefreshGit();
            }
        }

        private static void DrawVersion(AutoBuilderUserSettings settings)
        {
            Section("Version");

            string version = PlayerSettings.bundleVersion;
            int code = PlayerSettings.Android.bundleVersionCode;

            using (new EditorGUILayout.HorizontalScope())
            {
                string newVersion = EditorGUILayout.DelayedTextField("Version", version);
                int newCode = EditorGUILayout.DelayedIntField(code, GUILayout.Width(60f));
                GUILayout.Label("code", EditorStyles.miniLabel, GUILayout.Width(30f));

                if (newVersion != version || newCode != code)
                    SetVersion(newVersion.Trim(), Mathf.Max(1, newCode));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Raise now");
                bool parsed = AppVersion.TryParse(version, out AppVersion current);
                using (new EditorGUI.DisabledScope(!parsed))
                {
                    if (GUILayout.Button("+ Patch", EditorStyles.miniButtonLeft))
                        SetVersion(current.Bump(VersionPart.Patch).ToString(), code + 1);
                    if (GUILayout.Button("+ Minor", EditorStyles.miniButtonMid))
                        SetVersion(current.Bump(VersionPart.Minor).ToString(), code + 1);
                    if (GUILayout.Button("+ Major", EditorStyles.miniButtonMid))
                        SetVersion(current.Bump(VersionPart.Major).ToString(), code + 1);
                }

                if (GUILayout.Button("+ Code", EditorStyles.miniButtonRight))
                    SetVersion(version, code + 1);
            }

            string afterBuild = settings.BumpAfterSuccess && AppVersion.TryParse(version, out AppVersion parsedVersion)
                ? $"{parsedVersion.Bump(settings.BumpPart)} ({code + 1})"
                : $"stays {version} ({code})";
            EditorGUILayout.LabelField("After a build", afterBuild, EditorStyles.miniLabel);
        }

        private static void SetVersion(string version, int code)
        {
            if (!AppVersion.TryParse(version, out _))
            {
                EditorUtility.DisplayDialog("AutoBuilder", $"'{version}' is not a version: use major.minor.patch, e.g. 1.2.0.", "OK");
                return;
            }

            ProjectSettingsVersionFile.Write(version, code);
            GUI.FocusControl(null);
        }

        private static void DrawBuildOptions(AutoBuilderUserSettings settings)
        {
            Section("Build");

            settings.Kind = (AabBuildKind)GUILayout.Toolbar((int)settings.Kind, KindLabels);
            EditorGUILayout.Space(2f);

            using (new EditorGUILayout.HorizontalScope())
            {
                settings.BumpAfterSuccess = EditorGUILayout.ToggleLeft("Raise version after a successful build",
                    settings.BumpAfterSuccess);
                using (new EditorGUI.DisabledScope(!settings.BumpAfterSuccess))
                    settings.BumpPart = (VersionPart)EditorGUILayout.EnumPopup(settings.BumpPart, GUILayout.Width(80f));
            }

            settings.BuildAndRun = EditorGUILayout.ToggleLeft(
                new GUIContent("Build & Run — install on the connected phone",
                    "Needs a phone with USB debugging. If the install fails, Unity reports the build as failed and the version is not raised."),
                settings.BuildAndRun);
            settings.IncludeSymbols = EditorGUILayout.ToggleLeft(
                new GUIContent("Native symbols in the bundle",
                    "Play Console reads them from the AAB and shows native crash stacks with function names."),
                settings.IncludeSymbols);

            using (new EditorGUILayout.HorizontalScope())
            {
                settings.OutputDirectory = EditorGUILayout.TextField("Output folder", settings.OutputDirectory);
                if (GUILayout.Button("…", EditorStyles.miniButtonLeft, GUILayout.Width(24f)))
                    ChooseOutputDirectory(settings);
                if (GUILayout.Button("Open", EditorStyles.miniButtonRight, GUILayout.Width(44f)))
                    OpenFolder(AutoBuilderPaths.ResolveDirectory(settings.OutputDirectory));
            }

            EditorGUILayout.LabelField("File", AutoBuilderPaths.AabFileName(PlayerSettings.productName,
                PlayerSettings.bundleVersion, PlayerSettings.Android.bundleVersionCode, settings.Kind, DateTime.Now),
                EditorStyles.miniLabel);
        }

        private static void ChooseOutputDirectory(AutoBuilderUserSettings settings)
        {
            string chosen = EditorUtility.OpenFolderPanel("AAB output folder",
                AutoBuilderPaths.ResolveDirectory(settings.OutputDirectory), string.Empty);
            if (string.IsNullOrEmpty(chosen))
                return;

            string root = AutoBuilderPaths.ProjectRoot.Replace('\\', '/').TrimEnd('/') + "/";
            chosen = chosen.Replace('\\', '/');
            settings.OutputDirectory = chosen.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                ? chosen.Substring(root.Length)
                : chosen;
            settings.Save();
            GUI.FocusControl(null);
        }

        private static void DrawSigning(AutoBuilderUserSettings settings)
        {
            Section("Signing");

            if (!PlayerSettings.Android.useCustomKeystore)
            {
                EditorGUILayout.LabelField("Keystore", "Unity debug key", EditorStyles.miniLabel);
                return;
            }

            EditorGUILayout.LabelField("Keystore", $"{PlayerSettings.Android.keystoreName}  ·  alias {PlayerSettings.Android.keyaliasName}",
                EditorStyles.miniLabel);
            settings.KeystorePass = EditorGUILayout.PasswordField("Keystore password", settings.KeystorePass);
            settings.KeyaliasPass = EditorGUILayout.PasswordField("Alias password", settings.KeyaliasPass);
            EditorGUILayout.LabelField("Kept on this machine only, in UserSettings/ (not in git).", EditorStyles.centeredGreyMiniLabel);
        }

        private static void DrawReleaseNotes(AutoBuilderUserSettings settings)
        {
            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Release notes", EditorStyles.boldLabel);
                if (GUILayout.Button("Copy", EditorStyles.miniButton, GUILayout.Width(50f)))
                    EditorGUIUtility.systemCopyBuffer = settings.ReleaseNotes;
            }

            settings.ReleaseNotes = EditorGUILayout.TextArea(settings.ReleaseNotes, GUILayout.MinHeight(48f));
            EditorGUILayout.LabelField("\"What's new\" for Play Console; saved with the build in the history.",
                EditorStyles.centeredGreyMiniLabel);
        }

        private void DrawChecks()
        {
            Section("Checks");

            if (_issues.Count == 0)
            {
                EditorGUILayout.HelpBox("Ready to build.", MessageType.Info);
                return;
            }

            foreach (PreflightIssue issue in _issues)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.HelpBox(issue.Message,
                        issue.Severity == PreflightSeverity.Error ? MessageType.Error : MessageType.Warning);
                    DrawFixButton(issue.Fix);
                }
            }
        }

        private static void DrawFixButton(PreflightFix fix)
        {
            string label;
            switch (fix)
            {
                case PreflightFix.SwitchToAndroid: label = "Switch"; break;
                case PreflightFix.OpenPlayerSettings: label = "Player"; break;
                case PreflightFix.OpenBuildSettings: label = "Scenes"; break;
                default: return;
            }

            if (!GUILayout.Button(label, GUILayout.Width(56f), GUILayout.Height(38f)))
                return;

            switch (fix)
            {
                case PreflightFix.SwitchToAndroid:
                    EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.Android, BuildTarget.Android);
                    break;
                case PreflightFix.OpenPlayerSettings:
                    SettingsService.OpenProjectSettings("Project/Player");
                    break;
                case PreflightFix.OpenBuildSettings:
                    BuildPlayerWindow.ShowBuildPlayerWindow();
                    break;
            }

            GUIUtility.ExitGUI();
        }

        private void DrawBuildButton(AutoBuilderUserSettings settings)
        {
            EditorGUILayout.Space(6f);
            bool blocked = BuildPreflight.HasErrors(_issues);
            string label = settings.BuildAndRun ? "Build & Run AAB" : "Build AAB";
            string kind = settings.Kind == AabBuildKind.Development ? "  ·  development" : string.Empty;

            using (new EditorGUI.DisabledScope(blocked))
            {
                Color background = GUI.backgroundColor;
                GUI.backgroundColor = blocked ? background : new Color(0.55f, 0.9f, 0.55f);
                bool pressed = GUILayout.Button(
                    $"{label}   v{PlayerSettings.bundleVersion} ({PlayerSettings.Android.bundleVersionCode}){kind}",
                    GUILayout.Height(34f));
                GUI.backgroundColor = background;

                if (pressed)
                {
                    // BuildPlayer must not run inside OnGUI: it would break the window's layout mid-frame.
                    EditorApplication.delayCall += RunBuild;
                    GUIUtility.ExitGUI();
                }
            }
        }

        private void RunBuild()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            AutoBuilderUserSettings settings = AutoBuilderUserSettings.instance;
            settings.Save();
            AabBuildResult result = AabBuilder.Build(settings.ToRequest());

            RefreshGit();
            EditorApplication.Beep();
            if (!result.Succeeded && result.Report == null)
                EditorUtility.DisplayDialog("AutoBuilder", result.Error, "OK");
            Repaint();
        }

        private static void DrawLastBuild()
        {
            IReadOnlyList<AabBuildRecord> records = AabBuildHistory.instance.Records;
            if (records.Count == 0)
                return;

            AabBuildRecord last = records[0];
            Section("Last build");

            if (last.Succeeded)
            {
                string message = $"v{last.Version} ({last.VersionCode}) {last.Kind}, {AabBuilder.FormatSize(last.SizeBytes)}, " +
                                 $"{FormatSeconds(last.DurationSeconds)}  ·  {last.StartedAt}\n{last.OutputPath}";
                if (!string.IsNullOrEmpty(last.Error))
                    message += "\n" + last.Error;
                EditorGUILayout.HelpBox(message, string.IsNullOrEmpty(last.Error) ? MessageType.Info : MessageType.Warning);

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(!File.Exists(last.OutputPath)))
                    {
                        if (GUILayout.Button("Show in Explorer"))
                            EditorUtility.RevealInFinder(last.OutputPath);
                    }

                    if (GUILayout.Button("Copy path"))
                        EditorGUIUtility.systemCopyBuffer = last.OutputPath;
                }
            }
            else
            {
                EditorGUILayout.HelpBox($"v{last.Version} ({last.VersionCode}) {last.Kind} failed  ·  {last.StartedAt}\n{last.Error}",
                    MessageType.Error);
            }
        }

        private void DrawHistory()
        {
            IReadOnlyList<AabBuildRecord> records = AabBuildHistory.instance.Records;
            if (records.Count == 0)
                return;

            EditorGUILayout.Space(6f);
            _showHistory = EditorGUILayout.Foldout(_showHistory, $"History ({records.Count})", true);
            if (!_showHistory)
                return;

            foreach (AabBuildRecord record in records)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    GUIContent icon = EditorGUIUtility.IconContent(record.Succeeded ? "TestPassed" : "TestFailed");
                    GUILayout.Label(icon, GUILayout.Width(18f), GUILayout.Height(18f));

                    string kind = record.Kind == AabBuildKind.Development ? " dev" : string.Empty;
                    string size = record.Succeeded ? $"  {AabBuilder.FormatSize(record.SizeBytes)}" : string.Empty;
                    string dirty = record.UncommittedFiles > 0 ? "*" : string.Empty;
                    GUILayout.Label(new GUIContent(
                            $"v{record.Version} ({record.VersionCode}){kind}   {record.StartedAt}{size}   {FormatSeconds(record.DurationSeconds)}   {record.Branch}@{record.Commit}{dirty}",
                            record.Succeeded ? record.OutputPath : record.Error),
                        EditorStyles.miniLabel);
                    GUILayout.FlexibleSpace();

                    using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(record.ReleaseNotes)))
                    {
                        if (GUILayout.Button(new GUIContent("Notes", record.ReleaseNotes), EditorStyles.miniButtonLeft, GUILayout.Width(44f)))
                            EditorGUIUtility.systemCopyBuffer = record.ReleaseNotes;
                    }

                    using (new EditorGUI.DisabledScope(!File.Exists(record.OutputPath)))
                    {
                        if (GUILayout.Button(EditorGUIUtility.IconContent("FolderOpened Icon"), EditorStyles.miniButtonRight,
                                GUILayout.Width(28f), GUILayout.Height(18f)))
                            EditorUtility.RevealInFinder(record.OutputPath);
                    }
                }
            }

            if (GUILayout.Button("Clear history", EditorStyles.miniButton, GUILayout.Width(100f)) &&
                EditorUtility.DisplayDialog("AutoBuilder", "Clear the build history? The .aab files stay on disk.", "Clear", "Cancel"))
                AabBuildHistory.instance.Clear();
        }

        private static void Section(string title)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        private static void OpenFolder(string directory)
        {
            Directory.CreateDirectory(directory);
            Application.OpenURL("file:///" + directory.Replace('\\', '/'));
        }

        private static string FormatSeconds(float seconds) => AabBuilder.FormatDuration(TimeSpan.FromSeconds(seconds));
    }
}
