using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Ads.Tests
{
    /// <summary>
    /// The ad block through the manager: what it stops, how temporary blocks add up and run out, and what survives a
    /// restart.
    /// </summary>
    [TestFixture]
    internal sealed class AdBlockTests : AdsServiceFixture
    {
        [Test]
        public void AnAdBlockStopsBannersAndInterstitialsButNotRewardedAds()
        {
            AdsManagerService ads = Create();
            ads.AddAdBlock(7);

            Assert.That(ads.IsAdBlockActive, Is.True);
            Assert.That(ads.IsAdAvailable(AdType.Banner), Is.False);

            ads.ShowInterstitial(Record);
            Assert.That(Last.Status, Is.EqualTo(AdStatus.AdBlock));
            Assert.That(Provider.InterstitialShows, Is.EqualTo(0));

            ads.ShowRewarded(Record);
            Provider.Close(AdStatus.Completed);
            Assert.That(Last.Type, Is.EqualTo(AdType.Rewarded));
            Assert.That(Last.IsSuccess, Is.True);
        }

        [Test]
        public void AnAdBlockTakesTheBannerAwayAtOnce()
        {
            AdsManagerService ads = Create();
            ads.ShowBanner();
            Assert.That(ads.IsBannerVisible, Is.True);

            ads.AddPermanentAdBlock();

            Assert.That(ads.IsBannerVisible, Is.False, "without waiting for the next refresh");
            Assert.That(Provider.BannerOnScreen, Is.False);
        }

        [Test]
        public void TheBannerComesBackWhenTheAdBlockRunsOut()
        {
            AdsManagerService ads = Create();
            ads.ShowBanner();
            ads.AddAdBlock(TimeSpan.FromHours(1));
            Assert.That(ads.IsBannerVisible, Is.False);

            TickAfter(ads, TimeSpan.FromHours(1).TotalSeconds);

            Assert.That(ads.IsAdBlockActive, Is.False);
            Assert.That(ads.IsBannerVisible, Is.True);
            Assert.That(Provider.BannerOnScreen, Is.True);
        }

        [Test]
        public void DaysAddUp()
        {
            AdsManagerService ads = Create();
            DateTime start = Clock.UtcNow;

            ads.AddAdBlock(3);
            Clock.Advance(TimeSpan.FromDays(1));
            ads.AddAdBlock(7);

            Assert.That(ads.AdBlockEndsAtUtc, Is.EqualTo(start.AddDays(10)));
        }

        [Test]
        public void AnAdBlockAddedAfterTheLastRanOutStartsFromNow()
        {
            AdsManagerService ads = Create();
            ads.AddAdBlock(1);
            Clock.Advance(TimeSpan.FromDays(2));
            ads.AddAdBlock(1);

            Assert.That(ads.AdBlockEndsAtUtc, Is.EqualTo(Clock.UtcNow.AddDays(1)));
        }

        [Test]
        public void ATemporaryAdBlockRunsOut()
        {
            AdsManagerService ads = Create();
            ads.AddAdBlock(2);

            Clock.Advance(TimeSpan.FromDays(2) - TimeSpan.FromSeconds(1));
            Assert.That(ads.IsAdBlockActive, Is.True);

            Clock.AdvanceSeconds(1);
            Assert.That(ads.IsAdBlockActive, Is.False);
            Assert.That(ads.AdBlockEndsAtUtc, Is.Null);
            Assert.That(ads.IsAdAvailable(AdType.Interstitial), Is.True);
        }

        [Test]
        public void APermanentAdBlockNeverRunsOut()
        {
            AdsManagerService ads = Create();
            ads.AddPermanentAdBlock();

            TickAfter(ads, TimeSpan.FromDays(3650).TotalSeconds);

            Assert.That(ads.IsAdBlockActive, Is.True);
            Assert.That(ads.IsAdBlockPermanent, Is.True);
            Assert.That(ads.AdBlockEndsAtUtc, Is.Null);
        }

        [Test]
        public void ATemporaryAdBlockSurvivesARestart()
        {
            AdsManagerService before = Create();
            before.AddAdBlock(5);
            DateTime? endsAt = before.AdBlockEndsAtUtc;

            AdsManagerService after = Create();

            Assert.That(after.IsAdBlockActive, Is.True);
            Assert.That(after.AdBlockEndsAtUtc, Is.EqualTo(endsAt));
            Assert.That(Storage.SaveCalls, Is.GreaterThan(0), "written to disk at once, not at some later save");
        }

        [Test]
        public void APermanentAdBlockSurvivesARestart()
        {
            Create().AddPermanentAdBlock();

            AdsManagerService after = Create();

            Assert.That(after.IsAdBlockActive, Is.True);
            Assert.That(after.IsAdBlockPermanent, Is.True);
        }

        [Test]
        public void AnAdBlockThatRanOutWhileTheGameWasClosedIsForgottenQuietly()
        {
            Create().AddAdBlock(1);
            Clock.Advance(TimeSpan.FromDays(2));

            AdsManagerService after = Create();
            int changes = 0;
            after.AdBlockChanged += () => changes++;
            TickAfter(after, AdsManagerService.RefreshInterval);

            Assert.That(after.IsAdBlockActive, Is.False);
            Assert.That(Storage.Has(AdBlock.EndsAtKey), Is.False);
            Assert.That(changes, Is.EqualTo(0), "it ended before this session began");
        }

        [Test]
        public void ADamagedStoredValueMeansNoAdBlock()
        {
            Storage.SetString(AdBlock.EndsAtKey, "not a number");

            AdsManagerService ads = Create();

            Assert.That(ads.IsAdBlockActive, Is.False);
        }

        [Test]
        public void RemoveAdBlockClearsBothKindsForGood()
        {
            AdsManagerService ads = Create();
            ads.AddPermanentAdBlock();
            ads.AddAdBlock(3);

            ads.RemoveAdBlock();

            Assert.That(ads.IsAdBlockActive, Is.False);
            Assert.That(ads.IsAdBlockPermanent, Is.False);
            Assert.That(Create().IsAdBlockActive, Is.False, "and after a restart");
        }

        [Test]
        public void AdBlockChangedIsRaisedOnAddRemoveAndRunningOut()
        {
            AdsManagerService ads = Create();
            int changes = 0;
            ads.AdBlockChanged += () => changes++;

            ads.AddAdBlock(TimeSpan.FromHours(1));
            ads.RemoveAdBlock();
            ads.AddAdBlock(TimeSpan.FromHours(1));
            TickAfter(ads, TimeSpan.FromHours(1).TotalSeconds);

            Assert.That(changes, Is.EqualTo(4));
        }

        [Test]
        public void ATemporaryBlockRunningOutUnderAPermanentOneChangesNothing()
        {
            AdsManagerService ads = Create();
            ads.AddAdBlock(TimeSpan.FromHours(1));
            ads.AddPermanentAdBlock();
            int changes = 0;
            ads.AdBlockChanged += () => changes++;

            TickAfter(ads, TimeSpan.FromHours(1).TotalSeconds);

            Assert.That(changes, Is.EqualTo(0));
            Assert.That(ads.IsAdBlockActive, Is.True);
        }

        [Test]
        public void AListenerThatThrowsDoesNotKeepTheNewsFromTheOthers()
        {
            AdsManagerService ads = Create();
            bool heard = false;
            ads.AdBlockChanged += () => { throw new InvalidOperationException("The shop screen broke."); };
            ads.AdBlockChanged += () => heard = true;

            LogAssert.Expect(LogType.Exception, new Regex("shop screen broke"));
            ads.AddPermanentAdBlock();

            Assert.That(heard, Is.True);
        }

        [Test]
        public void DurationsThatAreNotPositiveAreIgnored()
        {
            AdsManagerService ads = Create();

            LogAssert.Expect(LogType.Warning, new Regex("0 days was ignored"));
            ads.AddAdBlock(0);
            LogAssert.Expect(LogType.Warning, new Regex("-3 days was ignored"));
            ads.AddAdBlock(-3);
            LogAssert.Expect(LogType.Warning, new Regex("has to be longer than zero"));
            ads.AddAdBlock(TimeSpan.Zero);

            Assert.That(ads.IsAdBlockActive, Is.False);
        }

        [Test]
        public void AnEnormousAdBlockDoesNotOverflow()
        {
            AdsManagerService ads = Create();

            Assert.DoesNotThrow(() => ads.AddAdBlock(int.MaxValue));
            Assert.DoesNotThrow(() => ads.AddAdBlock(TimeSpan.MaxValue));

            Assert.That(ads.IsAdBlockActive, Is.True);
            Assert.That(ads.AdBlockEndsAtUtc, Is.EqualTo(DateTime.MaxValue));
        }
    }
}
