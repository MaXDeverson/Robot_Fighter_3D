using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.AutoBuilder
{
    [Serializable]
    internal sealed class AabBuildRecord
    {
        public bool Succeeded;
        public string Error = string.Empty;
        public AabBuildKind Kind;
        public string Version = string.Empty;
        public int VersionCode;

        /// <summary>Local time, "yyyy-MM-dd HH:mm".</summary>
        public string StartedAt = string.Empty;

        public float DurationSeconds;
        public long SizeBytes;
        public string OutputPath = string.Empty;
        public string Branch = string.Empty;
        public string Commit = string.Empty;
        public int UncommittedFiles;
        public string ReleaseNotes = string.Empty;
    }

    /// <summary>The last builds made on this machine, newest first, kept in UserSettings/ next to the window's settings.</summary>
    [FilePath("UserSettings/AutoBuilderHistory.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class AabBuildHistory : ScriptableSingleton<AabBuildHistory>
    {
        public const int MaxRecords = 30;

        [SerializeField] private List<AabBuildRecord> _records = new List<AabBuildRecord>();

        public IReadOnlyList<AabBuildRecord> Records => _records;

        public void Add(AabBuildRecord record)
        {
            _records.Insert(0, record);
            if (_records.Count > MaxRecords)
                _records.RemoveRange(MaxRecords, _records.Count - MaxRecords);
            Save(true);
        }

        public void Clear()
        {
            _records.Clear();
            Save(true);
        }

        /// <summary>The newest successful build, or null.</summary>
        public AabBuildRecord LastSucceeded()
        {
            foreach (AabBuildRecord record in _records)
            {
                if (record.Succeeded)
                    return record;
            }

            return null;
        }
    }
}
