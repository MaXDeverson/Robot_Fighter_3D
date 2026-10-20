using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

public class GameBoot : MonoBehaviour
{
    [Inject] private SceneLoader _sceneLoader;

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

        _sceneLoader.LoadScene(SceneLoader.SceneType.MenuScene);
    }

}