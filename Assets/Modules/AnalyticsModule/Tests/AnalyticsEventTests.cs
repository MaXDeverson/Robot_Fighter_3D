using System.Globalization;
using System.Threading;
using NUnit.Framework;

namespace Game.Analytics.Tests
{
    /// <summary>The event model, its values, the text they render to and the segment helpers.</summary>
    [TestFixture]
    internal sealed class AnalyticsEventTests
    {
        [Test]
        public void TheHierarchyIsJoinedWithColons()
        {
            AnalyticsEvent analyticsEvent = AnalyticsEvent.Design("player_death", "Death", "Level_003", "Acid").Build();

            Assert.That(analyticsEvent.HierarchyPath, Is.EqualTo("Death:Level_003:Acid"));
            Assert.That(analyticsEvent.Hierarchy.Count, Is.EqualTo(3));
            Assert.That(analyticsEvent.Category, Is.EqualTo(AnalyticsCategory.Design));
            Assert.That(analyticsEvent.ProgressionStatus, Is.EqualTo(ProgressionStatus.None));
        }

        [Test]
        public void ARepeatedKeyKeepsItsPlaceAndTakesTheLastValue()
        {
            AnalyticsEvent analyticsEvent = AnalyticsEvent.Design("player_death", "Death")
                .With("first", 1)
                .With("second", 2)
                .With("first", 3)
                .Build();

            Assert.That(analyticsEvent.Parameters.Count, Is.EqualTo(2));
            Assert.That(analyticsEvent.Parameters[0].Key, Is.EqualTo("first"));
            Assert.That(analyticsEvent.Parameters[0].Value.AsInteger, Is.EqualTo(3));
        }

        [Test]
        public void ChangingTheHierarchyArrayAfterwardsDoesNotReachTheEvent()
        {
            string[] hierarchy = { "Death", "Level_001" };
            AnalyticsEventBuilder builder = AnalyticsEvent.Design("player_death", hierarchy);
            hierarchy[1] = "Level_999";

            Assert.That(builder.Build().HierarchyPath, Is.EqualTo("Death:Level_001"));
        }

        [Test]
        public void ABuilderConvertsToTheEventItDescribes()
        {
            AnalyticsEvent analyticsEvent = AnalyticsEvent
                .Progression("level_complete", ProgressionStatus.Complete, "Level_003")
                .WithScore(3);

            Assert.That(analyticsEvent.ProgressionStatus, Is.EqualTo(ProgressionStatus.Complete));
            Assert.That(analyticsEvent.Value, Is.EqualTo(3d));
            Assert.That(analyticsEvent.IsTracked, Is.False);
        }

        [Test]
        public void ValuesKeepTheirType()
        {
            AnalyticsValue text = "Level_3";
            AnalyticsValue whole = 3;
            AnalyticsValue fraction = 2.5;
            AnalyticsValue flag = true;

            Assert.That(text.Type, Is.EqualTo(AnalyticsValueType.String));
            Assert.That(whole.Type, Is.EqualTo(AnalyticsValueType.Integer));
            Assert.That(fraction.Type, Is.EqualTo(AnalyticsValueType.Float));
            Assert.That(flag.Type, Is.EqualTo(AnalyticsValueType.Boolean));
            Assert.That(whole.ToObject(), Is.TypeOf<long>());
            Assert.That(flag.ToObject(), Is.EqualTo(true));
            Assert.That(flag.AsInteger, Is.EqualTo(1));
        }

        [Test]
        public void AFloatTravelsAsTheNumberItWasMeantToBe()
        {
            AnalyticsValue value = 0.1f;

            Assert.That(value.AsFloat, Is.EqualTo(0.1d));
            Assert.That(value.ToString(), Is.EqualTo("0.1"));
        }

        [Test]
        public void NumbersRenderTheSameInEveryLocale()
        {
            CultureInfo original = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("uk-UA");

                AnalyticsEvent analyticsEvent = AnalyticsEvent.Design("player_death", "Death")
                    .WithValue(12.75)
                    .With("duration", 42.5)
                    .Build();

                Assert.That(new AnalyticsValue(42.5).ToString(), Is.EqualTo("42.5"));
                Assert.That(new AnalyticsEventFormatter().FormatLine(analyticsEvent), Does.Contain("value=12.75"));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = original;
            }
        }

        [Test]
        public void TheFormatterWritesTheWholeEventOnOneLine()
        {
            AnalyticsEvent analyticsEvent = AnalyticsEvent
                .Progression("level_complete", ProgressionStatus.Complete, "Level_003")
                .WithScore(3)
                .With("duration", 42.5)
                .With("first_completion", true)
                .With("level_id", "Level_3")
                .Build();

            Assert.That(
                new AnalyticsEventFormatter().FormatLine(analyticsEvent),
                Is.EqualTo("level_complete  Progression:Complete  Level_003  score=3  " +
                           "{duration=42.5 first_completion=true level_id=\"Level_3\"}"));
        }

        [Test]
        public void AnAdEventIsAboutItsTypeAndPlacementAndCarriesTheAd()
        {
            AnalyticsEvent analyticsEvent = AnalyticsEvent
                .Ad("ad_show_fail", AdAction.FailedShow, AdType.Rewarded, "double_coins", "admob")
                .WithFailureReason(AdFailureReason.NoFill)
                .Build();

            Assert.That(analyticsEvent.Category, Is.EqualTo(AnalyticsCategory.Ad));
            Assert.That(analyticsEvent.HierarchyPath, Is.EqualTo("Rewarded:double_coins"));
            Assert.That(analyticsEvent.AdDetails.Action, Is.EqualTo(AdAction.FailedShow));
            Assert.That(analyticsEvent.AdDetails.Type, Is.EqualTo(AdType.Rewarded));
            Assert.That(analyticsEvent.AdDetails.Placement, Is.EqualTo("double_coins"));
            Assert.That(analyticsEvent.AdDetails.Network, Is.EqualTo("admob"));
            Assert.That(analyticsEvent.AdDetails.FailureReason, Is.EqualTo(AdFailureReason.NoFill));
            Assert.That(new AnalyticsEventValidator().TryValidate(analyticsEvent, out string problem), Is.True, problem);
            Assert.That(
                new AnalyticsEventFormatter().FormatLine(analyticsEvent),
                Is.EqualTo("ad_show_fail  Ad:FailedShow  Rewarded:double_coins  network=admob reason=NoFill"));
        }

        [Test]
        public void OnlyAnAdEventCarriesAnAd()
        {
            Assert.That(AnalyticsEvent.Design("player_death", "Death").Build().AdDetails, Is.Null);
            Assert.That(
                AnalyticsEvent.Progression("level_start", ProgressionStatus.Start, "Level_001").Build().AdDetails,
                Is.Null);
        }

        [Test]
        public void SanitizeSegmentReplacesWhatABackendWouldRefuse()
        {
            Assert.That(AnalyticsEventValidator.SanitizeSegment("move: intro/1"), Is.EqualTo("move_ intro_1"));
            Assert.That(AnalyticsEventValidator.SanitizeSegment("рух"), Is.EqualTo("___"));
            Assert.That(AnalyticsEventValidator.SanitizeSegment(null), Is.EqualTo("unknown"));
            Assert.That(
                AnalyticsEventValidator.SanitizeSegment(new string('a', 80)).Length,
                Is.EqualTo(AnalyticsEventValidator.MaxSegmentLength));
        }

        [Test]
        public void AWellFormedEventPassesValidation()
        {
            AnalyticsEvent analyticsEvent = AnalyticsEvent
                .Progression("tutorial_step_start", ProgressionStatus.Start, "Tutorial", "move", "03_jump_tutorial")
                .With("step_index", 3)
                .With("returned", false)
                .Build();

            bool valid = new AnalyticsEventValidator().TryValidate(analyticsEvent, out string problem);

            Assert.That(valid, Is.True, problem);
            Assert.That(problem, Is.Null);
        }
    }
}
