using System;
using System.Globalization;

namespace Game.Analytics
{
    /// <summary>The kind of value an <see cref="AnalyticsValue"/> holds.</summary>
    public enum AnalyticsValueType
    {
        /// <summary>Text.</summary>
        String = 0,

        /// <summary>A whole number.</summary>
        Integer = 1,

        /// <summary>A fractional number.</summary>
        Float = 2,

        /// <summary>A flag.</summary>
        Boolean = 3,
    }

    /// <summary>
    /// One parameter value, with its type kept.
    /// </summary>
    /// <remarks>
    /// Every backend accepts some mix of text, whole numbers, fractions and flags, but not the same mix. Keeping the
    /// type lets each sink convert on its own terms — one without booleans sends 0 or 1, one with them sends the
    /// flag — instead of every tracker guessing. Values convert implicitly from the usual primitives, so a tracker
    /// writes <c>.With("stars", 3)</c> and never names this type.
    /// </remarks>
    public readonly struct AnalyticsValue : IEquatable<AnalyticsValue>
    {
        private readonly string _text;
        private readonly long _integer;
        private readonly double _float;

        /// <summary>Creates a text value. <c>null</c> becomes the empty string.</summary>
        public AnalyticsValue(string value)
        {
            Type = AnalyticsValueType.String;
            _text = value ?? string.Empty;
            _integer = 0;
            _float = 0d;
        }

        /// <summary>Creates a whole number value.</summary>
        public AnalyticsValue(long value)
        {
            Type = AnalyticsValueType.Integer;
            _text = null;
            _integer = value;
            _float = 0d;
        }

        /// <summary>Creates a fractional value.</summary>
        public AnalyticsValue(double value)
        {
            Type = AnalyticsValueType.Float;
            _text = null;
            _integer = 0;
            _float = value;
        }

        /// <summary>Creates a flag.</summary>
        public AnalyticsValue(bool value)
        {
            Type = AnalyticsValueType.Boolean;
            _text = null;
            _integer = value ? 1 : 0;
            _float = 0d;
        }

        /// <summary>Which of the four kinds of value this is.</summary>
        public AnalyticsValueType Type { get; }

        /// <summary>The text, or for the other types the value rendered as invariant text.</summary>
        public string AsString
        {
            get { return Type == AnalyticsValueType.String ? _text ?? string.Empty : ToString(); }
        }

        /// <summary>The value as a whole number: fractions are truncated, flags are 0 or 1, text is 0.</summary>
        public long AsInteger
        {
            get
            {
                switch (Type)
                {
                    case AnalyticsValueType.Integer:
                    case AnalyticsValueType.Boolean:
                        return _integer;
                    case AnalyticsValueType.Float:
                        return (long)_float;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>The value as a fraction: flags are 0 or 1, text is 0.</summary>
        public double AsFloat
        {
            get
            {
                switch (Type)
                {
                    case AnalyticsValueType.Float:
                        return _float;
                    case AnalyticsValueType.Integer:
                    case AnalyticsValueType.Boolean:
                        return _integer;
                    default:
                        return 0d;
                }
            }
        }

        /// <summary>The flag, or whether a number is non-zero. Text is false.</summary>
        public bool AsBoolean
        {
            get
            {
                switch (Type)
                {
                    case AnalyticsValueType.Boolean:
                    case AnalyticsValueType.Integer:
                        return _integer != 0;
                    case AnalyticsValueType.Float:
                        return _float != 0d;
                    default:
                        return false;
                }
            }
        }

        /// <summary>Text from a string.</summary>
        public static implicit operator AnalyticsValue(string value)
        {
            return new AnalyticsValue(value);
        }

        /// <summary>A flag from a bool.</summary>
        public static implicit operator AnalyticsValue(bool value)
        {
            return new AnalyticsValue(value);
        }

        /// <summary>A whole number from an int.</summary>
        public static implicit operator AnalyticsValue(int value)
        {
            return new AnalyticsValue((long)value);
        }

        /// <summary>A whole number from a long.</summary>
        public static implicit operator AnalyticsValue(long value)
        {
            return new AnalyticsValue(value);
        }

        /// <summary>A fraction from a float, widened to the double it was meant to be. See <see cref="Widen"/>.</summary>
        public static implicit operator AnalyticsValue(float value)
        {
            return new AnalyticsValue(Widen(value));
        }

        /// <summary>A fraction from a double.</summary>
        public static implicit operator AnalyticsValue(double value)
        {
            return new AnalyticsValue(value);
        }

        /// <summary>Same type and same value.</summary>
        public static bool operator ==(AnalyticsValue left, AnalyticsValue right)
        {
            return left.Equals(right);
        }

        /// <summary>Different type or different value.</summary>
        public static bool operator !=(AnalyticsValue left, AnalyticsValue right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// The value boxed as its natural CLR type — <see cref="string"/>, <see cref="long"/>, <see cref="double"/>
        /// or <see cref="bool"/> — for backends that take a <c>Dictionary&lt;string, object&gt;</c>.
        /// </summary>
        public object ToObject()
        {
            switch (Type)
            {
                case AnalyticsValueType.Integer:
                    return _integer;
                case AnalyticsValueType.Float:
                    return _float;
                case AnalyticsValueType.Boolean:
                    return _integer != 0;
                default:
                    return _text ?? string.Empty;
            }
        }

        /// <summary>
        /// Invariant text: <c>42.5</c> stays <c>42.5</c> on a device set to a locale that writes <c>42,5</c>, and
        /// flags are <c>true</c> / <c>false</c>. Text is returned as is, without quotes.
        /// </summary>
        public override string ToString()
        {
            switch (Type)
            {
                case AnalyticsValueType.Integer:
                    return _integer.ToString(CultureInfo.InvariantCulture);
                case AnalyticsValueType.Float:
                    return _float.ToString("R", CultureInfo.InvariantCulture);
                case AnalyticsValueType.Boolean:
                    return _integer != 0 ? "true" : "false";
                default:
                    return _text ?? string.Empty;
            }
        }

        /// <inheritdoc />
        public bool Equals(AnalyticsValue other)
        {
            if (Type != other.Type)
            {
                return false;
            }

            switch (Type)
            {
                case AnalyticsValueType.String:
                    return string.Equals(_text ?? string.Empty, other._text ?? string.Empty, StringComparison.Ordinal);
                case AnalyticsValueType.Float:
                    return _float.Equals(other._float);
                default:
                    return _integer == other._integer;
            }
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is AnalyticsValue other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            switch (Type)
            {
                case AnalyticsValueType.String:
                    return StringComparer.Ordinal.GetHashCode(_text ?? string.Empty);
                case AnalyticsValueType.Float:
                    return _float.GetHashCode();
                default:
                    return _integer.GetHashCode() ^ ((int)Type << 16);
            }
        }

        /// <summary>
        /// Widens a float to the double it was meant to be. A plain cast keeps the float's binary error, so
        /// <c>0.1f</c> would travel as <c>0.100000001490116</c>; going through the float's shortest round-trip text
        /// gives exactly <c>0.1</c>.
        /// </summary>
        private static double Widen(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return value;
            }

            return double.Parse(
                value.ToString("R", CultureInfo.InvariantCulture),
                NumberStyles.Float,
                CultureInfo.InvariantCulture);
        }
    }
}
