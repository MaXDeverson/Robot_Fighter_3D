using UnityEngine;
using Zenject;

namespace Game.Privacy.DI
{
    /// <summary>
    /// Installs <see cref="IPrivacyService"/> into the context this component belongs to — the <c>ProjectContext</c>:
    /// consent is gathered once at boot and read by the ads and the analytics for the whole session.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The consent platform is created from a prefab on a GameObject of its own under this one: the
    /// <see cref="BasePrivacyConsentProvider"/> stub, or — with <b>Provider</b> set to <c>Network</c> — the prefab of a
    /// real platform such as Google's UMP, which derives from <see cref="PrivacyConsentProvider"/> outside this module.
    /// </para>
    /// <para>
    /// The clock and the URL opener are bound with <c>IfNotBound</c>, so an installer earlier in the list can replace
    /// them.
    /// </para>
    /// </remarks>
    public sealed class PrivacyInstaller : MonoInstaller
    {
        /// <summary>Which consent platform the game runs on.</summary>
        internal enum ProviderKind
        {
            /// <summary>The stub: no form, answers as its Inspector says.</summary>
            Base = 0,

            /// <summary>A real platform from <b>Network Provider Prefab</b>.</summary>
            Network = 1,
        }

        [SerializeField]
        [Tooltip("The privacy policy page, the contact address, the timeout and the testing settings. Without one " +
                 "there is no privacy policy and the defaults are used.")]
        private PrivacyConfig _config;

        [SerializeField]
        [Tooltip("Base: the stub, no form. Network: the real consent platform from Network Provider Prefab.")]
        private ProviderKind _provider = ProviderKind.Base;

        [SerializeField]
        [Tooltip("Prefabs/BasePrivacyConsentProvider — the stub.")]
        private BasePrivacyConsentProvider _baseProviderPrefab;

        [SerializeField]
        [Tooltip("The prefab of the real consent platform (Google's UMP), with a PrivacyConsentProvider on its root.")]
        private PrivacyConsentProvider _networkProviderPrefab;

        /// <inheritdoc />
        public override void InstallBindings()
        {
            if (Container.HasBindingId(typeof(IPrivacyService), null, InjectSources.Local))
            {
                Debug.LogWarning(
                    "[Privacy] This context already installs the privacy service, so the installer on '" + name +
                    "' was skipped. Keep exactly one PrivacyInstaller per context.", this);
                return;
            }

            if (Container.HasBindingId(typeof(IPrivacyService), null, InjectSources.AnyParent))
            {
                // A second service would gather consent a second time and could show the form twice.
                Debug.LogWarning(
                    "[Privacy] A parent context already provides the privacy service, so the installer on '" + name +
                    "' was skipped and everything here uses the parent's.", this);
                return;
            }

            PrivacySettings settings;
            if (_config != null)
            {
                settings = _config.BuildSettings(Debug.isDebugBuild);
            }
            else
            {
                Debug.LogError(
                    "[Privacy] No config assigned on '" + name + "', so the game has no privacy policy to open. " +
                    "Assign Configs/PrivacyConfig.", this);
                settings = new PrivacySettings(logRequests: Debug.isDebugBuild);
            }

            Container.BindInstance(settings).WhenInjectedInto<PrivacyService>();
            Container.Bind<IPrivacyClock>().To<UnityPrivacyClock>().AsSingle().IfNotBound();
            Container.Bind<IUrlOpener>().To<UnityUrlOpener>().AsSingle().IfNotBound();
            BindProvider();

            Container.BindInterfacesTo<PrivacyService>().AsSingle();
        }

        /// <summary>Test hook: fills the installer without going through the serializer.</summary>
        internal void Setup(
            PrivacyConfig config,
            ProviderKind provider,
            BasePrivacyConsentProvider baseProviderPrefab,
            PrivacyConsentProvider networkProviderPrefab = null)
        {
            _config = config;
            _provider = provider;
            _baseProviderPrefab = baseProviderPrefab;
            _networkProviderPrefab = networkProviderPrefab;
        }

        private void BindProvider()
        {
            PrivacyConsentProvider prefab = _baseProviderPrefab;
            string objectName = "BasePrivacyConsentProvider";

            if (_provider == ProviderKind.Network)
            {
                if (_networkProviderPrefab != null)
                {
                    prefab = _networkProviderPrefab;
                    objectName = _networkProviderPrefab.name;
                }
                else
                {
                    Debug.LogError(
                        "[Privacy] Provider is Network on '" + name + "', but no Network Provider Prefab is assigned, " +
                        "so the stub is used and no consent form will ever be shown.", this);
                }
            }

            if (prefab == null)
            {
                Debug.LogError(
                    "[Privacy] No provider prefab assigned on '" + name + "', so consent is never gathered and ads " +
                    "and analytics stay off. Assign Prefabs/BasePrivacyConsentProvider.", this);
                Container.Bind<IPrivacyConsentProvider>().To<NullPrivacyConsentProvider>().AsSingle();
                return;
            }

            Container.Bind<IPrivacyConsentProvider>()
                .To(prefab.GetType())
                .FromComponentInNewPrefab(prefab)
                .WithGameObjectName(objectName)
                .UnderTransform(transform)
                .AsSingle();
        }
    }
}
