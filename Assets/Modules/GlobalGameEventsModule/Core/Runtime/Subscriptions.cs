using System;
using System.Collections.Generic;

namespace Game.Events
{
    /// <summary>
    /// Non generic handle on a typed channel, so the service can keep every channel in one dictionary.
    /// </summary>
    internal interface ITypedChannel
    {
        Type EventType { get; }

        int ActiveCount { get; }

        void CollectInfo(List<SubscriptionInfo> buffer, string path);

        void Clear();
    }

    /// <summary>
    /// Holds the handlers of one concrete event type, sorted by priority then subscription order.
    /// </summary>
    /// <remarks>
    /// The entry array is swapped on every structural change instead of being mutated, so a dispatch can walk
    /// the array it captured on entry while handlers subscribe and unsubscribe underneath it. Subscribing costs
    /// one array copy; publishing costs nothing.
    /// </remarks>
    internal sealed class TypedChannel<T> : ITypedChannel where T : struct, IGameEvent
    {
        private TypedSubscription<T>[] _entries = new TypedSubscription<T>[0];

        public Type EventType
        {
            get { return typeof(T); }
        }

        public int ActiveCount
        {
            get
            {
                TypedSubscription<T>[] current = _entries;
                int count = 0;
                for (int i = 0; i < current.Length; i++)
                {
                    if (current[i].IsActive)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>The current snapshot. Never mutated in place; read it once and walk it.</summary>
        internal TypedSubscription<T>[] Entries
        {
            get { return _entries; }
        }

        internal void Add(TypedSubscription<T> subscription)
        {
            TypedSubscription<T>[] current = _entries;
            int insertAt = current.Length;
            for (int i = 0; i < current.Length; i++)
            {
                TypedSubscription<T> existing = current[i];
                bool goesBefore = subscription.Priority < existing.Priority ||
                                  (subscription.Priority == existing.Priority && subscription.Order < existing.Order);
                if (goesBefore)
                {
                    insertAt = i;
                    break;
                }
            }

            TypedSubscription<T>[] next = new TypedSubscription<T>[current.Length + 1];
            Array.Copy(current, 0, next, 0, insertAt);
            next[insertAt] = subscription;
            Array.Copy(current, insertAt, next, insertAt + 1, current.Length - insertAt);
            _entries = next;
        }

        internal void Remove(TypedSubscription<T> subscription)
        {
            TypedSubscription<T>[] current = _entries;
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
                _entries = new TypedSubscription<T>[0];
                return;
            }

            TypedSubscription<T>[] next = new TypedSubscription<T>[current.Length - 1];
            Array.Copy(current, 0, next, 0, index);
            Array.Copy(current, index + 1, next, index, current.Length - index - 1);
            _entries = next;
        }

        public void CollectInfo(List<SubscriptionInfo> buffer, string path)
        {
            TypedSubscription<T>[] current = _entries;
            for (int i = 0; i < current.Length; i++)
            {
                TypedSubscription<T> subscription = current[i];
                if (subscription.IsActive)
                {
                    buffer.Add(new SubscriptionInfo(path, false, subscription.Priority, subscription.DescribeTarget()));
                }
            }
        }

        public void Clear()
        {
            TypedSubscription<T>[] current = _entries;
            _entries = new TypedSubscription<T>[0];
            for (int i = 0; i < current.Length; i++)
            {
                current[i].Deactivate();
            }
        }
    }

    /// <summary>One handler registered for one concrete event type. Disposing it unsubscribes.</summary>
    internal sealed class TypedSubscription<T> : IDisposable where T : struct, IGameEvent
    {
        private readonly GlobalGameEvents _events;
        private readonly TypedChannel<T> _channel;

        internal TypedSubscription(GlobalGameEvents events, TypedChannel<T> channel, Action<T> handler, int priority, int order)
        {
            _events = events;
            _channel = channel;
            Handler = handler;
            Priority = priority;
            Order = order;
            IsActive = true;
        }

        internal Action<T> Handler { get; private set; }

        internal int Priority { get; }

        internal int Order { get; }

        /// <summary>
        /// Checked immediately before the handler is invoked, which is what makes unsubscribing from inside a
        /// handler safe: the dispatch snapshot still holds this entry, but it is skipped.
        /// </summary>
        internal bool IsActive { get; private set; }

        public void Dispose()
        {
            if (!IsActive)
            {
                return;
            }

            IsActive = false;
            _channel.Remove(this);
            _events.NotifyTypedUnsubscribed(typeof(T), Priority, Handler);
            Handler = null;
        }

        internal void Deactivate()
        {
            IsActive = false;
            Handler = null;
        }

        internal string DescribeTarget()
        {
            return _events.DescribeHandler(Handler);
        }
    }

    /// <summary>One handler registered for a whole branch of the path tree. Disposing it unsubscribes.</summary>
    internal sealed class BranchSubscription : IDisposable
    {
        private readonly GlobalGameEvents _events;

        internal BranchSubscription(GlobalGameEvents events, EventPath branch, Action<IGameEvent> handler, int priority, int order)
        {
            _events = events;
            Branch = branch;
            Handler = handler;
            Priority = priority;
            Order = order;
            IsActive = true;
        }

        internal EventPath Branch { get; }

        internal Action<IGameEvent> Handler { get; private set; }

        internal int Priority { get; }

        internal int Order { get; }

        internal bool IsActive { get; private set; }

        public void Dispose()
        {
            if (!IsActive)
            {
                return;
            }

            IsActive = false;
            _events.RemoveBranch(this);
            _events.NotifyBranchUnsubscribed(Branch, Priority, Handler);
            Handler = null;
        }

        internal void Deactivate()
        {
            IsActive = false;
            Handler = null;
        }

        internal string DescribeTarget()
        {
            return _events.DescribeHandler(Handler);
        }
    }

    /// <summary>A registered observer. Disposing it detaches the observer.</summary>
    internal sealed class ObserverSubscription : IDisposable
    {
        private readonly GlobalGameEvents _events;

        internal ObserverSubscription(GlobalGameEvents events, IEventObserver observer)
        {
            _events = events;
            Observer = observer;
            IsActive = true;
        }

        internal IEventObserver Observer { get; }

        internal bool IsActive { get; private set; }

        public void Dispose()
        {
            if (!IsActive)
            {
                return;
            }

            IsActive = false;
            _events.RemoveObserver(this);
        }

        internal void Deactivate()
        {
            IsActive = false;
        }
    }
}
