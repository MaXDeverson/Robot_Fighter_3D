using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Zenject;

namespace Game.Events.EditorTools
{
    /// <summary>
    /// Shows the live event stream and the live subscription list of every running context.
    /// </summary>
    /// <remarks>
    /// The window never reaches for a global service, because there is none. It looks for the contexts that happen
    /// to be alive, asks each one's container for <see cref="IEventHistory"/> and
    /// <see cref="IGameEventsDiagnostics"/>, and shows whatever it gets. A context that inherits its service from a
    /// parent is labelled as such, which is the quickest way to spot an accidental second service.
    /// </remarks>
    internal sealed class GameEventsDebuggerWindow : EditorWindow
    {
        private const float PathColumnWidth = 190f;
        private const float TimeColumnWidth = 66f;
        private const float PriorityColumnWidth = 58f;
        private const double ContextRefreshInterval = 1d;

        private readonly List<ContextEntry> _contexts = new List<ContextEntry>();
        private readonly List<SubscriptionInfo> _subscriptions = new List<SubscriptionInfo>();
        private readonly List<string> _groupOrder = new List<string>();
        private readonly Dictionary<string, int> _groupCounts = new Dictionary<string, int>();

        [SerializeField] private int _selectedContext;
        [SerializeField] private string _branchFilter = string.Empty;
        [SerializeField] private bool _showSubscriptions = true;
        [SerializeField] private Vector2 _eventScroll;
        [SerializeField] private Vector2 _subscriptionScroll;

        private string[] _filterSegments;
        private string _filterSource;
        private double _nextContextRefresh;

        private void OnEnable()
        {
            titleContent = new GUIContent("Game Events");
            minSize = new Vector2(460f, 260f);
        }

        private void Update()
        {
            if (!EditorApplication.isPlaying)
            {
                if (_contexts.Count > 0)
                {
                    _contexts.Clear();
                    Repaint();
                }

                return;
            }

            if (EditorApplication.timeSinceStartup >= _nextContextRefresh)
            {
                _nextContextRefresh = EditorApplication.timeSinceStartup + ContextRefreshInterval;
                RefreshContexts();
            }

            Repaint();
        }

        private void OnGUI()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Enter Play Mode to inspect an event service. The service lives in a Zenject container, so there is " +
                    "nothing to read while the game is stopped.",
                    MessageType.Info);
                return;
            }

            if (_contexts.Count == 0)
            {
                RefreshContexts();
            }

            if (_contexts.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No Zenject context is running. Add a GlobalGameEventsInstaller to your ProjectContext prefab or " +
                    "to a SceneContext in the open scene.",
                    MessageType.Warning);
                return;
            }

            DrawToolbar();

            _selectedContext = Mathf.Clamp(_selectedContext, 0, _contexts.Count - 1);
            ContextEntry entry = _contexts[_selectedContext];

            if (entry.Diagnostics == null)
            {
                EditorGUILayout.HelpBox(
                    "This context has no event service. Add a GlobalGameEventsInstaller to it, or pick another context " +
                    "above.",
                    MessageType.Warning);
                return;
            }

            DrawSummary(entry);
            DrawEvents(entry);

            if (_showSubscriptions)
            {
                DrawSubscriptions(entry);
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            string[] names = new string[_contexts.Count];
            for (int i = 0; i < _contexts.Count; i++)
            {
                names[i] = _contexts[i].Label;
            }

            _selectedContext = EditorGUILayout.Popup(_selectedContext, names, EditorStyles.toolbarPopup, GUILayout.Width(220f));

            GUILayout.Label("Branch", EditorStyles.miniLabel, GUILayout.Width(46f));
            _branchFilter = EditorGUILayout.TextField(_branchFilter, EditorStyles.toolbarTextField, GUILayout.Width(150f));

            GUILayout.FlexibleSpace();

            _showSubscriptions = GUILayout.Toggle(_showSubscriptions, "Subscriptions", EditorStyles.toolbarButton, GUILayout.Width(96f));

            ContextEntry entry = _contexts[Mathf.Clamp(_selectedContext, 0, _contexts.Count - 1)];
            using (new EditorGUI.DisabledScope(entry.History == null))
            {
                if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50f)) && entry.History != null)
                {
                    entry.History.Clear();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSummary(ContextEntry entry)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label(entry.OwnsEvents ? "Owns the service" : "Inherits the service from a parent context", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label(
                "subscriptions " + entry.Diagnostics.SubscriptionCount +
                "   queued " + entry.Diagnostics.PendingEventCount +
                "   depth " + entry.Diagnostics.DispatchDepth,
                EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEvents(ContextEntry entry)
        {
            GUILayout.Label("Events (newest first)", EditorStyles.boldLabel);

            if (entry.History == null)
            {
                EditorGUILayout.HelpBox(
                    "History recording is off for this service. Turn on 'Record History' on the GlobalGameEventsInstaller " +
                    "or its config asset to see the event stream here.",
                    MessageType.Info);
                return;
            }

            UpdateFilterSegments();

            float height = _showSubscriptions ? position.height * 0.45f : position.height - 96f;
            _eventScroll = EditorGUILayout.BeginScrollView(_eventScroll, GUILayout.Height(Mathf.Max(80f, height)));

            int shown = 0;
            for (int i = entry.History.Count - 1; i >= 0; i--)
            {
                EventRecord record = entry.History[i];
                if (!MatchesFilter(record.Path))
                {
                    continue;
                }

                shown++;
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(record.Time.ToString("0.00"), EditorStyles.miniLabel, GUILayout.Width(TimeColumnWidth));
                GUILayout.Label(record.Path, EditorStyles.miniBoldLabel, GUILayout.Width(PathColumnWidth));
                GUILayout.Label(record.Payload, EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }

            if (shown == 0)
            {
                GUILayout.Label(
                    entry.History.Count == 0 ? "Nothing published yet." : "No event matches this branch filter.",
                    EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSubscriptions(ContextEntry entry)
        {
            entry.Diagnostics.CopySubscriptions(_subscriptions);

            _groupCounts.Clear();
            _groupOrder.Clear();
            for (int i = 0; i < _subscriptions.Count; i++)
            {
                string key = (_subscriptions[i].IsBranch ? "branch " : "") + _subscriptions[i].Path;
                if (_groupCounts.TryGetValue(key, out int count))
                {
                    _groupCounts[key] = count + 1;
                }
                else
                {
                    _groupCounts.Add(key, 1);
                    _groupOrder.Add(key);
                }
            }

            _groupOrder.Sort();

            GUILayout.Label("Subscriptions (" + _subscriptions.Count + ")", EditorStyles.boldLabel);
            _subscriptionScroll = EditorGUILayout.BeginScrollView(_subscriptionScroll);

            for (int g = 0; g < _groupOrder.Count; g++)
            {
                string key = _groupOrder[g];
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(key, EditorStyles.miniBoldLabel, GUILayout.Width(PathColumnWidth + TimeColumnWidth));
                GUILayout.Label("x" + _groupCounts[key], EditorStyles.miniLabel, GUILayout.Width(36f));
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel++;
                for (int i = 0; i < _subscriptions.Count; i++)
                {
                    SubscriptionInfo info = _subscriptions[i];
                    string infoKey = (info.IsBranch ? "branch " : "") + info.Path;
                    if (infoKey != key)
                    {
                        continue;
                    }

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(18f);
                    GUILayout.Label("p" + info.Priority, EditorStyles.miniLabel, GUILayout.Width(PriorityColumnWidth));
                    GUILayout.Label(info.Target, EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUI.indentLevel--;
            }

            if (_subscriptions.Count == 0)
            {
                GUILayout.Label("Nothing is subscribed.", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndScrollView();
        }

        private void RefreshContexts()
        {
            _contexts.Clear();

            if (ProjectContext.HasInstance)
            {
                AddContext("ProjectContext", ProjectContext.Instance.Container);
            }

            SceneContext[] sceneContexts = FindObjectsByType<SceneContext>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < sceneContexts.Length; i++)
            {
                SceneContext sceneContext = sceneContexts[i];
                if (sceneContext == null)
                {
                    continue;
                }

                AddContext(sceneContext.gameObject.scene.name + " (SceneContext)", sceneContext.Container);
            }
        }

        private void AddContext(string label, DiContainer container)
        {
            if (container == null)
            {
                return;
            }

            _contexts.Add(new ContextEntry(
                label,
                container.HasBindingId(typeof(IGlobalGameEvents), null, InjectSources.Local),
                container.TryResolve<IGameEventsDiagnostics>(),
                container.TryResolve<IEventHistory>()));
        }

        private void UpdateFilterSegments()
        {
            if (_filterSource == _branchFilter)
            {
                return;
            }

            _filterSource = _branchFilter;
            string trimmed = _branchFilter == null ? string.Empty : _branchFilter.Trim();
            _filterSegments = trimmed.Length == 0 || trimmed == EventPath.Wildcard
                ? null
                : trimmed.TrimEnd('*', EventPath.Separator).Split(EventPath.Separator);
        }

        /// <summary>
        /// Segment-wise branch test, written by hand so that filtering a full history buffer on every repaint
        /// does not parse a path per row. Matches <see cref="EventPath.IsUnder"/>: "Level" accepts
        /// "Level.Entered" but never "LevelUp.Something".
        /// </summary>
        private bool MatchesFilter(string path)
        {
            if (_filterSegments == null || _filterSegments.Length == 0)
            {
                return true;
            }

            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            int position = 0;
            for (int i = 0; i < _filterSegments.Length; i++)
            {
                string segment = _filterSegments[i];
                if (segment.Length == 0)
                {
                    return false;
                }

                int end = position + segment.Length;
                if (end > path.Length)
                {
                    return false;
                }

                if (string.CompareOrdinal(path, position, segment, 0, segment.Length) != 0)
                {
                    return false;
                }

                if (end < path.Length && path[end] != EventPath.Separator)
                {
                    return false;
                }

                position = end + 1;
            }

            return true;
        }

        private sealed class ContextEntry
        {
            internal ContextEntry(string label, bool ownsEvents, IGameEventsDiagnostics diagnostics, IEventHistory history)
            {
                Label = label;
                OwnsEvents = ownsEvents;
                Diagnostics = diagnostics;
                History = history;
            }

            internal string Label { get; }

            internal bool OwnsEvents { get; }

            internal IGameEventsDiagnostics Diagnostics { get; }

            internal IEventHistory History { get; }
        }
    }
}
