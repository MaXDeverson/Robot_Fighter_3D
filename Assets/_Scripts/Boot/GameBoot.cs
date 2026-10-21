using Cysharp.Threading.Tasks;
using Game.Cheating;
using Game.Events;
using Game.Gameplay.Events;
using UnityEngine;
using Zenject;

public class GameBoot : MonoBehaviour
{
    [Inject] private SceneLoader _sceneLoader;
    [Inject] private IGlobalGameEvents _events;
    [Inject] private ICheatingService _cheating;

    private void Start()
    {
        Initialize();
    }
    public void Initialize()
    {
        // The service itself is installed by the ProjectContext; this is where boot confirms it is up.
        _events.Publish(new GameBooted());

        InitProjectAdjustments();
        InitCheating();
        RunBoot().Forget();
    }

    private void InitProjectAdjustments()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 120;
    }

    // The service is created with the ProjectContext and reads the admin switch and the device's access there; boot
    // makes sure it is up before the menu, where the switch lives.
    private void InitCheating()
    {
        Debug.Log($"[Boot] Admin mode: {(_cheating.IsAdminEnabled ? "on" : "off")} " +
                  $"({(_cheating.IsAdminAvailable ? _cheating.Access.ToString() : "not available")}).");
    }
    async UniTask RunBoot()
    {
        //await config.InternalLoad();
        //await save.InternalLoad();

        _events.Publish(new GameLaunched());
        _sceneLoader.LoadScene(SceneLoader.SceneType.MenuScene);
    }

}