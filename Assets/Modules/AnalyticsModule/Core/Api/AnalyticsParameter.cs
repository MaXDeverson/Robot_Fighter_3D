namespace Game.Analytics
{
    /// <summary>One named value on an <see cref="AnalyticsEvent"/> — a custom field, in backend terms.</summary>
    public readonly struct AnalyticsParameter
    {
        /// <summary>Creates the parameter.</summary>
        public AnalyticsParameter(string key, AnalyticsValue value)
        {
            Key = key;
            Value = value;
        }

        /// <summary>Lower snake case name, for example <c>level_deaths</c>.</summary>
        public string Key { get; }

        /// <summary>The typed value.</summary>
        public AnalyticsValue Value { get; }

        /// <summary><c>key=value</c>, invariant.</summary>
        public override string ToString()
        {
            return Key + "=" + Value.ToString();
        }
    }
}
