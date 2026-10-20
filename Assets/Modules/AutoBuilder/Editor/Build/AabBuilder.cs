using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Unity.Android.Types;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Game.AutoBuilder
{
    /// <summary>
    /// Builds the Android App Bundle and, once it is built, raises the game version and the version code for the next
    /// one. A failed or cancelled build changes nothing in the project.
    /// </summary>
    internal static class AabBuilder
    {
        private const string LogTag = "[AutoBuilder]";

        public static AabBuildResult Build(AabBuildRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var stopwatch = Stopwatch.StartNew();
            DateTime startedAt = DateTime.Now;
            GitInfo git = GitInfo.Read();
            AabBuildHistory history = AabBuildHistory.instance;

            var result = new AabBuildResult
            {
                Version = PlayerSettings.bundleVersion,
                VersionCode = PlayerSettings.Android.bundleVersionCode,
            };

            List<PreflightIssue> issues = BuildPreflight.Run(request, git, history);
            if (BuildPreflight.HasErrors(issues))
            {
                result.Error = "Not built:\n" + string.Join("\n", issues
                    .Where(issue => issue.Severity == PreflightSeverity.Error)
                    .Select(issue => "• " + issue.Message));
                Debug.LogError($"{LogTag} {result.Error}");
                return result;
            }

            foreach (PreflightIssue warning in issues)
                Debug.LogWarning($"{LogTag} {warning.Message}");

            string directory = AutoBuilderPaths.ResolveDirectory(request.OutputDirectory);
            result.OutputPath = Path.Combine(directory, AutoBuilderPaths.AabFileName(
                PlayerSettings.productName, result.Version, result.VersionCode, request.Kind, startedAt));

            RunBuild(request, directory, result);
            result.Duration = stopwatch.Elapsed;

            if (result.Succeeded && request.BumpAfterSuccess)
                RaiseVersion(request.BumpPart, result);

            history.Add(ToRecord(request, result, git, startedAt));
            LogOutcome(result);
            return result;
        }

        private static void RunBuild(AabBuildRequest request, string directory, AabBuildResult result)
        {
            bool appBundleBefore = EditorUserBuildSettings.buildAppBundle;
            DebugSymbolLevel symbolLevelBefore = UserBuildSettings.DebugSymbols.level;
            DebugSymbolFormat symbolFormatBefore = UserBuildSettings.DebugSymbols.format;

            try
            {
                Directory.CreateDirectory(directory);

                EditorUserBuildSettings.buildAppBundle = true;
                if (request.IncludeSymbols)
                {
                    UserBuildSettings.DebugSymbols.level = DebugSymbolLevel.SymbolTable;
                    UserBuildSettings.DebugSymbols.format = DebugSymbolFormat.IncludeInBundle;
                }

                // Unity keeps these only until the editor closes, so they are set again for every build.
                if (PlayerSettings.Android.useCustomKeystore)
                {
                    PlayerSettings.Android.keystorePass = request.KeystorePass;
                    PlayerSettings.Android.keyaliasPass = request.KeyaliasPass;
                }

                var options = new BuildPlayerOptions
                {
                    scenes = BuildPreflight.BuildScenes(),
                    locationPathName = result.OutputPath,
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = Options(request),
                };

                BuildReport report = BuildPipeline.BuildPlayer(options);
                result.Report = report;

                if (report.summary.result == BuildResult.Succeeded && File.Exists(result.OutputPath))
                {
                    result.Succeeded = true;
                    result.SizeBytes = new FileInfo(result.OutputPath).Length;
                }
                else
                {
                    result.Error = Describe(report);
                }
            }
            catch (Exception exception)
            {
                result.Error = exception.Message;
                Debug.LogException(exception);
            }
            finally
            {
                EditorUserBuildSettings.buildAppBundle = appBundleBefore;
                UserBuildSettings.DebugSymbols.level = symbolLevelBefore;
                UserBuildSettings.DebugSymbols.format = symbolFormatBefore;
            }
        }

        private static BuildOptions Options(AabBuildRequest request)
        {
            BuildOptions options = BuildOptions.None;
            if (request.Kind == AabBuildKind.Development)
                options |= BuildOptions.Development | BuildOptions.AllowDebugging;
            if (request.BuildAndRun)
                options |= BuildOptions.AutoRunPlayer;
            return options;
        }

        private static void RaiseVersion(VersionPart part, AabBuildResult result)
        {
            if (!AppVersion.TryParse(result.Version, out AppVersion built))
                return;

            string next = built.Bump(part).ToString();
            int nextCode = result.VersionCode + 1;
            try
            {
                ProjectSettingsVersionFile.Write(next, nextCode);
                result.NextVersion = next;
                result.NextVersionCode = nextCode;
            }
            catch (Exception exception)
            {
                result.Error = $"Built, but the version was not raised: {exception.Message}";
                Debug.LogException(exception);
            }
        }

        private static string Describe(BuildReport report)
        {
            if (report.summary.result == BuildResult.Cancelled)
                return "The build was cancelled.";

            string[] errors = report.steps
                .SelectMany(step => step.messages)
                .Where(message => message.type == LogType.Error || message.type == LogType.Exception)
                .Select(message => message.content.Trim())
                .Distinct()
                .Take(5)
                .ToArray();

            return errors.Length > 0
                ? $"The build failed ({report.summary.totalErrors} error(s)):\n" + string.Join("\n", errors)
                : $"The build failed ({report.summary.result}). See the Console.";
        }

        private static AabBuildRecord ToRecord(AabBuildRequest request, AabBuildResult result, GitInfo git,
            DateTime startedAt)
        {
            return new AabBuildRecord
            {
                Succeeded = result.Succeeded,
                Error = result.Error,
                Kind = request.Kind,
                Version = result.Version,
                VersionCode = result.VersionCode,
                StartedAt = startedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                DurationSeconds = (float)result.Duration.TotalSeconds,
                SizeBytes = result.SizeBytes,
                OutputPath = result.Succeeded ? result.OutputPath : string.Empty,
                Branch = git.Branch,
                Commit = git.Commit,
                UncommittedFiles = git.ChangedFiles,
                ReleaseNotes = request.ReleaseNotes,
            };
        }

        private static void LogOutcome(AabBuildResult result)
        {
            if (!result.Succeeded)
            {
                Debug.LogError($"{LogTag} v{result.Version} ({result.VersionCode}) — {result.Error}");
                return;
            }

            string raised = result.Bumped
                ? $" The project is now v{result.NextVersion} ({result.NextVersionCode})."
                : string.Empty;
            Debug.Log($"{LogTag} Built v{result.Version} ({result.VersionCode}), {FormatSize(result.SizeBytes)} " +
                      $"in {FormatDuration(result.Duration)}: {result.OutputPath}.{raised}");
            if (!string.IsNullOrEmpty(result.Error))
                Debug.LogError($"{LogTag} {result.Error}");
        }

        public static string FormatSize(long bytes) =>
            string.Format(CultureInfo.InvariantCulture, "{0:0.0} MB", bytes / (1024.0 * 1024.0));

        public static string FormatDuration(TimeSpan duration) =>
            duration.TotalHours >= 1
                ? duration.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture)
                : duration.ToString(@"m\:ss", CultureInfo.InvariantCulture);
    }
}
