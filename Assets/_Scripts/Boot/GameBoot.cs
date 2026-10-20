using Cysharp.Threading.Tasks;
using Game.Events;
using Game.Gameplay.Events;
using UnityEngine;
using Zenject;

public class GameBoot : MonoBehaviour
{
    [Inject] private SceneLoader _sceneLoader;
    [Inject] private IGlobalGameEvents _events;

    private void Start()
    {
        Initialize();
    }
    public void Initialize()
    {
        InitProjectAdjustments();
        RunBoot().Forget();
    }

    private void InitProjectAdjustments()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 120;
    }
    async UniTask RunBoot()
    {
        //await config.InternalLoad();
        //await save.InternalLoad();

        // The service itself is installed by the ProjectContext; this is where boot confirms it is up.
        _events.Publish(new GameBooted());

        _sceneLoader.LoadScene(SceneLoader.SceneType.MenuScene);
    }

}