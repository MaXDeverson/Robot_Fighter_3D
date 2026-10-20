using System;
using System.Diagnostics;

namespace Game.AutoBuilder
{
    /// <summary>The branch, the commit and the uncommitted changes a build is made from. Read only, never writes.</summary>
    internal sealed class GitInfo
    {
        public static readonly GitInfo Unavailable = new GitInfo(false, string.Empty, string.Empty, 0);

        public readonly bool IsAvailable;
        public readonly string Branch;
        public readonly string Commit;
        public readonly int ChangedFiles;

        private GitInfo(bool isAvailable, string branch, string commit, int changedFiles)
        {
            IsAvailable = isAvailable;
            Branch = branch;
            Commit = commit;
            ChangedFiles = changedFiles;
        }

        public static GitInfo Read()
        {
            string branch = Run("rev-parse --abbrev-ref HEAD");
            string commit = Run("rev-parse --short HEAD");
            string status = Run("status --porcelain");
            if (branch == null || commit == null || status == null)
                return Unavailable;

            int changed = status.Length == 0
                ? 0
                : status.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
            return new GitInfo(true, branch, commit, changed);
        }

        /// <summary>The command's trimmed output, or null when git is missing, fails or hangs.</summary>
        private static string Run(string arguments)
        {
            try
            {
                var startInfo = new ProcessStartInfo("git", arguments)
                {
                    WorkingDirectory = AutoBuilderPaths.ProjectRoot,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };

                using (Process process = Process.Start(startInfo))
                {
                    if (process == null)
                        return null;

                    var output = process.StandardOutput.ReadToEndAsync();
                    process.StandardError.ReadToEndAsync();
                    if (!process.WaitForExit(5000))
                    {
                        process.Kill();
                        return null;
                    }

                    return process.ExitCode == 0 ? output.Result.Trim() : null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
