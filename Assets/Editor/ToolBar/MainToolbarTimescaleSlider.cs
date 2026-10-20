using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

public class MainToolbarTimescaleSlider
{
    const float k_minTimeScale = 0f;
    const float k_maxTimeScale = 5f;

    [MainToolbarElement("Timescale/Slider", defaultDockPosition = MainToolbarDockPosition.Middle)]
    public static MainToolbarElement TimeSlider()
    {
        var content = new MainToolbarContent("Time Scale", "Time Scale");
        var slider = new MainToolbarSlider(content, Time.timeScale, k_minTimeScale, k_maxTimeScale, OnSliderValueChanged);

        slider.populateContextMenu = (menu) => {
            menu.AppendAction("Reset", _ => {
                Time.timeScale = 1f;
                MainToolbar.Refresh("Timescale/Slider");
            });
        };

        MainToolbarElementStyler.StyleElement<VisualElement>("Timescale/Slider", (element) => {
            element.style.paddingLeft = 10f;
        });

        return slider;
    }

    static void OnSliderValueChanged(float newValue)
    {
        Time.timeScale = newValue;
    }
}