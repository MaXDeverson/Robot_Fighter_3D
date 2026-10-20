namespace Game.Analytics
{
    /// <summary>
    /// What kind of moment an <see cref="AnalyticsEvent"/> describes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The kinds follow the event types game analytics backends are built around — GameAnalytics uses these very
    /// names — so a sink for such a backend knows which of its calls to make without knowing anything about the
    /// game. A backend that only has flat events, such as Firebase, ignores the kind and sends
    /// <see cref="AnalyticsEvent.Name"/> with the parameters.
    /// </para>
    /// <para>
    /// Only the kinds this game reports are listed. Resources or purchases are a new member here plus whatever
    /// structured data that kind needs on <see cref="AnalyticsEvent"/>, the way <see cref="Ad"/> has
    /// <see cref="AnalyticsEvent.AdDetails"/>.
    /// </para>
    /// </remarks>
    public enum AnalyticsCategory
    {
        /// <summary>A free-form gameplay moment: a death, a choice, a feature used. One to five hierarchy segments.</summary>
        Design = 0,

        /// <summary>
        /// A step through the game's structure — a level or a tutorial started, completed or failed. Carries a
        /// <see cref="ProgressionStatus"/> and one to three hierarchy segments.
        /// </summary>
        Progression = 1,

        /// <summary>
        /// An ad shown, rewarded or failed to show. Carries <see cref="AnalyticsEvent.AdDetails"/>; its hierarchy is the
        /// ad's type and placement, <c>Interstitial:level_fail</c>.
        /// </summary>
        Ad = 2,
    }

    /// <summary>Where an <see cref="AnalyticsCategory.Progression"/> event stands.</summary>
    public enum ProgressionStatus
    {
        /// <summary>The event is not a progression event.</summary>
        None = 0,

        /// <summary>The player entered the progression.</summary>
        Start = 1,

        /// <summary>The player finished it.</summary>
        Complete = 2,

        /// <summary>The player lost it.</summary>
        Fail = 3,
    }
}
