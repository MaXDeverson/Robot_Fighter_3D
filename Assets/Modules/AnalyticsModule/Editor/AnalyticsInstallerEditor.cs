using Game.Analytics.DI;
using UnityEditor;
using UnityEngine;

namespace Game.Analytics.EditorTools
{
    /// <summary>Adds a shortcut to the analytics debugger on the installer's inspector.</summary>
    [CustomEditor(typeof(AnalyticsInstaller))]
    internal sealed class AnalyticsInstallerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            if (GUILayout.Button("Open Analytics Debugger"))
            {
                AnalyticsDebuggerWindow.Open();
            }
        }
    }
}
