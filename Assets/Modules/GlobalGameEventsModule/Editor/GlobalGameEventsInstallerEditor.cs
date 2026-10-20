using System.Collections.Generic;
using Game.Events.DI;
using UnityEditor;
using UnityEngine;

namespace Game.Events.EditorTools
{
    /// <summary>
    /// Adds a validation pass and a shortcut to the debugger on the installer's inspector.
    /// </summary>
    /// <remarks>
    /// Both buttons run from instance code, which is what lets the module get by with a single static method in
    /// the whole codebase. See <see cref="GameEventsDebuggerMenu"/>.
    /// </remarks>
    [CustomEditor(typeof(GlobalGameEventsInstaller))]
    internal sealed class GlobalGameEventsInstallerEditor : UnityEditor.Editor
    {
        private readonly List<string> _problems = new List<string>();

        private string _lastResult;
        private MessageType _lastResultType = MessageType.None;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            if (GUILayout.Button("Validate Event Paths"))
            {
                Validate();
            }

            if (GUILayout.Button("Open Global Game Events Debugger"))
            {
                EditorWindow.GetWindow<GameEventsDebuggerWindow>().Show();
            }

            if (_lastResultType != MessageType.None && !string.IsNullOrEmpty(_lastResult))
            {
                EditorGUILayout.HelpBox(_lastResult, _lastResultType);
            }
        }

        private void Validate()
        {
            EventTypeScanner scanner = new EventTypeScanner();
            EventPathRegistry registry = new EventPathRegistry();
            IReadOnlyList<System.Type> eventTypes = scanner.FindEventTypes();

            if (registry.TryValidate(eventTypes, _problems))
            {
                _lastResult = eventTypes.Count + " event type(s) checked, every path is unique and well formed.";
                _lastResultType = MessageType.Info;
                return;
            }

            _lastResult = _problems.Count + " problem(s) found; see the console.";
            _lastResultType = MessageType.Error;

            for (int i = 0; i < _problems.Count; i++)
            {
                Debug.LogError("[GameEvents] " + _problems[i]);
            }
        }
    }
}
