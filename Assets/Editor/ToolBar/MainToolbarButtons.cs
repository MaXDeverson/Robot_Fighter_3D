using System.IO;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

public class MainToolbarButtons
{
    [MainToolbarElement("Project/Open Project Settings", defaultDockPosition = MainToolbarDockPosition.Left)]
    public static MainToolbarElement ProjectSettingsButton()
    {
        var icon = EditorGUIUtility.IconContent("SettingsIcon").image as Texture2D;
        var content = new MainToolbarContent(icon);
        return new MainToolbarButton(content, () => { SettingsService.OpenProjectSettings(); });
    }

    [MainToolbarElement("Timescale/Reset", defaultDockPosition = MainToolbarDockPosition.Middle)]
    public static MainToolbarElement ResetTimeScaleButton()
    {
        var icon = EditorGUIUtility.IconContent("Refresh").image as Texture2D;
        var content = new MainToolbarContent(icon, "Reset");
        var button = new MainToolbarButton(content, () => {
            Time.timeScale = 1f;
            MainToolbar.Refresh("Timescale/Slider");
        });

        MainToolbarElementStyler.StyleElement<UnityEditor.Toolbars.EditorToolbarButton>("Timescale/Reset", element =>
        {
            element.style.paddingLeft = 0f;
            element.style.paddingRight = 0f;
            element.style.marginLeft = 0f;
            element.style.marginRight = 0f;
            element.style.minWidth = 20f;
            element.style.maxWidth = 20f;

            var image = element.Q<Image>();
            if (image != null)
            {
                image.style.width = 12f;
                image.style.height = 12f;
            }
        });

        return button;
    }

    [MainToolbarElement("Project/ResetSavings", defaultDockPosition = MainToolbarDockPosition.Left)]
    public static MainToolbarElement ResetSavingsScaleButton()
    {
        var icon = EditorGUIUtility.IconContent("TreeEditor.Trash").image as Texture2D;
        var content = new MainToolbarContent(icon, "Reset Savings");
        var button = new MainToolbarButton(content, () => {
            Debug.LogError("Reset savings");
            PlayerPrefs.DeleteAll();
            string[] files = Directory.GetFiles(Application.persistentDataPath);
            foreach (string file in files)
            {
                File.Delete(file);
            }
        });

        MainToolbarElementStyler.StyleElement<UnityEditor.Toolbars.EditorToolbarButton>("Project/ResetSavings", element =>
        {
            // Style for button with text
            element.style.paddingLeft = 4f;
            element.style.paddingRight = 4f;
            element.style.marginLeft = 2f;
            element.style.marginRight = 2f;
            element.style.minWidth = StyleKeyword.Auto;
            element.style.maxWidth = StyleKeyword.None;

            var image = element.Q<Image>();
            if (image != null)
            {
                image.style.width = 15f;
                image.style.height = 15f;
                image.style.marginRight = 4f; // Space between icon and text
            }
        });

        return button;
    }
}