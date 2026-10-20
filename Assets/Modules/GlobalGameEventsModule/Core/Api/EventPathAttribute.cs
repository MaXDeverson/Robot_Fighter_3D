using System;

namespace Game.Events
{
    /// <summary>
    /// Declares the hierarchical path of an event type, for example <c>"Level.Entered"</c>.
    /// </summary>
    /// <remarks>
    /// The path is read once per type by <see cref="IEventPathRegistry"/> and cached inside that registry
    /// instance, so reflection never runs on the publish path. Paths must be unique across the game: two event
    /// types declaring the same path raise an <see cref="EventPathConflictException"/>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class EventPathAttribute : Attribute
    {
        /// <summary>
        /// Creates the attribute.
        /// </summary>
        /// <param name="path">
        /// Dot separated path such as <c>"Level.Entered"</c>. Each segment must start with a letter or an
        /// underscore and may contain letters, digits and underscores.
        /// </param>
        public EventPathAttribute(string path)
        {
            Path = path;
        }

        /// <summary>The raw path exactly as it was written in the attribute.</summary>
        public string Path { get; }
    }
}
