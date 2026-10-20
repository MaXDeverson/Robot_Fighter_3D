using UnityEngine.SceneManagement;

public class SceneLoader
{
    public void LoadScene(SceneType sceneType)
    {
        string sceneName = string.Empty;
        switch (sceneType)
        {
            case SceneType.MenuScene:
                sceneName = "Menu";
                break;
            case SceneType.GameplayScene:
                sceneName = "GamePlay";
                break;
        }
        LoadScene(sceneName);
    }

    private void LoadScene(string scene)
    {
        SceneManager.LoadSceneAsync(scene);
    }

    public enum SceneType
    {
        MenuScene,
        GameplayScene,
    }
}
