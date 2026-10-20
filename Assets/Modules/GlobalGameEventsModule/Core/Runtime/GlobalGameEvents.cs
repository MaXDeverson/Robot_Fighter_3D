using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Game.Events
{
    /// <summary>
    /// The default event service: immediate and deferred publishing, typed and branch subscriptions, observers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every piece of state lives in this instance. Two services built by two containers share nothing, which is
    /// what makes a scene scoped service and a project scoped service able to coexist and what keeps tests isolated.
    /// </para>
    /// <para>
    /// Threading: the service captures the identity of the thread that constructed it and, in the editor and in
    /// development builds, refuses calls from any other thread. Outside those builds the check is compiled away,
    /// so publishing from a worker thread is undefined behaviour rather than a caught error.
    /// </para>
    /// <para>
    /// Allocation: a typed publish with no observers and no matching branch handler allocates nothing. The event
    /// is boxed at most once per dispatch, and only when an observer or a branch handler actually needs it.
    /// </para>
    /// </remarks>
    public sealed class GlobalGameEvents : IGlobalGameEvents, IGameEventsDiagnostics, IDisposable
    {
        private readonly GameEventsSettings _settings;
        private readonly IEventPathRegistry _registry;
        private readonly IGameEventsLogger _logger;

        private readonly Dictionary<Type, ITypedChannel> _typedChannels = new Dictionary<Type, ITypedChannel>();
        private readonly Dictionary<Type, BranchSubscription[]> _branchMatchCache = new Dictionary<Type, BranchSubscription[]>();
        private readonly Dictionary<Type, IEventQueueChannel> _queueChannels = new Dictionary<Type, IEventQueueChannel>();
        private readonly Queue<IEventQueueChannel> _queueOrder = new Queue<IEventQueueChannel>();
        private readonly List<BranchSubscription> _branchMatchBuffer = new List<BranchSubscription>();
        private readonly List<string> _dispatchChain;
        private readonly int _ownerThreadId;

        private BranchSubscription[] _branches = new BranchSubscription[0];
        private ObserverSubscription[] _observers = new ObserverSubscription[0];
        private int _nextOrder;
        private int _dispatchDepth;
        private bool _flushing;
        private bool _disposed;

        /// <summary>
        /// Creates a service.
        /// </summary>
        /// <param name="settings">Tuning knobs. Pass <c>null</c> for the documented defaults.</param>
        /// <param name="registry">
        /// Path cache. Pass <c>null</c> to get a private one. Sharing a registry between services is safe and saves
        /// the reflection pass, but is not required.
        /// </param>
        /// <param name="logger">Where failures and flow logging go. Pass <c>null</c> to discard both.</param>
        public GlobalGameEvents(GameEventsSettings settings, IEventPathRegistry registry, IGameEventsLogger logger)
        {
            _settings = settings ?? new GameEventsSettings();
            _registry = registry ?? new EventPathRegistry();
            _logger = logger ?? new NullGameEventsLogger();
            _dispatchChain = new List<string>(_settings.MaxDispatchDepth + 1);
            _ownerThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        /// <inheritdoc />
        public int PendingCount
        {
            get { return _queueOrder.Count; }
        }

        /// <inheritdoc />
        public int PendingEventCount
        {
            get { return _queueOrder.Count; }
        }

        /// <inheritdoc />
        public int DispatchDepth
        {
            get { return _dispatchDepth; }
        }

        /// <inheritdoc />
        public int SubscriptionCount
        {
            get
            {
                int total = 0;
                foreach (KeyValuePair<Type, ITypedChannel> pair in _typedChannels)
                {
                    total += pair.Value.ActiveCount;
                }

                BranchSubscription[] branches = _branches;
                for (int i = 0; i < branches.Length; i++)
                {
                    if (branches[i].IsActive)
                    {
                        total++;
                    }
                }

                return total;
            }
        }

        /// <inheritdoc />
        public void Publish<T>(T gameEvent) where T : struct, IGameEvent
        {
            VerifyThread();
            ThrowIfDisposed();
            EventPath path = _registry.GetPath(typeof(T));
            Dispatch(path, gameEvent, false);
        }

        /// <inheritdoc />
        public void Enqueue<T>(T gameEvent) where T : struct, IGameEvent
        {
            VerifyThread();
            ThrowIfDisposed();

            // Resolving the path now means a missing or malformed [EventPath] is reported at the call site that
            // produced the event rather than a frame later inside the flush, where the stack is useless.
            _registry.GetPath(typeof(T));

            Type key = typeof(T);
            EventQueueChannel<T> channel;
            if (_queueChannels.TryGetValue(key, out IEventQueueChannel existing))
            {
                channel = (EventQueueChannel<T>)existing;
            }
            else
            {
                channel = new EventQueueChannel<T>(this);
                _queueChannels.Add(key, channel);
            }

            channel.Enqueue(gameEvent);
            _queueOrder.Enqueue(channel);
        }

        /// <inheritdoc />
        public int Flush()
        {
            VerifyThread();
            ThrowIfDisposed();

            if (_flushing)
            {
                // A handler called Flush from inside a flush. Ignoring it keeps ordering intact; the outer loop
                // still owns the drain.
                return 0;
            }

            int budget = _queueOrder.Count;
            if (budget == 0)
            {
                return 0;
            }

            if (budget > _settings.MaxEventsPerFlush)
            {
                _logger.LogWarning(
                    "Event queue holds " + budget + " events but MaxEventsPerFlush is " +
                    _settings.MaxEventsPerFlush + ". The remainder is deferred to the next flush.");
                budget = _settings.MaxEventsPerFlush;
            }

            int dispatched = 0;
            _flushing = true;
            try
            {
                while (budget > 0 && _queueOrder.Count > 0)
                {
                    budget--;
                    IEventQueueChannel channel = _queueOrder.Dequeue();
                    try
                    {
                        if (channel.DispatchNext())
                        {
                            dispatched++;
                        }
                    }
                    catch (GameEventsRecursionException exception)
                    {
                        // A runaway chain must not take the rest of the frame's queue with it.
                        _logger.LogError("Queued event dispatch was stopped by runaway recursion.", exception);
                    }
                }
            }
            finally
            {
                _flushing = false;
            }

            return dispatched;
        }

        /// <inheritdoc />
        public IDisposable Subscribe<T>(Action<T> handler, int priority = 0) where T : struct, IGameEvent
        {
            VerifyThread();
            ThrowIfDisposed();
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            EventPath path = _registry.GetPath(typeof(T));

            Type key = typeof(T);
            TypedChannel<T> channel;
            if (_typedChannels.TryGetValue(key, out ITypedChannel existing))
            {
                channel = (TypedChannel<T>)existing;
            }
            else
            {
                channel = new TypedChannel<T>();
                _typedChannels.Add(key, channel);
            }

            TypedSubscription<T> subscription = new TypedSubscription<T>(this, channel, handler, priority, _nextOrder++);
            channel.Add(subscription);
            LogSubscriptionChange(path, true, false, handler);
            return subscription;
        }

        /// <inheritdoc />
        public IDisposable SubscribeBranch(string branchPath, Action<IGameEvent> handler, int priority = 0)
        {
            VerifyThread();
            ThrowIfDisposed();
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            EventPath branch = _registry.GetBranch(branchPath);
            BranchSubscription subscription = new BranchSubscription(this, branch, handler, priority, _nextOrder++);

            BranchSubscription[] current = _branches;
            int insertAt = current.Length;
            for (int i = 0; i < current.Length; i++)
            {
                BranchSubscription other = current[i];
                bool goesBefore = priority < other.Priority ||
                                  (priority == other.Priority && subscription.Order < other.Order);
                if (goesBefore)
                {
                    insertAt = i;
                    break;
                }
            }

            BranchSubscription[] next = new BranchSubscription[current.Length + 1];
            Array.Copy(current, 0, next, 0, insertAt);
            next[insertAt] = subscription;
            Array.Copy(current, insertAt, next, insertAt + 1, current.Length - insertAt);
            _branches = next;
            _branchMatchCache.Clear();

            LogSubscriptionChange(branch, true, true, handler);
            return subscription;
        }

        /// <inheritdoc />
        public IDisposable AddObserver(IEventObserver observer)
        {
            VerifyThread();
            ThrowIfDisposed();
            if (observer == null)
            {
                throw new ArgumentNullException(nameof(observer));
            }

            ObserverSubscription subscription = new ObserverSubscription(this, observer);
            ObserverSubscription[] current = _observers;
            ObserverSubscription[] next = new ObserverSubscription[current.Length + 1];
            Array.Copy(current, 0, next, 0, current.Length);
            next[current.Length] = subscription;
            _observers = next;
            return subscription;
        }

        /// <inheritdoc />
        public void CopySubscriptions(List<SubscriptionInfo> buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            buffer.Clear();
            foreach (KeyValuePair<Type, ITypedChannel> pair in _typedChannels)
            {
                string path = _registry.GetPath(pair.Key).Full;
                pair.Value.CollectInfo(buffer, path);
            }

            BranchSubscription[] branches = _branches;
            for (int i = 0; i < branches.Length; i++)
            {
                BranchSubscription subscription = branches[i];
                if (subscription.IsActive)
                {
                    buffer.Add(new SubscriptionInfo(
                        subscription.Branch.ToString(),
                        true,
                        subscription.Priority,
                        subscription.DescribeTarget()));
                }
            }
        }

        /// <summary>
        /// Releases every subscription, drops every queued event and makes further calls throw. Bound through
        /// Zenject's <c>DisposableManager</c>, so tearing a context down empties its service.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            foreach (KeyValuePair<Type, ITypedChannel> pair in _typedChannels)
            {
                pair.Value.Clear();
            }

            _typedChannels.Clear();

            BranchSubscription[] branches = _branches;
            _branches = new BranchSubscription[0];
            for (int i = 0; i < branches.Length; i++)
            {
                branches[i].Deactivate();
            }

            ObserverSubscription[] observers = _observers;
            _observers = new ObserverSubscription[0];
            for (int i = 0; i < observers.Length; i++)
            {
                observers[i].Deactivate();
            }

            foreach (KeyValuePair<Type, IEventQueueChannel> pair in _queueChannels)
            {
                pair.Value.Clear();
            }

            _queueChannels.Clear();
            _queueOrder.Clear();
            _branchMatchCache.Clear();
            _branchMatchBuffer.Clear();
            _dispatchChain.Clear();
        }

        internal void PublishFromQueue<T>(T gameEvent) where T : struct, IGameEvent
        {
            EventPath path = _registry.GetPath(typeof(T));
            Dispatch(path, gameEvent, true);
        }

        internal void RemoveBranch(BranchSubscription subscription)
        {
            BranchSubscription[] current = _branches;
            int index = -1;
            for (int i = 0; i < current.Length; i++)
            {
                if (ReferenceEquals(current[i], subscription))
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                return;
            }

            if (current.Length == 1)
            {
                _branches = new BranchSubscription[0];
            }
            else
            {
                BranchSubscription[] next = new BranchSubscription[current.Length - 1];
                Array.Copy(current, 0, next, 0, index);
                Array.Copy(current, index + 1, next, index, current.Length - index - 1);
                _branches = next;
            }

            _branchMatchCache.Clear();
        }

        internal void RemoveObserver(ObserverSubscription subscription)
        {
            ObserverSubscription[] current = _observers;
            int index = -1;
            for (int i = 0; i < current.Length; i++)
            {
                if (ReferenceEquals(current[i], subscription))
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                return;
            }

            if (current.Length == 1)
            {
                _observers = new ObserverSubscription[0];
                return;
            }

            ObserverSubscription[] next = new ObserverSubscription[current.Length - 1];
            Array.Copy(current, 0, next, 0, index);
            Array.Copy(current, index + 1, next, index, current.Length - index - 1);
            _observers = next;
        }

        internal string DescribeHandler(Delegate handler)
        {
            if (handler == null)
            {
                return "(released)";
            }

            Type declaring = handler.Method.DeclaringType;

            // A lambda lives on a compiler generated closure type nested in the real owner; walk out to it so
            // the debugger window shows "AnalyticsService.<lambda>" instead of "<>c__DisplayClass4_0".
            while (declaring != null && declaring.Name.Length > 0 && declaring.Name[0] == '<' && declaring.DeclaringType != null)
            {
                declaring = declaring.DeclaringType;
            }

            string owner;
            if (declaring != null)
            {
                owner = declaring.Name;
            }
            else if (handler.Target != null)
            {
                owner = handler.Target.GetType().Name;
            }
            else
            {
                owner = "(unknown)";
            }

            return owner + "." + handler.Method.Name;
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        internal void NotifyTypedUnsubscribed(Type eventType, int priority, Delegate handler)
        {
            if (!_settings.LogEventFlow)
            {
                return;
            }

            LogSubscriptionChange(_registry.GetPath(eventType), false, false, handler);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        internal void NotifyBranchUnsubscribed(EventPath branch, int priority, Delegate handler)
        {
            if (!_settings.LogEventFlow)
            {
                return;
            }

            LogSubscriptionChange(branch, false, true, handler);
        }

        private void Dispatch<T>(in EventPath path, T gameEvent, bool fromQueue) where T : struct, IGameEvent
        {
            if (_dispatchDepth >= _settings.MaxDispatchDepth)
            {
                throw new GameEventsRecursionException(BuildRecursionMessage(path));
            }

            _dispatchChain.Add(path.Full);
            _dispatchDepth++;
            try
            {
                DispatchCore(path, gameEvent, fromQueue);
            }
            finally
            {
                _dispatchDepth--;
                _dispatchChain.RemoveAt(_dispatchChain.Count - 1);
            }
        }

        private void DispatchCore<T>(in EventPath path, T gameEvent, bool fromQueue) where T : struct, IGameEvent
        {
            TypedSubscription<T>[] typed = null;
            if (_typedChannels.TryGetValue(typeof(T), out ITypedChannel channel))
            {
                typed = ((TypedChannel<T>)channel).Entries;
            }

            BranchSubscription[] branches = null;
            if (_branches.Length > 0)
            {
                branches = ResolveBranchMatches(typeof(T), path);
            }

            // The single box, created only once something actually needs the event as IGameEvent.
            IGameEvent boxed = null;

            ObserverSubscription[] observers = _observers;
            if (observers.Length > 0)
            {
                boxed = gameEvent;
                for (int i = 0; i < observers.Length; i++)
                {
                    ObserverSubscription observer = observers[i];
                    if (!observer.IsActive)
                    {
                        continue;
                    }

                    try
                    {
                        observer.Observer.OnPublished(path, boxed);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(
                            "Event observer " + observer.Observer.GetType().Name + " threw while observing '" +
                            path.Full + "'.", exception);
                    }
                }
            }

            int typedCount = typed == null ? 0 : typed.Length;
            int branchCount = branches == null ? 0 : branches.Length;
            int typedIndex = 0;
            int branchIndex = 0;

            // Typed and branch handlers form one ordering by priority. Walking both sorted arrays with two
            // indices keeps that ordering without merging them into a temporary list.
            while (typedIndex < typedCount || branchIndex < branchCount)
            {
                bool takeTyped;
                if (typedIndex >= typedCount)
                {
                    takeTyped = false;
                }
                else if (branchIndex >= branchCount)
                {
                    takeTyped = true;
                }
                else
                {
                    TypedSubscription<T> candidate = typed[typedIndex];
                    BranchSubscription other = branches[branchIndex];
                    takeTyped = candidate.Priority != other.Priority
                        ? candidate.Priority < other.Priority
                        : candidate.Order <= other.Order;
                }

                if (takeTyped)
                {
                    TypedSubscription<T> subscription = typed[typedIndex++];
                    if (!subscription.IsActive)
                    {
                        continue;
                    }

                    Action<T> handler = subscription.Handler;
                    try
                    {
                        handler(gameEvent);
                    }
                    catch (GameEventsRecursionException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        ReportHandlerException(path, handler, exception);
                    }
                }
                else
                {
                    BranchSubscription subscription = branches[branchIndex++];
                    if (!subscription.IsActive)
                    {
                        continue;
                    }

                    if (boxed == null)
                    {
                        boxed = gameEvent;
                    }

                    Action<IGameEvent> handler = subscription.Handler;
                    try
                    {
                        handler(boxed);
                    }
                    catch (GameEventsRecursionException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        ReportHandlerException(path, handler, exception);
                    }
                }
            }

            LogDispatch(path, typeof(T), typedCount + branchCount, fromQueue);
        }

        private BranchSubscription[] ResolveBranchMatches(Type eventType, in EventPath path)
        {
            if (_branchMatchCache.TryGetValue(eventType, out BranchSubscription[] cached))
            {
                return cached;
            }

            _branchMatchBuffer.Clear();
            BranchSubscription[] branches = _branches;
            for (int i = 0; i < branches.Length; i++)
            {
                BranchSubscription subscription = branches[i];
                if (path.IsUnder(subscription.Branch))
                {
                    _branchMatchBuffer.Add(subscription);
                }
            }

            BranchSubscription[] matches = _branchMatchBuffer.Count == 0
                ? new BranchSubscription[0]
                : _branchMatchBuffer.ToArray();
            _branchMatchBuffer.Clear();
            _branchMatchCache.Add(eventType, matches);
            return matches;
        }

        private void ReportHandlerException(in EventPath path, Delegate handler, Exception exception)
        {
            _logger.LogError(
                "Event handler " + DescribeHandler(handler) + " threw while handling '" + path.Full +
                "'. The remaining handlers of this event still ran.", exception);
        }

        private string BuildRecursionMessage(in EventPath path)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("Event dispatch nested deeper than MaxDispatchDepth (")
                .Append(_settings.MaxDispatchDepth)
                .Append(") while publishing '")
                .Append(path.Full)
                .Append("'. Chain: ");

            for (int i = 0; i < _dispatchChain.Count; i++)
            {
                builder.Append(_dispatchChain[i]).Append(" -> ");
            }

            builder.Append(path.Full)
                .Append(". A handler is publishing an event that leads back to itself. Break the cycle, or raise ")
                .Append("MaxDispatchDepth in the installer if the nesting is genuinely that deep.");

            return builder.ToString();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(GlobalGameEvents),
                    "This event service was disposed together with its container. Resolve the service from the context " +
                    "you are actually running in instead of holding on to one from a torn down context.");
            }
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private void VerifyThread()
        {
            if (!_settings.VerifyMainThread)
            {
                return;
            }

            int current = Thread.CurrentThread.ManagedThreadId;
            if (current == _ownerThreadId)
            {
                return;
            }

            throw new GameEventsThreadException(
                "The event service was touched from thread " + current + " but it belongs to thread " +
                _ownerThreadId + ". The service is a main thread component: marshal the call back yourself, for " +
                "example with UniTask.SwitchToMainThread, before publishing or subscribing.");
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private void LogDispatch(in EventPath path, Type eventType, int handlerCount, bool fromQueue)
        {
            if (!_settings.LogEventFlow)
            {
                return;
            }

            _logger.LogEvent(path, eventType, handlerCount, fromQueue);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private void LogSubscriptionChange(in EventPath path, bool subscribed, bool isBranch, Delegate handler)
        {
            if (!_settings.LogEventFlow)
            {
                return;
            }

            _logger.LogSubscription(path, subscribed, isBranch, DescribeHandler(handler));
        }
    }
}
