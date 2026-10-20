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

        Log("Install bindings completed");
    }

    private void Log(string message)
    {
        if(_isLogMode) Debug.Log("[BootInstaller] " + message);
    }
}
