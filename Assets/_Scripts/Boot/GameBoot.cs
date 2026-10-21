using Cysharp.Threading.Tasks;
using Game.Cheating;
using Game.Events;
using Game.Gameplay.Events;
using Game.Privacy;
using UnityEngine;
using Zenject;

public class GameBoot : MonoBehaviour
{
    [Inject] private SceneLoader _sceneLoader;
    [Inject] private IGlobalGameEvents _events;
    [Inject] private ICheatingService _cheating;
    [Inject] private IPrivacyService _privacy;

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

        // Before anything is collected: the consent platform shows its form where the law requires one (the EEA, the
        // UK, Switzerland) and nothing anywhere else. Ads and analytics wait for its answer.
        await GatherConsent();

        _events.Publish(new GameLaunched());
        _sceneLoader.LoadScene(SceneLoader.SceneType.MenuScene);
    }

    /// <summary>
    /// Answered when the consent platform is done, has failed or has timed out — boot never hangs on it. The timeout
    /// waits while the player reads the form.
    /// </summary>
    private UniTask GatherConsent()
    {
        var completion = new UniTaskCompletionSource();
        _privacy.GatherConsent(() => completion.TrySetResult());
        return completion.Task;
    }

}