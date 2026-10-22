using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Ads.Tests
{
    /// <summary>
    /// Full screen ads, the interstitial cooldown, the levels and the banner, on the manager built by hand. The ad
    /// block has a fixture of its own.
    /// </summary>
    [TestFixture]
    internal sealed class AdsManagerServiceTests : AdsServiceFixture
    {
        [Test]
        public void AnInterstitialIsReportedWhenItCloses()
        {
            AdsManagerService ads = Create();
            ads.ShowInterstitial(Record);

            Assert.That(Provider.InterstitialShows, Is.EqualTo(1));
            Assert.That(ads.IsShowingFullscreenAd, Is.True);
            Assert.That(Results, Is.Empty, "nothing is reported while the ad is up");

            Provider.Close(AdStatus.Completed);

            Assert.That(Results.Count, Is.EqualTo(1));
            Assert.That(Last.Type, Is.EqualTo(AdType.Interstitial));
            Assert.That(Last.IsSuccess, Is.True);
            Assert.That(ads.IsShowingFullscreenAd, Is.False);
        }

        [Test]
        public void ARewardedAdWatchedToTheEndIsASuccess()
        {
            AdsManagerService ads = Create();
            ads.ShowRewarded(Record);
            Provider.Close(AdStatus.Completed);

            Assert.That(Last.Type, Is.EqualTo(AdType.Rewarded));
            Assert.That(Last.Status, Is.EqualTo(AdStatus.Completed));
            Assert.That(Last.IsSuccess, Is.True);
        }

        [Test]
        public void ARewardedAdClosedEarlyEarnsNothing()
        {
            AdsManagerService ads = Create();
            ads.ShowRewarded(Record);
            Provider.Close(AdStatus.Cancelled);

            Assert.That(Last.Status, Is.EqualTo(AdStatus.Cancelled));
            Assert.That(Last.IsSuccess, Is.False);
        }

        [Test]
        public void AnInterstitialClosedEarlyStillCounts()
        {
            AdsManagerService ads = Create();
            ads.ShowInterstitial(Record);
            Provider.Close(AdStatus.Cancelled);

            Assert.That(Last.Status, Is.EqualTo(AdStatus.Completed));
        }

        [Test]
        public void AProviderUsingAStatusOfTheRulesIsTakenToHaveFailed()
        {
            AdsManagerService ads = Create();
            ads.ShowRewarded(Record);
            Provider.Close(AdStatus.Cooldown);

            Assert.That(Last.Status, Is.EqualTo(AdStatus.Failed));
        }

        [Test]
        public void TheCallbackRunsOnceWhenTheProviderAnswersTwice()
        {
            AdsManagerService ads = Create();
            ads.ShowRewarded(Record);
            Provider.Close(AdStatus.Completed);

            LogAssert.Expect(LogType.Warning, new Regex("closed a second time"));
            Provider.Close(AdStatus.Completed);

            Assert.That(Results.Count, Is.EqualTo(1));
        }

        [Test]
        public void AnAdAskedForWhileAnotherIsUpIsBusy()
        {
            AdsManagerService ads = Create();
            ads.ShowInterstitial(Record);
            ads.ShowRewarded(Record);

            Assert.That(Results.Count, Is.EqualTo(1), "answered at once");
            Assert.That(Last.Type, Is.EqualTo(AdType.Rewarded));
            Assert.That(Last.Status, Is.EqualTo(AdStatus.Busy));
            Assert.That(Provider.RewardedShows, Is.EqualTo(0));
            Assert.That(ads.IsAdAvailable(AdType.Rewarded), Is.False);

            Provider.Close(AdStatus.Completed);

            Assert.That(Last.Type, Is.EqualTo(AdType.Interstitial), "the first ad is still reported");
            Assert.That(Last.IsSuccess, Is.True);
        }

        [Test]
        public void AnAdTheNetworkDoesNotHaveIsNotReady()
        {
            Provider.RewardedReady = false;
            AdsManagerService ads = Create();

            Assert.That(ads.IsAdAvailable(AdType.Rewarded), Is.False);
            ads.ShowRewarded(Record);

            Assert.That(Last.Status, Is.EqualTo(AdStatus.NotReady));
            Assert.That(Provider.RewardedShows, Is.EqualTo(0));
            Assert.That(Pause.PauseCalls, Is.EqualTo(0));
        }

        [Test]
        public void AProviderThatThrowsCountsAsFailedAndFreesTheScreen()
        {
            Provider.ThrowOnShow = true;
            AdsManagerService ads = Create();

            LogAssert.Expect(LogType.Exception, new Regex("The network fell over"));
            ads.ShowInterstitial(Record);

            Assert.That(Last.Status, Is.EqualTo(AdStatus.Failed));
            Assert.That(ads.IsShowingFullscreenAd, Is.False);
            Assert.That(Pause.IsPaused, Is.False);
            Assert.That(ads.IsAdAvailable(AdType.Interstitial), Is.True, "a failed interstitial starts no cooldown");
        }

        [Test]
        public void AProviderAnsweringInsideTheShowCallIsHandled()
        {
            Provider.AnswerAtOnce = AdStatus.Completed;
            AdsManagerService ads = Create();
            ads.ShowRewarded(Record);

            Assert.That(Last.IsSuccess, Is.True);
            Assert.That(ads.IsShowingFullscreenAd, Is.False);
            Assert.That(Pause.IsPaused, Is.False);
        }

        [Test]
        public void TheGameIsPausedWhileTheAdIsUpAndRunsAgainInTheCallback()
        {
            AdsManagerService ads = Create();
            bool pausedInCallback = true;
            ads.ShowRewarded(result => pausedInCallback = Pause.IsPaused);

            Assert.That(Pause.IsPaused, Is.True);
            Provider.Close(AdStatus.Completed);

            Assert.That(pausedInCallback, Is.False);
        }

        [Test]
        public void TheGameIsNotPausedWhenTheSettingsSaySo()
        {
            AdsManagerService ads = Create(new AdsSettings(pauseGame: false, logRequests: false));
            ads.ShowRewarded(Record);
            Provider.Close(AdStatus.Completed);

            Assert.That(Pause.PauseCalls, Is.EqualTo(0));
        }

        [Test]
        public void AnInterstitialNeedsNoCallback()
        {
            AdsManagerService ads = Create();
            ads.ShowInterstitial();

            Assert.DoesNotThrow(() => Provider.Close(AdStatus.Completed));
            Assert.That(ads.IsShowingFullscreenAd, Is.False);
        }

        [Test]
        public void ACallbackThatThrowsDoesNotBreakTheManager()
        {
            AdsManagerService ads = Create();
            ads.ShowRewarded(result => { throw new InvalidOperationException("The game's handler broke."); });

            LogAssert.Expect(LogType.Exception, new Regex("handler broke"));
            Provider.Close(AdStatus.Completed);
            ads.ShowRewarded(Record);

            Assert.That(Provider.RewardedShows, Is.EqualTo(2));
        }

        [Test]
        public void TheProviderIsInitializedOnce()
        {
            AdsManagerService ads = Create();
            ads.ShowBanner();
            ads.ShowRewarded(Record);

            Assert.That(Provider.InitializeCalls, Is.EqualTo(1));
        }

        [Test]
        public void TheNetworkDoesNotStartBeforeThePrivacyChoicesAllowAds()
        {
            FakeConsentSource consent = new FakeConsentSource();
            AdsManagerService ads = Create(consent: consent);

            ads.ShowInterstitial(Record);
            ads.ShowRewarded(Record);

            Assert.That(Provider.InitializeCalls, Is.Zero);
            Assert.That(Provider.InterstitialShows + Provider.RewardedShows, Is.Zero);
            Assert.That(Results, Has.All.Matches<AdResult>(result => result.Status == AdStatus.NotReady));
            Assert.That(ads.IsAdAvailable(AdType.Interstitial), Is.False);
            Assert.That(Provider.InitializeCalls, Is.Zero, "asking whether an ad is there does not start the network");

            consent.Set(true);

            Assert.That(Provider.InitializeCalls, Is.EqualTo(1), "started the moment the choices allow it");
            ads.ShowInterstitial(Record);
            Assert.That(Provider.InterstitialShows, Is.EqualTo(1));
        }

        [Test]
        public void ARequestedBannerComesUpOnceThePrivacyChoicesAllowAds()
        {
            FakeConsentSource consent = new FakeConsentSource();
            AdsManagerService ads = Create(consent: consent);

            ads.ShowBanner();
            TickAfter(ads, AdsManagerService.RefreshInterval);

            Assert.That(ads.IsBannerVisible, Is.False);
            Assert.That(ads.IsAdAvailable(AdType.Banner), Is.False);
            Assert.That(Provider.BannerShows, Is.Zero);

            consent.Set(true);

            Assert.That(ads.IsBannerVisible, Is.True);
            Assert.That(Provider.BannerOnScreen, Is.True);
        }

        [Test]
        public void ANetworkAllowedFromTheStartStartsAtOnceAndDisposeLetsGoOfTheSource()
        {
            FakeConsentSource consent = new FakeConsentSource();
            consent.Set(true);
            AdsManagerService ads = Create(consent: consent);

            Assert.That(Provider.InitializeCalls, Is.EqualTo(1));
            Assert.That(consent.Subscribers, Is.EqualTo(1));

            ads.Dispose();

            Assert.That(consent.Subscribers, Is.Zero);
        }

        [Test]
        public void DisposingWithAnAdUpResumesTheGameAndDropsTheCallback()
        {
            AdsManagerService ads = Create();
            List<AdResult> announced = new List<AdResult>();
            ads.FullscreenAdFinished += announced.Add;
            ads.ShowRewarded(Record);
            ads.Dispose();

            Assert.That(Pause.IsPaused, Is.False);
            Provider.Close(AdStatus.Completed);
            Assert.That(Results, Is.Empty);
            Assert.That(announced, Is.Empty);
        }

        [Test]
        public void EveryAnswerIsAnnouncedWithItsPlacementAndNetworkJustBeforeTheCallback()
        {
            AdsManagerService ads = Create();
            List<string> order = new List<string>();
            AdResult announced = default(AdResult);
            ads.FullscreenAdFinished += result =>
            {
                announced = result;
                order.Add("announced");
            };

            ads.ShowInterstitial(result => order.Add("callback"), "level_fail");
            Assert.That(order, Is.Empty, "nothing is announced while the ad is up");
            Provider.Close(AdStatus.Completed);

            Assert.That(order, Is.EqualTo(new[] { "announced", "callback" }));
            Assert.That(announced.Type, Is.EqualTo(AdType.Interstitial));
            Assert.That(announced.Status, Is.EqualTo(AdStatus.Completed));
            Assert.That(announced.Placement, Is.EqualTo("level_fail"));
            Assert.That(announced.Network, Is.EqualTo("fake"));
        }

        [Test]
        public void AnAdTheRulesHoldBackIsAnnouncedToo()
        {
            AdsManagerService ads = Create();
            List<AdResult> announced = new List<AdResult>();
            ads.FullscreenAdFinished += announced.Add;

            ads.ShowInterstitial(Record, "level_fail");
            ads.ShowRewarded(Record, "double_coins");

            Assert.That(announced.Count, Is.EqualTo(1), "the interstitial is still up");
            Assert.That(announced[0].Status, Is.EqualTo(AdStatus.Busy));
            Assert.That(announced[0].Placement, Is.EqualTo("double_coins"));
        }

        [Test]
        public void WithoutAPlacementTheTypeNamesIt()
        {
            AdsManagerService ads = Create();
            ads.ShowInterstitial(Record);
            Provider.Close(AdStatus.Completed);
            ads.ShowRewarded(Record);
            Provider.Close(AdStatus.Completed);

            Assert.That(Results[0].Placement, Is.EqualTo("interstitial"));
            Assert.That(Results[1].Placement, Is.EqualTo("rewarded"));
        }

        [Test]
        public void AListenerThatThrowsKeepsTheResultFromNobodyElse()
        {
            AdsManagerService ads = Create();
            List<AdResult> announced = new List<AdResult>();
            ads.FullscreenAdFinished += result => { throw new InvalidOperationException("The listener broke."); };
            ads.FullscreenAdFinished += announced.Add;

            LogAssert.Expect(LogType.Exception, new Regex("listener broke"));
            ads.ShowRewarded(Record);
            Provider.Close(AdStatus.Completed);

            Assert.That(announced.Count, Is.EqualTo(1));
            Assert.That(Results.Count, Is.EqualTo(1));
        }

        [Test]
        public void AProviderWithoutANameIsReportedAsUnknown()
        {
            Provider.Network = null;
            AdsManagerService ads = Create();
            ads.ShowRewarded(Record);
            Provider.Close(AdStatus.Completed);

            Assert.That(Last.Network, Is.EqualTo("unknown"));
        }

        [Test]
        public void AnInterstitialWithinTheCooldownIsHeldBack()
        {
            AdsManagerService ads = Create();
            ads.ShowInterstitial(Record);
            Provider.Close(AdStatus.Completed);

            Clock.AdvanceSeconds(179);
            ads.ShowInterstitial(Record);

            Assert.That(Last.Status, Is.EqualTo(AdStatus.Cooldown));
            Assert.That(Provider.InterstitialShows, Is.EqualTo(1));

            Clock.AdvanceSeconds(1);
            Assert.That(ads.IsAdAvailable(AdType.Interstitial), Is.True);
        }

        [Test]
        public void TheCooldownCountsFromTheClose()
        {
            AdsManagerService ads = Create();
            ads.ShowInterstitial(Record);
            Clock.AdvanceSeconds(60);
            Provider.Close(AdStatus.Completed);

            Clock.AdvanceSeconds(179);
            Assert.That(ads.IsAdAvailable(AdType.Interstitial), Is.False, "239 s after it was shown, 179 s after it closed");

            Clock.AdvanceSeconds(1);
            Assert.That(ads.IsAdAvailable(AdType.Interstitial), Is.True);
        }

        [Test]
        public void AnInterstitialThatWasNotShownStartsNoCooldown()
        {
            AdsManagerService ads = Create();
            ads.ShowInterstitial(Record);
            Provider.Close(AdStatus.Failed);

            Assert.That(ads.IsAdAvailable(AdType.Interstitial), Is.True);
        }

        [Test]
        public void ARewardedAdRestartsTheInterstitialCooldown()
        {
            AdsManagerService ads = Create();
            ads.ShowRewarded(Record);
            Provider.Close(AdStatus.Cancelled);

            ads.ShowInterstitial(Record);
            Assert.That(Last.Status, Is.EqualTo(AdStatus.Cooldown), "watched in part, still watched");
        }

        [Test]
        public void ARewardedAdLeavesTheCooldownAloneWhenTheSettingsSaySo()
        {
            AdsManagerService ads = Create(new AdsSettings(rewardedRestartsInterstitialCooldown: false, logRequests: false));
            ads.ShowRewarded(Record);
            Provider.Close(AdStatus.Completed);

            Assert.That(ads.IsAdAvailable(AdType.Interstitial), Is.True);
        }

        [Test]
        public void EachTypeOfAdWaitsForItsLevel()
        {
            Levels.Level = 5;
            AdsManagerService ads = Create();

            Assert.That(ads.IsAdAvailable(AdType.Banner), Is.False);
            Assert.That(ads.IsAdAvailable(AdType.Interstitial), Is.False);
            Assert.That(ads.IsAdAvailable(AdType.Rewarded), Is.True);

            Levels.Level = 6;
            Assert.That(ads.IsAdAvailable(AdType.Interstitial), Is.True);
            Assert.That(ads.IsAdAvailable(AdType.Banner), Is.False);

            Levels.Level = 10;
            Assert.That(ads.IsAdAvailable(AdType.Banner), Is.True);
        }

        [Test]
        public void ALockedInterstitialIsNotShown()
        {
            Levels.Level = 5;
            AdsManagerService ads = Create();
            ads.ShowInterstitial(Record);

            Assert.That(Last.Status, Is.EqualTo(AdStatus.LevelLocked));
            Assert.That(Provider.InterstitialShows, Is.EqualTo(0));
        }

        [Test]
        public void TheRewardedLevelIsRespectedToo()
        {
            Levels.Level = 2;
            AdsManagerService ads = Create(new AdsSettings(rewardedFromLevel: 3, logRequests: false));
            ads.ShowRewarded(Record);

            Assert.That(Last.Status, Is.EqualTo(AdStatus.LevelLocked));
        }

        [Test]
        public void WithoutALevelSourceThereAreNoLevelLimits()
        {
            LogAssert.Expect(LogType.Warning, new Regex("No IAdsLevelSource"));
            AdsManagerService ads = Create(withLevels: false);

            Assert.That(ads.IsAdAvailable(AdType.Banner), Is.True);
            Assert.That(ads.IsAdAvailable(AdType.Interstitial), Is.True);
        }

        [Test]
        public void ALevelSourceThatFailsHoldsTheLimitedAdsBackAndIsReportedOnce()
        {
            Levels.Throw = true;
            AdsManagerService ads = Create();

            LogAssert.Expect(LogType.Exception, new Regex("progress is not loaded"));
            ads.ShowInterstitial(Record);

            Assert.That(Last.Status, Is.EqualTo(AdStatus.LevelLocked));
            Assert.That(ads.IsAdAvailable(AdType.Banner), Is.False);
        }

        [Test]
        public void TheBannerWaitsForItsLevelAndAppearsOnItsOwn()
        {
            Levels.Level = 9;
            AdsManagerService ads = Create();
            ads.ShowBanner();

            Assert.That(ads.IsBannerVisible, Is.False);
            Assert.That(Provider.BannerOnScreen, Is.False);

            Levels.Level = 10;
            TickAfter(ads, 0.5);
            Assert.That(ads.IsBannerVisible, Is.False, "the rules are looked at once a second, not every frame");

            TickAfter(ads, 0.5);
            Assert.That(ads.IsBannerVisible, Is.True);
            Assert.That(Provider.BannerOnScreen, Is.True);
        }

        [Test]
        public void HideBannerWithdrawsTheRequest()
        {
            AdsManagerService ads = Create();
            ads.ShowBanner();
            Assert.That(ads.IsBannerVisible, Is.True);

            ads.HideBanner();
            TickAfter(ads, AdsManagerService.RefreshInterval);

            Assert.That(ads.IsBannerVisible, Is.False);
            Assert.That(Provider.BannerOnScreen, Is.False);
        }

        [Test]
        public void AskingForTheBannerTwiceShowsItOnce()
        {
            AdsManagerService ads = Create();
            ads.ShowBanner();
            ads.ShowBanner();

            Assert.That(Provider.BannerShows, Is.EqualTo(1));
        }

        [Test]
        public void TheBannerAreaIsTheProvidersWhileTheBannerIsUp()
        {
            Rect strip = new Rect(0, 980, 1920, 100);
            Provider.BannerArea = strip;
            AdsManagerService ads = Create();

            Assert.That(ads.BannerScreenArea, Is.EqualTo(Rect.zero), "not asked for yet");

            ads.ShowBanner();
            Assert.That(ads.BannerScreenArea, Is.EqualTo(strip));

            ads.HideBanner();
            Assert.That(ads.BannerScreenArea, Is.EqualTo(Rect.zero));
        }

        [Test]
        public void TheBannerAreaIsEmptyWhileTheRulesHoldTheBannerBack()
        {
            Levels.Level = 9;
            Provider.BannerArea = new Rect(0, 980, 1920, 100);
            AdsManagerService ads = Create();
            ads.ShowBanner();

            Assert.That(ads.BannerScreenArea, Is.EqualTo(Rect.zero));
        }

        [Test]
        public void AProviderThatFailsToPlaceTheBannerLeavesTheScreenFree()
        {
            Provider.ThrowOnBannerArea = true;
            AdsManagerService ads = Create();
            ads.ShowBanner();

            LogAssert.Expect(LogType.Exception, new Regex("fell over"));
            Assert.That(ads.BannerScreenArea, Is.EqualTo(Rect.zero));
        }
    }
}
