using System;
using System.IO;
using UnityEngine;

namespace Game.Cheating
{
    /// <summary>
    /// Keeps the admin switch in a file of its own, <c>Cheating/admin_enabled</c> under the persistent data path. Not in
    /// the game's saves and not in PlayerPrefs: "Reset game" deletes the files at the top of that folder and all
    /// PlayerPrefs, and the admin mode should stay on through it — a fresh start is tested with the cheats at hand.
    /// </summary>
    public sealed class FileAdminStorage : IAdminStorage
    {
        private readonly string _path;
        private bool? _value;

        public FileAdminStorage() : this(Path.Combine(Application.persistentDataPath, "Cheating", "admin_enabled"))
        {
        }

        public FileAdminStorage(string path)
        {
            _path = path;
        }

        public bool IsAdminEnabled
        {
            get => _value ??= Read();
            set
            {
                _value = value;
                Write(value);
            }
        }

        private bool Read()
        {
            try
            {
                return File.Exists(_path) && File.ReadAllText(_path).Trim() == "1";
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Cheating] Could not read the admin switch from {_path}: {exception.Message}");
                return false;
            }
        }

        private void Write(bool value)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path));
                File.WriteAllText(_path, value ? "1" : "0");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Cheating] Could not save the admin switch to {_path}: {exception.Message}");
            }
        }
    }
}
