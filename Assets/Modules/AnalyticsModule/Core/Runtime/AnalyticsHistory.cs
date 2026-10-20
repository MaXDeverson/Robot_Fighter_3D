using System;

namespace Game.Analytics
{
    /// <summary>
    /// Ring buffer behind <see cref="IAnalyticsHistory"/>, owned by the service. Records are shared with the consent
    /// queue, so a queued event shown here moves on to sent or dropped in place.
    /// </summary>
    internal sealed class AnalyticsHistory : IAnalyticsHistory
    {
        private readonly AnalyticsRecord[] _records;
        private int _count;
        private int _next;

        internal AnalyticsHistory(int capacity)
        {
            _records = new AnalyticsRecord[capacity < 1 ? 1 : capacity];
        }

        public int Capacity
        {
            get { return _records.Length; }
        }

        public int Count
        {
            get { return _count; }
        }

        public AnalyticsRecord this[int index]
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

        public void Clear()
        {
            Array.Clear(_records, 0, _records.Length);
            _count = 0;
            _next = 0;
        }

        internal void Add(AnalyticsRecord record)
        {
            _records[_next] = record;
            _next = (_next + 1) % _records.Length;
            if (_count < _records.Length)
            {
                _count++;
            }
        }
    }
}
