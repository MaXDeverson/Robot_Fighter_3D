using UnityEngine;
using Zenject;

namespace Game.Analytics.DI
{
    /// <summary>
    /// Flushes the sinks and saves the storage when the application is paused or quits — the moments a mobile OS may
    /// end the process without another frame.
    /// </summary>
    /// <remarks>
    /// Created by the <see cref="AnalyticsInstaller"/> on a GameObject of its own under the context, so it lives exactly
    /// as long as the service.
    /// </remarks>
    internal sealed class AnalyticsApplicationHooks : MonoBehaviour
    {
        private AnalyticsService _service;
        private IAnalyticsStorage _storage;

        [Inject]
        private void Construct(AnalyticsService service, IAnalyticsStorage storage)
        {
            _service = service;
            _storage = storage;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                Persist();
            }
        }

        private void OnApplicationQuit()
        {
            Persist();
        }

        private void Persist()
        {
            if (_service != null)
            {
                _service.Flush();
            }

            if (_storage != null)
            {
                _storage.Save();
            }
        }
    }
}
