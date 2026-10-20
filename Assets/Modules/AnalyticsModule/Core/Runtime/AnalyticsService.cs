using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Game.Analytics
{
    /// <summary>
    /// The analytics pipeline: stamps every event, merges the global parameters in, validates it, holds it back while
    /// consent does not allow collection, and fans it out to every sink.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The service knows no backend. Sinks are attached through <see cref="AddSink"/> — the Zenject integration
    /// attaches every bound <see cref="IAnalyticsSink"/> — and one that throws is logged and counted without the
    /// others missing the event. Every piece of state lives in this instance; there is no static way in.
    /// </para>
    /// <para>
    /// Threading: a main thread component. In the editor and in development builds a call from another thread throws;
    /// elsewhere that check is compiled out.
    /// </para>
    /// </remarks>
    public sealed class AnalyticsService : IAnalytics, IAnalyticsSinkRegistry, IAnalyticsDiagnostics, IDisposable
    {
        private readonly AnalyticsSettings _settings;
        private readonly IAnalyticsConsent _consent;
        private readonly IAnalyticsClock _clock;
        private readonly IAnalyticsLogger _logger;
        private readonly AnalyticsEventValidator _validator = new AnalyticsEventValidator();
        private readonly AnalyticsEventFormatter _formatter = new AnalyticsEventFormatter();
        private readonly AnalyticsHistory _history;
        private readonly List<AnalyticsParameter> _globals = new List<AnalyticsParameter>();
        private readonly Queue<AnalyticsRecord> _consentQueue = new Queue<AnalyticsRecord>();
        private readonly int _ownerThreadId;

        // Replaced rather than modified, so a sink added or removed while an event is being sent does not disturb the
        // loop that is sending it.
        private SinkEntry[] _sinks = Array.Empty<SinkEntry>();
        private long _sequence;
        private bool _collectionAllowed;
        private bool _queueOverflowReported;
        private bool _disposed;
        private int _trackedCount;
        private int _sentCount;
        private int _droppedCount;
        private int _rejectedCount;

        /// <summary>
        /// Creates a service.
        /// </summary>
        /// <param name="settings">Tuning. Pass <c>null</c> for the documented defaults.</param>
        /// <param name="consent">The player's answer. Pass <c>null</c> for one kept in memory only.</param>
        /// <param name="clock">Timestamp source. Pass <c>null</c> for a stopwatch started now.</param>
        /// <param name="logger">Where rejected events and failing sinks are reported. Pass <c>null</c> to discard it.</param>
        public AnalyticsService(
            AnalyticsSettings settings,
            IAnalyticsConsent consent,
            IAnalyticsClock clock = null,
            IAnalyticsLogger logger = null)
        {
            _settings = settings ?? new AnalyticsSettings();
            _consent = consent ?? new AnalyticsConsent(_settings, new MemoryAnalyticsStorage());
            _clock = clock ?? new SystemAnalyticsClock();
            _logger = logger ?? new NullAnalyticsLogger();
            _history = _settings.RecordHistory ? new AnalyticsHistory(_settings.HistoryCapacity) : null;
            _ownerThreadId = Thread.CurrentThread.ManagedThreadId;

            _collectionAllowed = _consent.CollectionAllowed;
            _consent.Changed += OnConsentChanged;
        }

        /// <inheritdoc />
        public bool IsEnabled
        {
            get { return _settings.Enabled; }
        }

        /// <inheritdoc />
        public int TrackedCount
        {
            get { return _trackedCount; }
        }

        /// <inheritdoc />
        public int SentCount
        {
            get { return _sentCount; }
        }

        /// <inheritdoc />
        public int QueuedCount
        {
            get { return _consentQueue.Count; }
        }

        /// <inheritdoc />
        public int DroppedCount
        {
            get { return _droppedCount; }
        }

        /// <inheritdoc />
        public int RejectedCount
        {
            get { return _rejectedCount; }
        }

        /// <inheritdoc />
        public IAnalyticsHistory History
        {
            get { return _history; }
        }

        /// <inheritdoc />
        public void Track(AnalyticsEvent analyticsEvent)
        {
            VerifyThread();
            if (analyticsEvent == null)
            {
                throw new ArgumentNullException(nameof(analyticsEvent));
            }

            if (_disposed)
            {
                // Teardown order is not ours to pick: something may still report while its context is being destroyed.
                // There is nowhere left to send the event, and that is not worth an exception in the middle of a quit.
                _logger.LogWarning(
                    "Analytics event '" + analyticsEvent.Name + "' was tracked after the service was disposed and " +
                    "has been discarded.");
                return;
            }

            _trackedCount++;
            AnalyticsEvent stamped = analyticsEvent.Stamp(++_sequence, _clock.Now, _clock.UtcNow, MergeGlobals(analyticsEvent));
            AnalyticsRecord record = new AnalyticsRecord(stamped);
            Record(record);

            if (!_settings.Enabled)
            {
                Drop(record, "Analytics is disabled in the settings.");
                return;
            }

            if (!_validator.TryValidate(stamped, out string problem))
            {
                _rejectedCount++;
                record.Status = AnalyticsDeliveryStatus.Rejected;
                record.Note = problem;
                _logger.LogError("Analytics event '" + stamped.Name + "' was rejected and not sent: " + problem, null);
                return;
            }

            if (_collectionAllowed)
            {
                Dispatch(record);
                return;
            }

            if (_consent.State == AnalyticsConsentState.Denied)
            {
                Drop(record, "The player refused consent.");
                return;
            }

            Enqueue(record);
        }

        /// <inheritdoc />
        public void SetGlobalParameter(string key, AnalyticsValue value)
        {
            VerifyThread();
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("A global parameter needs a key.", nameof(key));
            }

            for (int i = 0; i < _globals.Count; i++)
            {
                if (string.Equals(_globals[i].Key, key, StringComparison.Ordinal))
                {
                    _globals[i] = new AnalyticsParameter(key, value);
                    return;
                }
            }

            _globals.Add(new AnalyticsParameter(key, value));
        }

        /// <inheritdoc />
        public bool RemoveGlobalParameter(string key)
        {
            VerifyThread();
            for (int i = 0; i < _globals.Count; i++)
            {
                if (string.Equals(_globals[i].Key, key, StringComparison.Ordinal))
                {
                    _globals.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public IDisposable AddSink(IAnalyticsSink sink)
        {
            VerifyThread();
            ThrowIfDisposed();
            if (sink == null)
            {
                throw new ArgumentNullException(nameof(sink));
            }

            SinkEntry[] current = _sinks;
            for (int i = 0; i < current.Length; i++)
            {
                if (ReferenceEquals(current[i].Sink, sink))
                {
                    throw new InvalidOperationException(
                        "Analytics sink '" + current[i].Name + "' is already attached. A sink is added once; dispose " +
                        "the token of the first AddSink to remove it.");
                }
            }

            SinkEntry entry = new SinkEntry(this, sink);
            try
            {
                sink.Initialize();
            }
            catch (Exception exception)
            {
                // Kept in the list, so the debugger shows why it is silent, but never called again.
                entry.Deactivate(exception);
                _logger.LogError(
                    "Analytics sink '" + entry.Name + "' failed to initialize and will be skipped. The other sinks " +
                    "are unaffected.", exception);
            }

            SinkEntry[] next = new SinkEntry[current.Length + 1];
            Array.Copy(current, next, current.Length);
            next[current.Length] = entry;
            _sinks = next;

            NotifyCollection(entry, _collectionAllowed);
            return entry;
        }

        /// <summary>
        /// Asks every sink to push out what it buffers. The Zenject integration calls it when the application is
        /// paused or quits — the moments a mobile OS may end the process without another frame.
        /// </summary>
        public void Flush()
        {
            VerifyThread();
            if (_disposed)
            {
                return;
            }

            SinkEntry[] sinks = _sinks;
            for (int i = 0; i < sinks.Length; i++)
            {
                FlushSink(sinks[i]);
            }
        }

        /// <inheritdoc />
        public void CopySinks(List<AnalyticsSinkInfo> buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            buffer.Clear();
            SinkEntry[] sinks = _sinks;
            for (int i = 0; i < sinks.Length; i++)
            {
                SinkEntry entry = sinks[i];
                buffer.Add(new AnalyticsSinkInfo(entry.Name, entry.IsActive, entry.SentCount, entry.FailedCount, entry.LastError));
            }
        }

        /// <inheritdoc />
        public void CopyGlobalParameters(List<AnalyticsParameter> buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            buffer.Clear();
            buffer.AddRange(_globals);
        }

        /// <summary>
        /// Shuts every sink down and discards whatever still waits for consent. Bound through Zenject's
        /// <c>DisposableManager</c>, so tearing the context down closes the backends.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _consent.Changed -= OnConsentChanged;

            SinkEntry[] sinks = _sinks;
            _sinks = Array.Empty<SinkEntry>();
            for (int i = 0; i < sinks.Length; i++)
            {
                sinks[i].Detach();
                ShutDown(sinks[i]);
            }

            while (_consentQueue.Count > 0)
            {
                Drop(_consentQueue.Dequeue(), "The service was disposed before the player answered.");
            }
        }

        private AnalyticsParameter[] MergeGlobals(AnalyticsEvent analyticsEvent)
        {
            AnalyticsParameter[] own = analyticsEvent.ParameterArray;
            if (_globals.Count == 0)
            {
                // Events are immutable, so the draft's array can be shared as is.
                return own;
            }

            List<AnalyticsParameter> merged = new List<AnalyticsParameter>(own.Length + _globals.Count);
            merged.AddRange(own);
            for (int i = 0; i < _globals.Count; i++)
            {
                AnalyticsParameter global = _globals[i];
                if (!analyticsEvent.TryGetParameter(global.Key, out AnalyticsValue _))
                {
                    merged.Add(global);
                }
            }

            return merged.ToArray();
        }

        private void Record(AnalyticsRecord record)
        {
            if (_history == null)
            {
                return;
            }

            // Rendered once here rather than on every repaint of the debugger window.
            record.Headline = _formatter.FormatHeadline(record.Event);
            record.ParametersText = _formatter.FormatParameters(record.Event.Parameters);
            _history.Add(record);
        }

        private void Dispatch(AnalyticsRecord record)
        {
            SinkEntry[] sinks = _sinks;
            int delivered = 0;
            string failed = null;

            for (int i = 0; i < sinks.Length; i++)
            {
                SinkEntry entry = sinks[i];
                if (!entry.IsActive)
                {
                    continue;
                }

                try
                {
                    entry.Sink.Send(record.Event);
                    entry.SentCount++;
                    delivered++;
                }
                catch (Exception exception)
                {
                    entry.ReportFailure(exception);
                    failed = failed == null ? entry.Name : failed + ", " + entry.Name;
                    _logger.LogError(
                        "Analytics sink '" + entry.Name + "' threw while sending '" + record.Event.Name + "'. The " +
                        "other sinks still received it.", exception);
                }
            }

            _sentCount++;
            record.Status = AnalyticsDeliveryStatus.Sent;
            if (failed != null)
            {
                record.Note = "Failed in: " + failed + ".";
            }
            else if (delivered == 0)
            {
                record.Note = "No sink is attached, so the event went nowhere.";
            }
            else
            {
                record.Note = null;
            }
        }

        private void Enqueue(AnalyticsRecord record)
        {
            if (_consentQueue.Count >= _settings.ConsentQueueCapacity)
            {
                Drop(_consentQueue.Dequeue(), "Pushed out of the full consent queue.");
                if (!_queueOverflowReported)
                {
                    _queueOverflowReported = true;
                    _logger.LogWarning(
                        "The analytics consent queue is full (" + _settings.ConsentQueueCapacity + " events). The " +
                        "oldest events are being discarded until the player answers.");
                }
            }

            record.Status = AnalyticsDeliveryStatus.Queued;
            record.Note = "Waiting for the player's consent.";
            _consentQueue.Enqueue(record);
        }

        private void Drop(AnalyticsRecord record, string reason)
        {
            _droppedCount++;
            record.Status = AnalyticsDeliveryStatus.Dropped;
            record.Note = reason;
        }

        private void OnConsentChanged(AnalyticsConsentState state)
        {
            if (_disposed)
            {
                return;
            }

            bool allowed = _consent.CollectionAllowed;
            if (allowed != _collectionAllowed)
            {
                _collectionAllowed = allowed;
                SinkEntry[] sinks = _sinks;
                for (int i = 0; i < sinks.Length; i++)
                {
                    NotifyCollection(sinks[i], allowed);
                }
            }

            if (allowed)
            {
                // Delivered in the order they were tracked, as if they had never waited.
                while (_consentQueue.Count > 0)
                {
                    Dispatch(_consentQueue.Dequeue());
                }

                _queueOverflowReported = false;
            }
            else if (state == AnalyticsConsentState.Denied)
            {
                while (_consentQueue.Count > 0)
                {
                    Drop(_consentQueue.Dequeue(), "The player refused consent.");
                }

                _queueOverflowReported = false;
            }
        }

        private void NotifyCollection(SinkEntry entry, bool allowed)
        {
            if (!entry.IsActive)
            {
                return;
            }

            try
            {
                entry.Sink.SetCollectionEnabled(allowed);
            }
            catch (Exception exception)
            {
                entry.ReportFailure(exception);
                _logger.LogError(
                    "Analytics sink '" + entry.Name + "' threw while being told collection is " +
                    (allowed ? "enabled." : "disabled."), exception);
            }
        }

        private void FlushSink(SinkEntry entry)
        {
            if (!entry.IsActive)
            {
                return;
            }

            try
            {
                entry.Sink.Flush();
            }
            catch (Exception exception)
            {
                entry.ReportFailure(exception);
                _logger.LogError("Analytics sink '" + entry.Name + "' threw while flushing.", exception);
            }
        }

        private void RemoveSink(SinkEntry entry)
        {
            SinkEntry[] current = _sinks;
            int index = Array.IndexOf(current, entry);
            if (index < 0)
            {
                return;
            }

            SinkEntry[] next = new SinkEntry[current.Length - 1];
            Array.Copy(current, 0, next, 0, index);
            Array.Copy(current, index + 1, next, index, current.Length - index - 1);
            _sinks = next;

            ShutDown(entry);
        }

        private void ShutDown(SinkEntry entry)
        {
            FlushSink(entry);
            try
            {
                entry.Sink.Shutdown();
            }
            catch (Exception exception)
            {
                entry.ReportFailure(exception);
                _logger.LogError("Analytics sink '" + entry.Name + "' threw while shutting down.", exception);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AnalyticsService),
                    "This analytics service was disposed together with its container. Resolve the service from the " +
                    "context you are running in instead of holding on to one from a torn down context.");
            }
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private void VerifyThread()
        {
            int current = Thread.CurrentThread.ManagedThreadId;
            if (current == _ownerThreadId)
            {
                return;
            }

            throw new InvalidOperationException(
                "The analytics service was called from thread " + current + " but belongs to thread " +
                _ownerThreadId + ". It is a main thread component: marshal the call back first, for example with " +
                "UniTask.SwitchToMainThread.");
        }

        /// <summary>One attached sink and its counters. Doubles as the token <see cref="AddSink"/> returns.</summary>
        private sealed class SinkEntry : IDisposable
        {
            private AnalyticsService _owner;

            internal SinkEntry(AnalyticsService owner, IAnalyticsSink sink)
            {
                _owner = owner;
                Sink = sink;
                string name = sink.Name;
                Name = string.IsNullOrEmpty(name) ? sink.GetType().Name : name;
                IsActive = true;
            }

            internal IAnalyticsSink Sink { get; }

            internal string Name { get; }

            internal bool IsActive { get; private set; }

            internal int SentCount { get; set; }

            internal int FailedCount { get; private set; }

            internal string LastError { get; private set; }

            public void Dispose()
            {
                AnalyticsService owner = _owner;
                if (owner == null)
                {
                    return;
                }

                _owner = null;
                owner.VerifyThread();
                owner.RemoveSink(this);
            }

            internal void ReportFailure(Exception exception)
            {
                FailedCount++;
                LastError = exception.GetType().Name + ": " + exception.Message;
            }

            internal void Deactivate(Exception exception)
            {
                ReportFailure(exception);
                IsActive = false;
            }

            /// <summary>The service is gone: disposing the token from now on does nothing.</summary>
            internal void Detach()
            {
                _owner = null;
            }
        }
    }
}
