using System;
using System.Collections.Generic;

namespace Game.Events
{
    /// <summary>
    /// Default <see cref="IEventPathRegistry"/>. Every cache it owns is an instance field, so two services in two
    /// containers never share a lookup table and nothing leaks across a domain reload.
    /// </summary>
    public sealed class EventPathRegistry : IEventPathRegistry
    {
        private readonly Dictionary<Type, EventPath> _pathByType = new Dictionary<Type, EventPath>();
        private readonly Dictionary<string, Type> _typeByPath = new Dictionary<string, Type>(StringComparer.Ordinal);
        private readonly Dictionary<string, EventPath> _branchByText = new Dictionary<string, EventPath>(StringComparer.Ordinal);
        private readonly EventPath _rootBranch = new EventPath(null);

        /// <inheritdoc />
        public IReadOnlyDictionary<Type, EventPath> KnownPaths
        {
            get { return _pathByType; }
        }

        /// <inheritdoc />
        public EventPath GetPath(Type eventType)
        {
            if (eventType == null)
            {
                throw new ArgumentNullException(nameof(eventType));
            }

            if (_pathByType.TryGetValue(eventType, out EventPath cached))
            {
                return cached;
            }

            EventPath resolved = Resolve(eventType);
            _pathByType.Add(eventType, resolved);
            _typeByPath[resolved.Full] = eventType;
            return resolved;
        }

        /// <inheritdoc />
        public EventPath GetBranch(string branchPath)
        {
            if (string.IsNullOrEmpty(branchPath) || branchPath == EventPath.Wildcard)
            {
                return _rootBranch;
            }

            if (_branchByText.TryGetValue(branchPath, out EventPath cached))
            {
                return cached;
            }

            EventPath parsed = new EventPath(branchPath);
            _branchByText.Add(branchPath, parsed);
            return parsed;
        }

        /// <inheritdoc />
        public bool TryValidate(IEnumerable<Type> eventTypes, List<string> problems)
        {
            if (problems == null)
            {
                throw new ArgumentNullException(nameof(problems));
            }

            problems.Clear();
            if (eventTypes == null)
            {
                return true;
            }

            foreach (Type eventType in eventTypes)
            {
                try
                {
                    GetPath(eventType);
                }
                catch (GameEventsException exception)
                {
                    problems.Add(exception.Message);
                }
            }

            return problems.Count == 0;
        }

        private EventPath Resolve(Type eventType)
        {
            if (!typeof(IGameEvent).IsAssignableFrom(eventType))
            {
                throw new EventPathFormatException(
                    "Type '" + eventType.FullName + "' does not implement " + nameof(IGameEvent) +
                    " and cannot be used as an event.");
            }

            object[] attributes = eventType.GetCustomAttributes(typeof(EventPathAttribute), false);
            if (attributes.Length == 0)
            {
                throw new EventPathFormatException(
                    "Event type '" + eventType.FullName + "' has no [EventPath] attribute. Add one, for example " +
                    "[EventPath(\"Level.Entered\")], so the event can be reached by branch subscriptions.");
            }

            EventPathAttribute attribute = (EventPathAttribute)attributes[0];
            EventPath path;
            try
            {
                path = new EventPath(attribute.Path);
            }
            catch (EventPathFormatException exception)
            {
                throw new EventPathFormatException(
                    "Event type '" + eventType.FullName + "' declares an invalid path. " + exception.Message,
                    exception);
            }

            if (path.IsRoot)
            {
                throw new EventPathFormatException(
                    "Event type '" + eventType.FullName + "' declares an empty path. The root is reserved for " +
                    "branch subscriptions that want every event; an event type needs a real path.");
            }

            if (_typeByPath.TryGetValue(path.Full, out Type owner) && owner != eventType)
            {
                throw new EventPathConflictException(
                    "Event path '" + path.Full + "' is declared by both '" + owner.FullName + "' and '" +
                    eventType.FullName + "'. Paths must be unique so branch subscribers can tell events apart.");
            }

            return path;
        }
    }
}
