using Game.Ads;
using UnityEngine;
using Zenject;

public class BootInstaller : MonoInstaller
{
    private SceneLoader _sceneLoader;

    private bool _isLogMode = false;

    public override void InstallBindings()
    {
        Log("Install bindings start");

        _sceneLoader = new SceneLoader();

        Container.Bind<SceneLoader>().FromInstance(_sceneLoader).AsSingle().NonLazy();
        Container.BindInterfacesTo<AdsGameBridge>().AsSingle();
        // The ad network starts only once the player's privacy choices allow ads (PrivacyModule).
        Container.Bind<IAdsConsentSource>().To<PrivacyAdsConsentSource>().AsSingle();
        // Analytics collect nothing before the consent platform has answered, and nothing where the player refused.
        // Before the analytics sinks attach (-100): the answer saved by the analytics module from the last launch must
        // not start GameAnalytics when the consent platform no longer agrees with it.
        Container.BindInterfacesTo<PrivacyAnalyticsConsent>().AsSingle();
        Container.BindInitializableExecutionOrder<PrivacyAnalyticsConsent>(-200);

        Log("Install bindings completed");
    }

    private void Log(string message)
    {
        if(_isLogMode) Debug.Log("[BootInstaller] " + message);
    }
}
