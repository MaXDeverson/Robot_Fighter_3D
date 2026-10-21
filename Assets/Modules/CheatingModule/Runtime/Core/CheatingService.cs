using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Cheating
{
    /// <summary>
    /// The admin mode and the registry of cheats. The access is worked out once, in the constructor; the switch is read
    /// from the storage then and written back on every change.
    /// </summary>
    public sealed class CheatingService : ICheatingService
    {
        private readonly IAdminStorage _storage;
        private readonly Dictionary<CheatDefinition, List<Registration>> _handlers =
            new Dictionary<CheatDefinition, List<Registration>>();

        private bool _isAdminEnabled;

        public CheatingService(CheatingConfig config, IDeviceInfo device, IAdminStorage storage)
        {
            _storage = storage;
            Access = AdminAccessPolicy.Resolve(config, device);

            // A switch saved on another build or edited into the files means nothing where the mode is not available.
            _isAdminEnabled = IsAdminAvailable && storage.IsAdminEnabled;

            if (config != null && config.LogDeviceIdOnStart)
                LogDevice(config, device);
        }

        public event Action<bool> AdminEnabledChanged;

        public AdminAccess Access { get; }

        public bool IsAdminAvailable => Access != AdminAccess.Denied;

        public bool IsAdminEnabled => _isAdminEnabled;

        public IReadOnlyCollection<CheatDefinition> RegisteredCheats => _handlers.Keys;

        public void SetAdminEnabled(bool enabled)
        {
            if (enabled && !IsAdminAvailable)
            {
                Debug.LogWarning("[Cheating] The admin mode is not available on this device.");
                return;
            }

            if (_isAdminEnabled == enabled)
                return;

            _isAdminEnabled = enabled;
            _storage.IsAdminEnabled = enabled;
            Debug.Log($"[Cheating] Admin mode {(enabled ? "on" : "off")}.");

            NotifyAdminEnabledChanged(enabled);
        }

        public IDisposable Register(CheatDefinition cheat, Action execute)
        {
            if (cheat == null)
                throw new ArgumentNullException(nameof(cheat));

            if (execute == null)
                throw new ArgumentNullException(nameof(execute));

            if (!_handlers.TryGetValue(cheat, out List<Registration> registrations))
            {
                registrations = new List<Registration>();
                _handlers.Add(cheat, registrations);
            }

            var registration = new Registration(this, cheat, execute);
            registrations.Add(registration);
            return registration;
        }

        public bool CanExecute(CheatDefinition cheat)
        {
            return _isAdminEnabled && cheat != null && _handlers.ContainsKey(cheat);
        }

        public bool Execute(CheatDefinition cheat)
        {
            if (cheat == null)
                return false;

            if (!_isAdminEnabled)
            {
                Debug.LogWarning($"[Cheating] '{cheat.Title}' refused: the admin mode is off.");
                return false;
            }

            if (!_handlers.TryGetValue(cheat, out List<Registration> registrations))
            {
                Debug.LogWarning($"[Cheating] Nothing in the loaded scenes handles '{cheat.Title}'.");
                return false;
            }

            Registration handler = registrations[registrations.Count - 1];
            Debug.Log($"[Cheating] {cheat.Title}");

            try
            {
                handler.Execute();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }
        }

        private void Unregister(Registration registration)
        {
            if (!_handlers.TryGetValue(registration.Cheat, out List<Registration> registrations))
                return;

            if (registrations.Remove(registration) && registrations.Count == 0)
                _handlers.Remove(registration.Cheat);
        }

        // One listener that throws must not keep the others from hearing the change.
        private void NotifyAdminEnabledChanged(bool enabled)
        {
            Action<bool> listeners = AdminEnabledChanged;
            if (listeners == null)
                return;

            foreach (Delegate listener in listeners.GetInvocationList())
            {
                try
                {
                    ((Action<bool>)listener)(enabled);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        private void LogDevice(CheatingConfig config, IDeviceInfo device)
        {
            if (device == null)
                return;

            string access = Access switch
            {
                AdminAccess.AllowedDevice when config.TryFindDevice(device.DeviceId, out CheatingConfig.AllowedDevice entry)
                    => $"available (allowed device '{entry.Label}')",
                AdminAccess.Denied => "not available",
                _ => $"available ({Access})",
            };

            Debug.Log($"[Cheating] Device id: {device.DeviceId} ({device.DeviceModel}). Admin mode: {access}, " +
                      $"{(_isAdminEnabled ? "on" : "off")}.");
        }

        private sealed class Registration : IDisposable
        {
            private CheatingService _owner;

            public Registration(CheatingService owner, CheatDefinition cheat, Action execute)
            {
                _owner = owner;
                Cheat = cheat;
                Execute = execute;
            }

            public CheatDefinition Cheat { get; }

            public Action Execute { get; }

            public void Dispose()
            {
                _owner?.Unregister(this);
                _owner = null;
            }
        }
    }
}
