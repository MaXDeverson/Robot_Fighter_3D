using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityUtils;

public static class MainToolbarElementStyler
{
    public static void StyleElement<T>(string elementName, System.Action<T> styleAction) where T : VisualElement
    {
        EditorApplication.delayCall += () => {
            ApplyStyle(elementName, (element) => {
                T targetElement = null;

                if (element is T typedElement)
                {
                    targetElement = typedElement;
                }
                else
                {
                    targetElement = element.Query<T>().First();
                }

                if (targetElement != null)
                {
                    styleAction(targetElement);
                }
            });
        };
    }

    static void ApplyStyle(string elementName, System.Action<VisualElement> styleCallback)
    {
        var element = FindElementByName(elementName);
        if (element != null)
        {
            styleCallback(element);
        }
    }

    static VisualElement FindElementByName(string name)
    {
        var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
        foreach (var window in windows)
        {
            var root = window.rootVisualElement;
            if (root == null) continue;

            VisualElement element;
            if ((element = root.FindElementByName(name)) != null) return element;
            if ((element = root.FindElementByTooltip(name)) != null) return element;
        }
        return null;
    }
}