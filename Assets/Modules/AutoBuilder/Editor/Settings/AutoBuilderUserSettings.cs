using UnityEditor;
using UnityEngine;

namespace Game.AutoBuilder
{
    /// <summary>
    /// This machine's choices in the AutoBuilder window and the keystore passwords, kept in UserSettings/ — the folder
    /// Unity reserves for per-user state, and one this project's .gitignore keeps out of the repository.
    /// </summary>
    /// <remarks>
    /// Unity itself forgets the keystore passwords on every editor restart. They are kept here in plain text, so whoever
    /// can read this file can sign as the game — the same as whoever holds the keystore file and its password.
    /// </remarks>
    [FilePath("UserSettings/AutoBuilderUserSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class AutoBuilderUserSettings : ScriptableSingleton<AutoBuilderUserSettings>
    {
        [SerializeField] private string _keystorePass = string.Empty;
        [SerializeField] private string _keyaliasPass = string.Empty;
        [SerializeField] private AabBuildKind _kind = AabBuildKind.Release;
        [SerializeField] private VersionPart _bumpPart = VersionPart.Patch;
        [SerializeField] private bool _bumpAfterSuccess = true;
        [SerializeField] private bool _buildAndRun;
        [SerializeField] private bool _includeSymbols = true;
        [SerializeField] private string _outputDirectory = AutoBuilderPaths.DefaultOutputDirectory;
        [SerializeField] private string _releaseNotes = string.Empty;

        public string KeystorePass
        {
            get => _keystorePass;
            set => _keystorePass = value ?? string.Empty;
        }

        public string KeyaliasPass
        {
            get => _keyaliasPass;
            set => _keyaliasPass = value ?? string.Empty;
        }

        public AabBuildKind Kind
        {
            get => _kind;
            set => _kind = value;
        }

        public VersionPart BumpPart
        {
            get => _bumpPart;
            set => _bumpPart = value;
        }

        public bool BumpAfterSuccess
        {
            get => _bumpAfterSuccess;
            set => _bumpAfterSuccess = value;
        }

        public bool BuildAndRun
        {
            get => _buildAndRun;
            set => _buildAndRun = value;
        }

        public bool IncludeSymbols
        {
            get => _includeSymbols;
            set => _includeSymbols = value;
        }

        public string OutputDirectory
        {
            get => string.IsNullOrWhiteSpace(_outputDirectory) ? AutoBuilderPaths.DefaultOutputDirectory : _outputDirectory;
            set => _outputDirectory = value ?? string.Empty;
        }

        public string ReleaseNotes
        {
            get => _releaseNotes;
            set => _releaseNotes = value ?? string.Empty;
        }

        public AabBuildRequest ToRequest() => new AabBuildRequest
        {
            Kind = _kind,
            BumpAfterSuccess = _bumpAfterSuccess,
            BumpPart = _bumpPart,
            BuildAndRun = _buildAndRun,
            IncludeSymbols = _includeSymbols,
            OutputDirectory = OutputDirectory,
            KeystorePass = _keystorePass,
            KeyaliasPass = _keyaliasPass,
            ReleaseNotes = _releaseNotes,
        };

        public void Save() => Save(true);
    }
}
