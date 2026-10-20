using System;
using UnityEditor.Build.Reporting;

namespace Game.AutoBuilder
{
    internal enum AabBuildKind
    {
        /// <summary>For Google Play: no development flags, IL2CPP + ARM64 and the upload key required.</summary>
        Release,

        /// <summary>Development Build with script debugging, for testing on our phones. Never upload it.</summary>
        Development,
    }

    internal sealed class AabBuildRequest
    {
        public AabBuildKind Kind = AabBuildKind.Release;
        public bool BumpAfterSuccess = true;
        public VersionPart BumpPart = VersionPart.Patch;

        /// <summary>Install and start the build on the connected phone once it is built.</summary>
        public bool BuildAndRun;

        /// <summary>Native symbols packed into the AAB, so Play Console shows readable native crash stacks.</summary>
        public bool IncludeSymbols = true;

        /// <summary>Relative to the project root unless absolute.</summary>
        public string OutputDirectory = AutoBuilderPaths.DefaultOutputDirectory;

        public string KeystorePass = string.Empty;
        public string KeyaliasPass = string.Empty;
        public string ReleaseNotes = string.Empty;
    }

    internal sealed class AabBuildResult
    {
        public bool Succeeded;
        public string Error = string.Empty;

        public string OutputPath = string.Empty;
        public string Version = string.Empty;
        public int VersionCode;
        public long SizeBytes;
        public TimeSpan Duration;

        /// <summary>What the project was raised to after the build; empty when it was not raised.</summary>
        public string NextVersion = string.Empty;
        public int NextVersionCode;

        public BuildReport Report;

        public bool Bumped => !string.IsNullOrEmpty(NextVersion);
    }
}
