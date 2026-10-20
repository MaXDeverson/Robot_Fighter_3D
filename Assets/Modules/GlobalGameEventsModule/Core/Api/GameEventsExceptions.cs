using System;

namespace Game.Events
{
    /// <summary>Base type for every error the event service raises.</summary>
    public class GameEventsException : Exception
    {
        /// <summary>Creates the exception.</summary>
        public GameEventsException(string message) : base(message)
        {
        }

        /// <summary>Creates the exception with an inner cause.</summary>
        public GameEventsException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>Raised when an event path is malformed or missing.</summary>
    public sealed class EventPathFormatException : GameEventsException
    {
        /// <summary>Creates the exception.</summary>
        public EventPathFormatException(string message) : base(message)
        {
        }

        /// <summary>Creates the exception with an inner cause.</summary>
        public EventPathFormatException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>Raised when two event types claim the same path.</summary>
    public sealed class EventPathConflictException : GameEventsException
    {
        /// <summary>Creates the exception.</summary>
        public EventPathConflictException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Raised when publishing nests deeper than <see cref="GameEventsSettings.MaxDispatchDepth"/>, which almost
    /// always means two handlers are publishing each other's events in a loop.
    /// </summary>
    /// <remarks>
    /// Unlike an ordinary handler failure this exception is never swallowed: the service rethrows it through every
    /// nested dispatch so it surfaces at the call site that started the chain, with the whole path chain in
    /// <see cref="Exception.Message"/>.
    /// </remarks>
    public sealed class GameEventsRecursionException : GameEventsException
    {
        /// <summary>Creates the exception.</summary>
        public GameEventsRecursionException(string message) : base(message)
        {
        }
    }

    /// <summary>Raised when the service is touched from a thread other than the one that created it.</summary>
    public sealed class GameEventsThreadException : GameEventsException
    {
        /// <summary>Creates the exception.</summary>
        public GameEventsThreadException(string message) : base(message)
        {
        }
    }
}
