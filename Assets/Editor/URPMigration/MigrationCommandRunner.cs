using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Temporary helper for the URP migration: runs editor commands written to Temp/URPMigration/command.txt and
/// writes their log to Temp/URPMigration/result.txt, so the migration can be driven while another editor has
/// the Unity MCP connection. Delete this file once the migration is done.
/// </summary>
[InitializeOnLoad]
internal static class MigrationCommandRunner
{
    private const string Folder = "Temp/URPMigration";
    private static double _nextPoll;

    static MigrationCommandRunner()
    {
        EditorApplication.update += Poll;
    }

    private static void Poll()
    {
        if (EditorApplication.timeSinceStartup < _nextPoll)
            return;
        _nextPoll = EditorApplication.timeSinceStartup + 1.0;

        string commandPath = Folder + "/command.txt";
        if (!File.Exists(commandPath) || EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;

        string[] commands = File.ReadAllLines(commandPath);
        File.Delete(commandPath);

        var log = new StringBuilder();
        Application.LogCallback capture = (message, stack, type) =>
            log.AppendLine(type == LogType.Log ? message : $"[{type}] {message}\n{stack}");
        Application.logMessageReceived += capture;
        try
        {
            foreach (string command in commands.Where(c => !string.IsNullOrWhiteSpace(c)))
            {
                log.AppendLine("> " + command);
                Run(command.Trim());
            }
        }
        catch (Exception e)
        {
            log.AppendLine("[Exception] " + e);
        }
        finally
        {
            Application.logMessageReceived -= capture;
            File.WriteAllText(Folder + "/result.txt", log.ToString());
        }
    }

    private static void Run(string command)
    {
        int space = command.IndexOf(' ');
        string verb = space < 0 ? command : command.Substring(0, space);
        string argument = space < 0 ? "" : command.Substring(space + 1);

        switch (verb)
        {
            case "ping":
                Debug.Log($"pong {Application.dataPath} playing={EditorApplication.isPlaying}");
                break;
            case "menu":
                Debug.Log($"menu {argument}: {EditorApplication.ExecuteMenuItem(argument)}");
                break;
            case "open":
                EditorSceneManager.OpenScene(argument, OpenSceneMode.Single);
                Debug.Log("opened " + argument);
                break;
            case "play":
                EditorApplication.EnterPlaymode();
                break;
            case "stop":
                EditorApplication.ExitPlaymode();
                break;
            case "level":
                General.CurrentLevel = int.Parse(argument);
                UnityEngine.SceneManagement.SceneManager.LoadScene("GamePlay");
                break;
            case "shot":
                Screenshot(argument);
                break;
            case "materials":
                LogVisibleMaterials();
                break;
            default:
                Debug.LogWarning("unknown command " + verb);
                break;
        }
    }

    /// <summary>Renders the main game camera (without UI canvases) into a PNG at the given path.</summary>
    private static void Screenshot(string path)
    {
        var camera = Camera.allCameras.OrderByDescending(c => c.depth).FirstOrDefault();
        if (camera == null)
        {
            Debug.LogWarning("no camera");
            return;
        }

        const int width = 1246, height = 623;
        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        var previousTarget = camera.targetTexture;
        camera.targetTexture = rt;
        camera.Render();
        camera.targetTexture = previousTarget;

        var previousActive = RenderTexture.active;
        RenderTexture.active = rt;
        var image = new Texture2D(width, height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        RenderTexture.active = previousActive;
        File.WriteAllBytes(path, image.EncodeToPNG());
        Object.DestroyImmediate(image);
        rt.Release();
        Object.DestroyImmediate(rt);
        Debug.Log($"shot {camera.name} -> {path}");
    }

    private static void LogVisibleMaterials()
    {
        var names = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
            .Where(r => r.enabled && r.gameObject.activeInHierarchy)
            .SelectMany(r => r.sharedMaterials)
            .Where(m => m != null)
            .Select(m => $"{m.name} [{m.shader.name}]")
            .Distinct()
            .OrderBy(n => n);
        Debug.Log(string.Join("\n", names));
    }
}
