using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using Zenject;

namespace Game.Analytics.EditorTools
{
    /// <summary>
    /// Shows the analytics events of every running context as they are tracked — what became of each, the sinks they
    /// went to, the global parameters and the consent state.
    /// </summary>
    /// <remarks>
    /// Works like the Game Events Debugger next to it. There is no global service to reach for, so the window finds the
    /// contexts that happen to be alive, asks each one's container for <see cref="IAnalyticsDiagnostics"/> and
    /// <see cref="IAnalyticsConsent"/>, and shows whatever it gets. A context that inherits its service from a parent is
    /// labelled as such.
    /// </remarks>
    internal sealed class AnalyticsDebuggerWindow : EditorWindow
    {
        private const float TimeColumnWidth = 52f;
        private const float SequenceColumnWidth = 40f;
        private const float StatusColumnWidth = 62f;
        private const float NameColumnWidth = 170f;
        private const float DetailsIndent = TimeColumnWidth + SequenceColumnWidth + StatusColumnWidth + 12f;
        private const double ContextRefreshInterval = 1d;

        // In the order of AnalyticsCategory, after "All kinds": the filter compares the index with the member's value.
        private static readonly string[] CategoryLabels = { "All kinds", "Design", "Progression", "Ad" };

        private readonly List<ContextEntry> _contexts = new List<ContextEntry>();
        private readonly List<AnalyticsSinkInfo> _sinks = new List<AnalyticsSinkInfo>();
        private readonly List<AnalyticsParameter> _globals = new List<AnalyticsParameter>();
        private readonly AnalyticsEventFormatter _formatter = new AnalyticsEventFormatter();

        [SerializeField] private int _selectedContext;
        [SerializeField] private string _nameFilter = string.Empty;
        [SerializeField] private int _categoryFilter;
        [SerializeField] private bool _problemsOnly;
        [SerializeField] private bool _showSinks = true;
        [SerializeField] private Vector2 _eventScroll;
        [SerializeField] private Vector2 _sinkScroll;

        private double _nextContextRefresh;
        private Styles _styles;

        /// <summary>Opens the window. Also reachable from the AnalyticsInstaller inspector.</summary>
        [MenuItem("Window/Analytics Debugger")]
        internal static void Open()
        {
            GetWindow<AnalyticsDebuggerWindow>().Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Analytics");
            minSize = new Vector2(560f, 280f);
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
            if (_styles == null)
            {
                _styles = new Styles();
            }

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Enter Play Mode to inspect the analytics service. It lives in a Zenject container, so there is " +
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
                    "No Zenject context is running. Add an AnalyticsInstaller to your ProjectContext prefab.",
                    MessageType.Warning);
                return;
            }

            _selectedContext = Mathf.Clamp(_selectedContext, 0, _contexts.Count - 1);
            ContextEntry entry = _contexts[_selectedContext];

            DrawToolbar(entry);

            if (entry.Diagnostics == null)
            {
                EditorGUILayout.HelpBox(
                    "This context has no analytics service. Add an AnalyticsInstaller to it, or pick another context " +
                    "above.",
                    MessageType.Warning);
                return;
            }

            DrawSummary(entry);
            DrawEvents(entry);

            if (_showSinks)
            {
                DrawSinks(entry);
            }
        }

        private void DrawToolbar(ContextEntry entry)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            string[] names = new string[_contexts.Count];
            for (int i = 0; i < _contexts.Count; i++)
            {
                names[i] = _contexts[i].Label;
            }

            _selectedContext = EditorGUILayout.Popup(_selectedContext, names, EditorStyles.toolbarPopup, GUILayout.Width(200f));
            _nameFilter = EditorGUILayout.TextField(_nameFilter, EditorStyles.toolbarSearchField, GUILayout.Width(150f));
            _categoryFilter = EditorGUILayout.Popup(_categoryFilter, CategoryLabels, EditorStyles.toolbarPopup, GUILayout.Width(90f));
            _problemsOnly = GUILayout.Toggle(
                _problemsOnly,
                new GUIContent("Problems", "Only events that were not sent cleanly: queued, dropped, rejected, or sent with a note."),
                EditorStyles.toolbarButton,
                GUILayout.Width(64f));

            GUILayout.FlexibleSpace();

            _showSinks = GUILayout.Toggle(_showSinks, "Sinks", EditorStyles.toolbarButton, GUILayout.Width(48f));

            IAnalyticsHistory history = entry.Diagnostics != null ? entry.Diagnostics.History : null;
            using (new EditorGUI.DisabledScope(history == null))
            {
                if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(44f)) && history != null)
                {
                    history.Clear();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSummary(ContextEntry entry)
        {
            IAnalyticsDiagnostics diagnostics = entry.Diagnostics;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(
                entry.OwnsService ? "Owns the service" : "Inherits the service from a parent context",
                EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label(
                (diagnostics.IsEnabled ? string.Empty : "DISABLED   ") +
                "tracked " + diagnostics.TrackedCount +
                "   sent " + diagnostics.SentCount +
                "   queued " + diagnostics.QueuedCount +
                "   dropped " + diagnostics.DroppedCount +
                "   rejected " + diagnostics.RejectedCount,
                EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            if (entry.Consent != null)
            {
                DrawConsent(entry.Consent);
            }

            diagnostics.CopyGlobalParameters(_globals);
            GUILayout.Label(
                "Global parameters: " + (_globals.Count == 0 ? "none" : _formatter.FormatParameters(_globals)),
                EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
        }

        private void DrawConsent(IAnalyticsConsent consent)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(
                "Consent " + consent.State + (consent.IsRequired ? " (required)" : " (not required)") +
                (consent.CollectionAllowed ? " — collecting" : " — not collecting"),
                EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();

            const string savedTooltip = "Saved like the player's own answer. Reset forgets it.";
            if (GUILayout.Button(new GUIContent("Grant", savedTooltip), EditorStyles.miniButtonLeft, GUILayout.Width(50f)))
            {
                consent.Grant();
            }

            if (GUILayout.Button(new GUIContent("Deny", savedTooltip), EditorStyles.miniButtonMid, GUILayout.Width(50f)))
            {
                consent.Deny();
            }

            if (GUILayout.Button(new GUIContent("Reset", savedTooltip), EditorStyles.miniButtonRight, GUILayout.Width(50f)))
            {
                consent.Reset();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawEvents(ContextEntry entry)
        {
            GUILayout.Label("Events (newest first)", EditorStyles.boldLabel);

            IAnalyticsHistory history = entry.Diagnostics.History;
            if (history == null)
            {
                EditorGUILayout.HelpBox(
                    "Recording is off for this service. Turn on 'Record History' in the AnalyticsConfig to see the " +
                    "event stream here; it is only ever recorded in the editor and in development builds.",
                    MessageType.Info);
                return;
            }

            float height = _showSinks ? position.height * 0.55f : position.height - 140f;
            _eventScroll = EditorGUILayout.BeginScrollView(_eventScroll, GUILayout.Height(Mathf.Max(80f, height)));

            int shown = 0;
            for (int i = history.Count - 1; i >= 0; i--)
            {
                AnalyticsRecord record = history[i];
                if (!Matches(record))
                {
                    continue;
                }

                shown++;
                DrawRecord(record);
            }

            if (shown == 0)
            {
                GUILayout.Label(
                    history.Count == 0 ? "Nothing tracked yet." : "No event matches the filter.",
                    EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawRecord(AnalyticsRecord record)
        {
            AnalyticsEvent analyticsEvent = record.Event;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(
                analyticsEvent.Time.ToString("0.00", CultureInfo.InvariantCulture),
                EditorStyles.miniLabel,
                GUILayout.Width(TimeColumnWidth));
            GUILayout.Label(
                "#" + analyticsEvent.Sequence.ToString(CultureInfo.InvariantCulture),
                EditorStyles.miniLabel,
                GUILayout.Width(SequenceColumnWidth));
            GUILayout.Label(record.Status.ToString(), _styles.ForStatus(record.Status), GUILayout.Width(StatusColumnWidth));
            GUILayout.Label(analyticsEvent.Name, EditorStyles.miniBoldLabel, GUILayout.Width(NameColumnWidth));
            GUILayout.Label(record.Headline, EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (!string.IsNullOrEmpty(analyticsEvent.Source))
            {
                GUILayout.Label(analyticsEvent.Source, EditorStyles.miniLabel);
            }

            EditorGUILayout.EndHorizontal();

            bool hasParameters = !string.IsNullOrEmpty(record.ParametersText);
            bool hasNote = !string.IsNullOrEmpty(record.Note);
            if (!hasParameters && !hasNote)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(DetailsIndent);
            EditorGUILayout.BeginVertical();
            if (hasParameters)
            {
                GUILayout.Label(record.ParametersText, _styles.Details);
            }

            if (hasNote)
            {
                GUILayout.Label(record.Note, _styles.Note);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSinks(ContextEntry entry)
        {
            entry.Diagnostics.CopySinks(_sinks);

            GUILayout.Label("Sinks (" + _sinks.Count + ")", EditorStyles.boldLabel);
            _sinkScroll = EditorGUILayout.BeginScrollView(_sinkScroll);

            for (int i = 0; i < _sinks.Count; i++)
            {
                AnalyticsSinkInfo sink = _sinks[i];
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(sink.Name, EditorStyles.miniBoldLabel, GUILayout.Width(NameColumnWidth));
                GUILayout.Label(
                    sink.IsActive ? "active" : "failed to initialize",
                    sink.IsActive ? EditorStyles.miniLabel : _styles.Rejected,
                    GUILayout.Width(110f));
                GUILayout.Label(
                    "sent " + sink.SentCount + "   failed " + sink.FailedCount,
                    EditorStyles.miniLabel,
                    GUILayout.Width(140f));
                if (!string.IsNullOrEmpty(sink.LastError))
                {
                    GUILayout.Label(sink.LastError, _styles.Note);
                }

                EditorGUILayout.EndHorizontal();
            }

            if (_sinks.Count == 0)
            {
                GUILayout.Label(
                    "No sink is attached: events are tracked and recorded here, but go nowhere. Bind an IAnalyticsSink, " +
                    "or set Console Output in the AnalyticsConfig.",
                    _styles.Details);
            }

            EditorGUILayout.EndScrollView();
        }

        private bool Matches(AnalyticsRecord record)
        {
            AnalyticsEvent analyticsEvent = record.Event;

            if (_categoryFilter > 0 && (int)analyticsEvent.Category != _categoryFilter - 1)
            {
                return false;
            }

            if (_problemsOnly && record.Status == AnalyticsDeliveryStatus.Sent && string.IsNullOrEmpty(record.Note))
            {
                return false;
            }

            string filter = _nameFilter == null ? string.Empty : _nameFilter.Trim();
            if (filter.Length == 0)
            {
                return true;
            }

            return Contains(analyticsEvent.Name, filter) ||
                   Contains(analyticsEvent.HierarchyPath, filter) ||
                   Contains(analyticsEvent.Source, filter);
        }

        private static bool Contains(string text, string part)
        {
            return text != null && text.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void RefreshContexts()
        {
            _contexts.Clear();

            if (ProjectContext.HasInstance)
            {
                AddContext("ProjectContext", ProjectContext.Instance.Container);
            }

            SceneContext[] sceneContexts = FindObjectsByType<SceneContext>(FindObjectsSortMode.None);
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
                container.HasBindingId(typeof(IAnalytics), null, InjectSources.Local),
                container.TryResolve<IAnalyticsDiagnostics>(),
                container.TryResolve<IAnalyticsConsent>()));
        }

        private sealed class ContextEntry
        {
            internal ContextEntry(string label, bool ownsService, IAnalyticsDiagnostics diagnostics, IAnalyticsConsent consent)
            {
                Label = label;
                OwnsService = ownsService;
                Diagnostics = diagnostics;
                Consent = consent;
            }

            internal string Label { get; }

            internal bool OwnsService { get; }

            internal IAnalyticsDiagnostics Diagnostics { get; }

            internal IAnalyticsConsent Consent { get; }
        }

        /// <summary>The window's text styles, built on first draw, when the editor styles exist.</summary>
        private sealed class Styles
        {
            internal Styles()
            {
                bool dark = EditorGUIUtility.isProSkin;
                Sent = StatusStyle(dark ? new Color(0.45f, 0.8f, 0.45f) : new Color(0.1f, 0.45f, 0.1f));
                Queued = StatusStyle(dark ? new Color(0.95f, 0.8f, 0.3f) : new Color(0.6f, 0.45f, 0f));
                Dropped = StatusStyle(dark ? new Color(0.6f, 0.6f, 0.6f) : new Color(0.4f, 0.4f, 0.4f));
                Rejected = StatusStyle(dark ? new Color(1f, 0.45f, 0.45f) : new Color(0.7f, 0.1f, 0.1f));
                Details = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
                Note = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true, fontStyle = FontStyle.Italic };
            }

            internal GUIStyle Sent { get; }

            internal GUIStyle Queued { get; }

            internal GUIStyle Dropped { get; }

            internal GUIStyle Rejected { get; }

            internal GUIStyle Details { get; }

            internal GUIStyle Note { get; }

            internal GUIStyle ForStatus(AnalyticsDeliveryStatus status)
            {
                switch (status)
                {
                    case AnalyticsDeliveryStatus.Queued:
                        return Queued;
                    case AnalyticsDeliveryStatus.Dropped:
                        return Dropped;
                    case AnalyticsDeliveryStatus.Rejected:
                        return Rejected;
                    default:
                        return Sent;
                }
            }

            private static GUIStyle StatusStyle(Color color)
            {
                GUIStyle style = new GUIStyle(EditorStyles.miniBoldLabel);
                style.normal.textColor = color;
                return style;
            }
        }
    }
}
