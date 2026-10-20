using System;
using System.Collections.Generic;

namespace Game.Events
{
    /// <summary>
    /// Resolves an event type to its <see cref="EventPath"/> and caches the answer.
    /// </summary>
    /// <remarks>
    /// The cache lives in the registry instance, not in a shared table, so two containers each get their own and
    /// nothing survives a domain reload. Reflection runs once per event type, never on the publish path.
    /// </remarks>
    public interface IEventPathRegistry
    {
        /// <summary>Every path resolved so far, keyed by event type.</summary>
        IReadOnlyDictionary<Type, EventPath> KnownPaths { get; }

        /// <summary>
        /// Returns the path of an event type, reading its <see cref="EventPathAttribute"/> on first use.
        /// </summary>
        /// <param name="eventType">A struct implementing <see cref="IGameEvent"/>.</param>
        /// <exception cref="EventPathFormatException">The attribute is missing or the path is malformed.</exception>
        /// <exception cref="EventPathConflictException">Another type already claimed this path.</exception>
        EventPath GetPath(Type eventType);

        /// <summary>
        /// Parses and caches a branch filter.
        /// </summary>
        /// <param name="branchPath"><c>null</c>, <c>""</c> or <c>"*"</c> for the root; otherwise a dot path.</param>
        EventPath GetBranch(string branchPath);

        /// <summary>
        /// Resolves a whole set of event types up front so malformed and duplicate paths surface as one report
        /// instead of one exception at a time. Used by the editor validator.
        /// </summary>
        /// <param name="eventTypes">Types to resolve.</param>
        /// <param name="problems">Receives one message per problem. Cleared first.</param>
        /// <returns>True when every type resolved cleanly.</returns>
        bool TryValidate(IEnumerable<Type> eventTypes, List<string> problems);
    }
}
