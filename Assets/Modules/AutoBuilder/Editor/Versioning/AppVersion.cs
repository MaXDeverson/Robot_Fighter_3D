using System;
using System.Globalization;

namespace Game.AutoBuilder
{
    internal enum VersionPart
    {
        Patch,
        Minor,
        Major,
    }

    /// <summary>
    /// The game version players see (<c>PlayerSettings.bundleVersion</c>), read as major.minor.patch.
    /// </summary>
    /// <remarks>
    /// "1" and "1.2" are accepted and read as 1.0.0 and 1.2.0, so a bump always writes all three parts back.
    /// </remarks>
    internal readonly struct AppVersion : IEquatable<AppVersion>
    {
        public readonly int Major;
        public readonly int Minor;
        public readonly int Patch;

        public AppVersion(int major, int minor, int patch)
        {
            if (major < 0 || minor < 0 || patch < 0)
                throw new ArgumentOutOfRangeException(nameof(major), "Version parts cannot be negative.");

            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public static bool TryParse(string text, out AppVersion version)
        {
            version = default;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            string[] parts = text.Trim().Split('.');
            if (parts.Length > 3)
                return false;

            var numbers = new int[3];
            for (int i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out numbers[i]))
                    return false;
            }

            version = new AppVersion(numbers[0], numbers[1], numbers[2]);
            return true;
        }

        public AppVersion Bump(VersionPart part)
        {
            switch (part)
            {
                case VersionPart.Major: return new AppVersion(Major + 1, 0, 0);
                case VersionPart.Minor: return new AppVersion(Major, Minor + 1, 0);
                case VersionPart.Patch: return new AppVersion(Major, Minor, Patch + 1);
                default: throw new ArgumentOutOfRangeException(nameof(part), part, null);
            }
        }

        public override string ToString() =>
            string.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}", Major, Minor, Patch);

        public bool Equals(AppVersion other) => Major == other.Major && Minor == other.Minor && Patch == other.Patch;

        public override bool Equals(object obj) => obj is AppVersion other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch);
    }
}
