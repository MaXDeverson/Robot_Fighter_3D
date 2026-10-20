using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static partial class BootLoader
{
    private const string BootScenePath = "Assets/_Scenes/Boot.unity";

    static BootLoader()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        UpdatePlayModeStartScene();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            // ����� ������ � Play Mode ���������� ���� ��������
            UpdatePlayModeStartScene();
        }
    }

    private static void UpdatePlayModeStartScene()
    {
        // �������� ���� �������� � PlayerPrefs
        bool isCheckboxEnabled = MainToolbarCheckbox.CheckboxValue;

        if (isCheckboxEnabled)
        {
            // ���� ������� �������� - ������������ Boot �����
            SceneAsset bootScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath);

            if (bootScene != null)
            {
                if (EditorSceneManager.playModeStartScene != bootScene)
                {
                    EditorSceneManager.playModeStartScene = bootScene;
                    Debug.Log("[BootLoader] Boot scene enabled. Play mode will start from Boot scene.");
                }
            }
            else
            {
                EditorSceneManager.playModeStartScene = null;
                Debug.LogWarning($"[BootLoader] Boot scene not found at path: {BootScenePath}");
            }
        }
        else
        {
            // ���� ������� �������� - ��������� Boot �����
            if (EditorSceneManager.playModeStartScene != null)
            {
                EditorSceneManager.playModeStartScene = null;
                Debug.Log("[BootLoader] Boot scene disabled. Play mode will start from current scene.");
            }
        }
    }

    // �������� ����� ��� ��������� ����� � ��������
    public static void SetBootLoaderEnabled(bool enabled)
    {
        UpdatePlayModeStartScene();

        Debug.Log($"[BootLoader] Boot loader {(enabled ? "enabled" : "disabled")}");
    }
}


//OLD SOLUTION:
public static partial class BootLoader
{
    //private const string PreviousSceneKey = "PreviousScenePath";

    //static BootLoader()
    //{
    //    EditorApplication.playModeStateChanged += OnPlayModeChanged;
    //}

    //private static void OnPlayModeChanged(PlayModeStateChange state)
    //{
    //    if (state == PlayModeStateChange.ExitingEditMode)
    //    {
    //        string currentScene = SceneManager.GetActiveScene().path;

    //        if (currentScene != BootScenePath)
    //        {
    //            SessionState.SetString(PreviousSceneKey, currentScene);

    //            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
    //            {
    //                EditorSceneManager.OpenScene(BootScenePath);
    //            }
    //            else
    //            {
    //                EditorApplication.isPlaying = false;
    //            }
    //        }
    //    }

    //    if (state == PlayModeStateChange.EnteredEditMode)
    //    {
    //        string previousScene = SessionState.GetString(PreviousSceneKey, "");

    //        if (!string.IsNullOrEmpty(previousScene) && previousScene != SceneManager.GetActiveScene().path)
    //        {
    //            EditorSceneManager.OpenScene(previousScene);
    //            SessionState.EraseString(PreviousSceneKey);
    //        }
    //    }
    //}
}

