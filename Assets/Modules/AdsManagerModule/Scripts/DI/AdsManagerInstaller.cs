using UnityEngine;
using Zenject;

namespace Game.Ads
{
    /// <summary>
    /// Installs the ads manager into the context this component belongs to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Meant for the <c>ProjectContext</c>: the cooldown and the banner request have to outlive scene loads, and every
    /// death reloads the gameplay scene.
    /// </para>
    /// <para>
    /// The only binding a game script can ask for is <see cref="IAdsManagerService"/>; the provider, the storage, the
    /// clock and the settings are internal types of the module. The service is also bound as <c>IInitializable</c>,
    /// <c>ITickable</c> and <c>IDisposable</c>, so the context drives it.
    /// </para>
    /// <para>
    /// The provider is picked in the Inspector: <see cref="ProviderChoice.Base"/> is the <see cref="BaseAdsProvider"/>
    /// stub, <see cref="ProviderChoice.Network"/> a real ad network's prefab (AdMob's lives in an assembly of its own,
    /// which this module cannot name, so any prefab with an <see cref="IAdsProvider"/> on its root is taken). Either one
    /// is created from its prefab on a GameObject of its own under this one. An installer earlier in the
    /// <b>Mono Installers</b> list that has bound an <see cref="IAdsProvider"/> already still wins over the choice.
    /// Storage, clock and pause are bound with <c>IfNotBound</c> the same way.
    /// </para>
    /// </remarks>
    internal sealed class AdsManagerInstaller : MonoInstaller
    {
        /// <summary>Which ad network the manager uses.</summary>
        internal enum ProviderChoice
        {
            /// <summary>The stub: black screens with a timer, nothing loaded from anywhere.</summary>
            Base = 0,

            /// <summary>The real ad network in <c>_networkProviderPrefab</c>.</summary>
            Network = 1,
        }

        [SerializeField]
        [Tooltip("Levels, cooldown, pausing and logging. Without one the defaults are used.")]
        private AdsConfig _config;

        [SerializeField]
        [Tooltip("Which ad network the manager uses. Base: the stub from Base Provider Prefab, black screens with a " +
                 "timer. Network: the real network from Network Provider Prefab.")]
        private ProviderChoice _provider = ProviderChoice.Base;

        [SerializeField]
        [Tooltip("The stub ad network (Prefabs/BaseAdsProvider), created on a GameObject of its own under this one " +
                 "when Provider is Base.")]
        private BaseAdsProvider _baseProviderPrefab;

        [SerializeField]
        [Tooltip("The real ad network (Prefabs/AdmobAdsProvider): a prefab with an ads provider on its root, created on " +
                 "a GameObject of its own under this one when Provider is Network.")]
        private GameObject _networkProviderPrefab;

        /// <inheritdoc />
        public override void InstallBindings()
        {
            if (Container.HasBindingId(typeof(IAdsManagerService), null, InjectSources.Local))
            {
                Debug.LogWarning(
                    "[Ads] This context already installs the ads manager, so the installer on '" + name + "' was " +
                    "skipped. Keep exactly one AdsManagerInstaller per context.", this);
                return;
            }

            if (Container.HasBindingId(typeof(IAdsManagerService), null, InjectSources.AnyParent))
            {
                // A second manager would keep its own cooldown and banner, and two networks would fight over the screen.
                Debug.LogWarning(
                    "[Ads] A parent context already provides the ads manager, so the installer on '" + name + "' was " +
                    "skipped and everything here uses the parent's.", this);
                return;
            }

            if (_config == null)
            {
                Debug.LogWarning(
                    "[Ads] No config assigned on '" + name + "', so the defaults are used: banners from level " +
                    AdsSettings.DefaultBannerFromLevel + ", interstitials from level " +
                    AdsSettings.DefaultInterstitialFromLevel + " at most every " +
                    AdsSettings.DefaultInterstitialCooldownSeconds + " s, rewarded ads from level " +
                    AdsSettings.DefaultRewardedFromLevel + ".", this);
            }

            AdsSettings settings = _config != null
                ? _config.BuildSettings(Debug.isDebugBuild)
                : new AdsSettings(logRequests: Debug.isDebugBuild);

            Container.BindInstance(settings).WhenInjectedInto<AdsManagerService>();
            Container.Bind<IAdsStorage>().To<PlayerPrefsAdsStorage>().AsSingle().IfNotBound();
            Container.Bind<IAdsClock>().To<UnityAdsClock>().AsSingle().IfNotBound();
            Container.Bind<IAdsGamePause>().To<UnityAdsGamePause>().AsSingle().IfNotBound();
            BindProvider();

            Container.BindInterfacesTo<AdsManagerService>().AsSingle();
        }

        private void BindProvider()
        {
            if (Container.HasBinding<IAdsProvider>())
            {
                return;
            }

            GameObject prefab = ChooseProviderPrefab();
            if (prefab == null)
            {
                Container.Bind<IAdsProvider>().To<NullAdsProvider>().AsSingle();
                return;
            }

            Container.Bind<IAdsProvider>()
                .FromComponentInNewPrefab(prefab)
                .WithGameObjectName(prefab.name)
                .UnderTransform(transform)
                .AsSingle();
        }

        private GameObject ChooseProviderPrefab()
        {
            if (_provider != ProviderChoice.Network)
            {
                if (_baseProviderPrefab == null)
                {
                    Debug.LogError(
                        "[Ads] No provider prefab assigned on '" + name + "' for Base, and no other provider bound, so " +
                        "no ad will ever be shown. Assign Prefabs/BaseAdsProvider to Base Provider Prefab.", this);
                    return null;
                }

                return _baseProviderPrefab.gameObject;
            }

            // No quiet fall back to the stub: a release build would show players black screens instead of ads.
            if (_networkProviderPrefab == null)
            {
                Debug.LogError(
                    "[Ads] No provider prefab assigned on '" + name + "' for Network, and no other provider bound, so " +
                    "no ad will ever be shown. Assign the network's prefab to Network Provider Prefab, or switch " +
                    "Provider to Base.", this);
                return null;
            }

            if (!IsProvider(_networkProviderPrefab))
            {
                Debug.LogError(
                    "[Ads] '" + _networkProviderPrefab.name + "', the Network Provider Prefab on '" + name + "', has " +
                    "no ads provider on its root, so no ad will ever be shown. Assign the network's prefab " +
                    "(Prefabs/AdmobAdsProvider), or switch Provider to Base.", this);
                return null;
            }

            return _networkProviderPrefab;
        }

        private void OnValidate()
        {
            // The field takes any GameObject: the network's type is in an assembly this module cannot reference.
            if (_networkProviderPrefab != null && !IsProvider(_networkProviderPrefab))
            {
                Debug.LogWarning(
                    "[Ads] '" + _networkProviderPrefab.name + "' has no ads provider on its root, so it cannot be the " +
                    "Network Provider Prefab on '" + name + "'.", this);
            }
        }

        private static bool IsProvider(GameObject prefab)
        {
            return prefab.GetComponent<IAdsProvider>() != null;
        }
    }
}
