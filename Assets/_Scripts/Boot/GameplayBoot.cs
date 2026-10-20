using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Starts the gameplay scene.
/// </summary>
[DefaultExecutionOrder(-50)]
public class GameplayBoot : MonoBehaviour
{
    private void Start()
    {
        Initialize();
    }
    public void Initialize()
    {
        RunBoot().Forget();
    }


    async UniTask RunBoot()
    {
    }
}
