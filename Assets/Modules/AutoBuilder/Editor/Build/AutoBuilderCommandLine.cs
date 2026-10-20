using System;
using UnityEditor;
using UnityEngine;

namespace Game.AutoBuilder
{
    /// <summary>
    /// The same build from the command line, for CI or a script:
    /// <code>
    /// Unity.exe -batchmode -projectPath &lt;project&gt; -buildTarget Android
    ///   -executeMethod Game.AutoBuilder.AutoBuilderCommandLine.Build
    ///   [-abKind Release|Development] [-abBump Patch|Minor|Major|None] [-abOutput &lt;folder&gt;] -logFile -
    /// </code>
    /// Anything not given comes from the AutoBuilder window's settings on this machine. The keystore passwords come
    /// from the AUTOBUILDER_KEYSTORE_PASS and AUTOBUILDER_KEYALIAS_PASS environment variables when they are set.
    /// The editor exits with 0 when the bundle is built, 1 otherwise.
    /// </summary>
    public static class AutoBuilderCommandLine
    {
        public static void Build()
        {
            int exitCode = 1;
            try
            {
                AabBuildRequest request = AutoBuilderUserSettings.instance.ToRequest();
                string[] args = Environment.GetCommandLineArgs();

                string kind = Argument(args, "-abKind");
                if (kind != null)
                    request.Kind = (AabBuildKind)Enum.Parse(typeof(AabBuildKind), kind, true);

                string bump = Argument(args, "-abBump");
                if (bump != null && bump.Equals("None", StringComparison.OrdinalIgnoreCase))
                    request.BumpAfterSuccess = false;
                else if (bump != null)
                {
                    request.BumpAfterSuccess = true;
                    request.BumpPart = (VersionPart)Enum.Parse(typeof(VersionPart), bump, true);
                }

                request.OutputDirectory = Argument(args, "-abOutput") ?? request.OutputDirectory;
                request.KeystorePass = Environment.GetEnvironmentVariable("AUTOBUILDER_KEYSTORE_PASS") ?? request.KeystorePass;
                request.KeyaliasPass = Environment.GetEnvironmentVariable("AUTOBUILDER_KEYALIAS_PASS") ?? request.KeyaliasPass;
                request.BuildAndRun = false;

                exitCode = AabBuilder.Build(request).Succeeded ? 0 : 1;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            if (Application.isBatchMode)
                EditorApplication.Exit(exitCode);
        }

        private static string Argument(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }

            return null;
        }
    }
}
