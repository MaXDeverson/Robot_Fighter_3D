using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

public class MainToolbarCheckbox
{
    public static bool CheckboxValue
    {
        get => EditorPrefs.GetBool("MyTool_CheckboxValue", false);
        set => EditorPrefs.SetBool("MyTool_CheckboxValue", value);
    }

    [MainToolbarElement("Project/LoadFromBoot", defaultDockPosition = MainToolbarDockPosition.Left)]
    public static MainToolbarElement FeatureCheckbox()
    {
        var content = new MainToolbarContent("From Boot", "Enable/Disable Feature");
        var checkbox = new MainToolbarToggle(content, CheckboxValue, OnCheckboxValueChanged);

        checkbox.populateContextMenu = (menu) => {
            menu.AppendAction("Reset", _ => {
                CheckboxValue = false;
                MainToolbar.Refresh("Project/From Boot");
                Debug.Log("Checkbox reset to: " + CheckboxValue);
            });
        };

        MainToolbarElementStyler.StyleElement<EditorToolbarToggle>("Project/Enable Feature", element =>
        {
            element.style.paddingLeft = 5f;
            element.style.paddingRight = 5f;
            element.style.marginLeft = 2f;
            element.style.marginRight = 2f;

            var label = element.Q<Label>();
            if (label != null)
            {
                label.style.fontSize = 12f;
                label.style.unityFontStyleAndWeight = FontStyle.Normal;
            }
        });

        return checkbox;
    }

    private static void OnCheckboxValueChanged(bool newValue)
    {
        CheckboxValue = newValue;
    }
}