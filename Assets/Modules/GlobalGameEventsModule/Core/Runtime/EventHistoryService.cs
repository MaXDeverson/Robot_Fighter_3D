using System;

namespace Game.Events
{
    /// <summary>
    /// Records the last <see cref="GameEventsSettings.HistoryCapacity"/> events in a ring buffer owned by this
    /// instance, and exposes them to the editor window.
    /// </summary>
    /// <remarks>
    /// It attaches to the service as an <see cref="IEventObserver"/>, so it sees every event exactly once, including
    /// events delivered by a queue flush, and it never appears in the subscription list the debugger shows.
    /// Registering it does mean each published event gets boxed once, which is why the installer only binds it
    /// when <see cref="GameEventsSettings.RecordHistory"/> is on.
    /// </remarks>
    public sealed class EventHistoryService : IEventHistory, IEventObserver
    {
        private readonly IEventClock _clock;
        private readonly EventPayloadFormatter _formatter = new EventPayloadFormatter();
        private readonly EventRecord[] _records;

        private int _count;
        private int _next;

        /// <summary>
        /// Creates the service.
        /// </summary>
        /// <param name="settings">Supplies <see cref="GameEventsSettings.HistoryCapacity"/>.</param>
        /// <param name="clock">Timestamp source. Pass <c>null</c> to use a private stopwatch.</param>
        public EventHistoryService(GameEventsSettings settings, IEventClock clock)
        {
            GameEventsSettings effective = settings ?? new GameEventsSettings();
            _records = new EventRecord[effective.HistoryCapacity];
            _clock = clock ?? new SystemEventClock();
        }

        /// <inheritdoc />
        public int Capacity
        {
            get { return _records.Length; }
        }

        /// <inheritdoc />
        public int Count
        {
            get { return _count; }
        }

        /// <inheritdoc />
        public EventRecord this[int index]
        {
            get
            {
                if (index < 0 || index >= _count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                int oldest = _count == _records.Length ? _next : 0;
                return _records[(oldest + index) % _records.Length];
            }
        }

        /// <inheritdoc />
        public void OnPublished(in EventPath path, IGameEvent gameEvent)
        {
            _records[_next] = new EventRecord(
                _clock.Now,
                path.Full,
                gameEvent == null ? "(null)" : gameEvent.GetType().Name,
                _formatter.Describe(gameEvent));

            _next = (_next + 1) % _records.Length;
            if (_count < _records.Length)
            {
                _count++;
            }
        }

        /// <inheritdoc />
        public void Clear()
        {
            Array.Clear(_records, 0, _records.Length);
            _count = 0;
            _next = 0;
        }
    }
}
