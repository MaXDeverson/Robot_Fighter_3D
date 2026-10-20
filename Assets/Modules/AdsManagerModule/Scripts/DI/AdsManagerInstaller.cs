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
    /// The provider is the <see cref="BaseAdsProvider"/> stub, created from its prefab on a GameObject of its own under
    /// this one — unless an installer earlier in the <b>Mono Installers</b> list has bound an <see cref="IAdsProvider"/>
    /// already, which is how a real ad network replaces the stub. Storage, clock and pause are bound with
    /// <c>IfNotBound</c> the same way.
    /// </para>
    /// </remarks>
    internal sealed class AdsManagerInstaller : MonoInstaller
    {
        [SerializeField]
        [Tooltip("Levels, cooldown, pausing and logging. Without one the defaults are used.")]
        private AdsConfig _config;

        [SerializeField]
        [Tooltip("The stub ad network, created on a GameObject of its own under this one. Not used when another " +
                 "installer has bound a provider already.")]
        private BaseAdsProvider _baseProviderPrefab;

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

            if (_baseProviderPrefab == null)
            {
                Debug.LogError(
                    "[Ads] No provider prefab assigned on '" + name + "' and no other provider bound, so no ad will " +
                    "ever be shown. Assign Prefabs/BaseAdsProvider.", this);
                Container.Bind<IAdsProvider>().To<NullAdsProvider>().AsSingle();
                return;
            }

            Container.Bind<IAdsProvider>()
                .To<BaseAdsProvider>()
                .FromComponentInNewPrefab(_baseProviderPrefab)
                .WithGameObjectName("BaseAdsProvider")
                .UnderTransform(transform)
                .AsSingle();
        }
    }
}
