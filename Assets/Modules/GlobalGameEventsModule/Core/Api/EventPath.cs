using System;

namespace Game.Events
{
    /// <summary>
    /// A parsed, segment-wise hierarchical event path such as <c>Level.Entered</c>.
    /// </summary>
    /// <remarks>
    /// Branch matching compares whole segments, never string prefixes, so the branch <c>"Level"</c> matches
    /// <c>Level.Entered</c> and <c>Level.Entered.Boss</c> but never <c>LevelUp.Something</c>.
    /// The <c>default</c> value is the root path, which every event is under.
    /// </remarks>
    public readonly struct EventPath : IEquatable<EventPath>
    {
        /// <summary>Character that separates path segments.</summary>
        public const char Separator = '.';

        /// <summary>Branch token meaning "every event".</summary>
        public const string Wildcard = "*";

        private readonly string _full;
        private readonly string[] _segments;

        /// <summary>
        /// Parses and validates a path.
        /// </summary>
        /// <param name="path">
        /// A dot separated path. <c>null</c>, the empty string and <see cref="Wildcard"/> all produce the root
        /// path. A trailing <c>".*"</c> is accepted and stripped, so <c>"Level.*"</c> and <c>"Level"</c> are the
        /// same branch.
        /// </param>
        /// <exception cref="EventPathFormatException">The path contains an empty or illegal segment.</exception>
        public EventPath(string path)
        {
            if (string.IsNullOrEmpty(path) || path == Wildcard)
            {
                _full = string.Empty;
                _segments = null;
                return;
            }

            string normalized = path;
            if (normalized.Length >= 2 &&
                normalized[normalized.Length - 1] == '*' &&
                normalized[normalized.Length - 2] == Separator)
            {
                normalized = normalized.Substring(0, normalized.Length - 2);
            }

            if (normalized.Length == 0)
            {
                _full = string.Empty;
                _segments = null;
                return;
            }

            string[] segments = normalized.Split(Separator);
            for (int i = 0; i < segments.Length; i++)
            {
                string segment = segments[i];
                if (segment.Length == 0)
                {
                    throw new EventPathFormatException(
                        "Event path '" + path + "' has an empty segment at index " + i +
                        ". Segments must be separated by a single '.' and none of them may be empty.");
                }

                char first = segment[0];
                bool firstIsLegal = (first >= 'A' && first <= 'Z') || (first >= 'a' && first <= 'z') || first == '_';
                if (!firstIsLegal)
                {
                    throw new EventPathFormatException(
                        "Event path '" + path + "' has segment '" + segment +
                        "' starting with '" + first + "'. A segment must start with a letter or an underscore.");
                }

                for (int c = 1; c < segment.Length; c++)
                {
                    char current = segment[c];
                    bool isLegal = (current >= 'A' && current <= 'Z') ||
                                   (current >= 'a' && current <= 'z') ||
                                   (current >= '0' && current <= '9') ||
                                   current == '_';
                    if (!isLegal)
                    {
                        throw new EventPathFormatException(
                            "Event path '" + path + "' has segment '" + segment + "' containing '" + current +
                            "'. Segments may only contain letters, digits and underscores.");
                    }
                }
            }

            _full = normalized;
            _segments = segments;
        }

        /// <summary>The normalized path, or the empty string for the root.</summary>
        public string Full
        {
            get { return _full ?? string.Empty; }
        }

        /// <summary>Number of segments. Zero for the root path.</summary>
        public int SegmentCount
        {
            get { return _segments == null ? 0 : _segments.Length; }
        }

        /// <summary>True when this path is the root, which every event is under.</summary>
        public bool IsRoot
        {
            get { return _segments == null || _segments.Length == 0; }
        }

        /// <summary>Returns a single segment.</summary>
        /// <param name="index">Zero based segment index, left to right.</param>
        public string GetSegment(int index)
        {
            if (_segments == null || index < 0 || index >= _segments.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _segments[index];
        }

        /// <summary>
        /// Tests whether this path sits inside <paramref name="branch"/>, comparing whole segments.
        /// </summary>
        /// <param name="branch">The branch to test against. The root branch matches everything.</param>
        /// <returns>True when every segment of <paramref name="branch"/> is a prefix of this path.</returns>
        public bool IsUnder(in EventPath branch)
        {
            int required = branch.SegmentCount;
            if (required == 0)
            {
                return true;
            }

            if (_segments == null || required > _segments.Length)
            {
                return false;
            }

            for (int i = 0; i < required; i++)
            {
                if (!string.Equals(_segments[i], branch._segments[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc />
        public bool Equals(EventPath other)
        {
            return string.Equals(Full, other.Full, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is EventPath other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Full.GetHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return IsRoot ? Wildcard : Full;
        }
    }
}
