using System;
using System.Collections.Generic;

namespace Game.Events
{
    /// <summary>
    /// Non generic handle on one type's deferred queue.
    /// </summary>
    internal interface IEventQueueChannel
    {
        Type EventType { get; }

        int Count { get; }

        /// <summary>Publishes the oldest queued value, if there is one.</summary>
        /// <returns>True when a value was dispatched.</returns>
        bool DispatchNext();

        void Clear();
    }

    /// <summary>
    /// A first in, first out queue of one event type.
    /// </summary>
    /// <remarks>
    /// Keeping a <see cref="Queue{T}"/> per type is what lets <see cref="IEventQueue.Enqueue{T}"/> stay free of
    /// boxing: the value is copied into a strongly typed slot instead of into an object. The service keeps a
    /// separate queue of channel references to preserve the order events were enqueued in across types.
    /// </remarks>
    internal sealed class EventQueueChannel<T> : IEventQueueChannel where T : struct, IGameEvent
    {
        private readonly GlobalGameEvents _events;
        private readonly Queue<T> _values = new Queue<T>();

        internal EventQueueChannel(GlobalGameEvents events)
        {
            _events = events;
        }

        public Type EventType
        {
            get { return typeof(T); }
        }

        public int Count
        {
            get { return _values.Count; }
        }

        internal void Enqueue(T value)
        {
            _values.Enqueue(value);
        }

        public bool DispatchNext()
        {
            if (_values.Count == 0)
            {
                return false;
            }

            T value = _values.Dequeue();
            _events.PublishFromQueue(value);
            return true;
        }

        public void Clear()
        {
            _values.Clear();
        }
    }
}
