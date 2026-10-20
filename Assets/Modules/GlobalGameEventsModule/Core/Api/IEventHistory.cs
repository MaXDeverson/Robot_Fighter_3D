namespace Game.Events
{
    /// <summary>One line of the recorded event stream.</summary>
    public readonly struct EventRecord
    {
        /// <summary>Timestamp from the <see cref="IEventClock"/>, in seconds.</summary>
        public readonly double Time;

        /// <summary>Path of the event, for example <c>Level.Entered</c>.</summary>
        public readonly string Path;

        /// <summary>Short name of the event type.</summary>
        public readonly string TypeName;

        /// <summary>Public fields of the event rendered as <c>Name=Value</c> pairs.</summary>
        public readonly string Payload;

        /// <summary>Creates a record.</summary>
        public EventRecord(double time, string path, string typeName, string payload)
        {
            Time = time;
            Path = path;
            TypeName = typeName;
            Payload = payload;
        }
    }

    /// <summary>
    /// A ring buffer of the last events, kept inside a container managed service so the editor window can read
    /// it without any global access to the service.
    /// </summary>
    public interface IEventHistory
    {
        /// <summary>How many records the buffer holds before it starts overwriting.</summary>
        int Capacity { get; }

        /// <summary>How many records are currently stored.</summary>
        int Count { get; }

        /// <summary>Records in chronological order, index zero being the oldest still kept.</summary>
        /// <param name="index">Zero based index below <see cref="Count"/>.</param>
        EventRecord this[int index] { get; }

        /// <summary>Drops every record.</summary>
        void Clear();
    }
}
