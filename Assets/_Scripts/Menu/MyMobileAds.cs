
using System;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;

public static class MyMobileAds
{
    private static string _adUnitId = "ca-app-pub-9558178408201758/2219575169";
    private static string _adRevardID = "ca-app-pub-9558178408201758/1246308808";
    private static InterstitialAd interstitialAd;
    private static RewardedAd rewardedAd;
    private static bool _isError;
    private static bool _isRewardError;

    private static Action _interstitialAction;
    private static Action _rewardAction;

    public static void Init()
    {
        MobileAds.Initialize(initStatus => { });
        LoadInterstitialAd();
        LoadRewardedAd();

    }
    private static void LoadInterstitialAd()
    {
        // Clean up the old ad before loading a new one.
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        Debug.Log("Loading the interstitial ad.");

        // create our request used to load the ad.
        var adRequest = new AdRequest();
        adRequest.Keywords.Add("unity-admob-sample");

        // send the request to load the ad.
        InterstitialAd.Load(_adUnitId, adRequest,
            (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    _isError = true;
                    return;
                }
                interstitialAd = ad;
                RegisterEventHandlers(interstitialAd);
                _isError = false;
            });
    }

    private static void LoadRewardedAd()
    {
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }
        var adRequest = new AdRequest();
        adRequest.Keywords.Add("unity-admob-sample");
        RewardedAd.Load(_adRevardID, adRequest,
            (RewardedAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    _isRewardError = true;
                    return;
                }
                _isRewardError = false;
                rewardedAd = ad;
                RewardRegisterReloadHandler(rewardedAd);
            });
    }

    private static int _interstitialCounter = 0;
    public static void ShowAd(Action afterAd)
    {

        if (PlayerPrefs.GetInt("ADSUNLOCK") == 0)
        {
            if(_interstitialCounter++ >= 1)
            {
                _interstitialCounter = 0;
                _interstitialAction = afterAd;
                if (_isError)
                {
                    LoadInterstitialAd();
                    afterAd?.Invoke();
                    return;
                }
                if (interstitialAd != null && interstitialAd.CanShowAd())
                {
                    Debug.Log("Showing interstitial ad.");
                    interstitialAd.Show();
                }
                else
                {
                    //LoadInterstitialAd();
                    afterAd?.Invoke();
                }
            }
            else
            {
                afterAd?.Invoke();
            }
        }
        else
        {
            afterAd?.Invoke();
        }
    }

    public static void ShowRewarded(Action afterAdAction)
    {

        Debug.Log("Show Rewarded invoke");
        if(PlayerPrefs.GetInt("ADSUNLOCK") == 0){
            _rewardAction = afterAdAction;
            if (_isRewardError)
            {
                LoadRewardedAd();
                afterAdAction?.Invoke();
                return;
            }
            const string rewardMsg = "Rewarded ad rewarded the user. Type: {0}, amount: {1}.";
            if (rewardedAd != null && rewardedAd.CanShowAd())
            {
                rewardedAd.Show((Reward reward) =>
                {

                });
                Debug.Log("Show AD");
            }
            else
            {
                LoadRewardedAd();
                afterAdAction?.Invoke();
            }
        }
        else
        {
            Debug.Log("ADBLOCK");
            afterAdAction?.Invoke();
        }
       
    }

    public static bool ShowRewardedSucssesCheck(Action afterAdAction)
    {

        Debug.Log("Show Rewarded invoke");
        if (PlayerPrefs.GetInt("ADSUNLOCK") == 0)
        {
            _rewardAction = afterAdAction;
            if (_isRewardError)
            {
                LoadRewardedAd();
                return false;
            }
            if (rewardedAd != null && rewardedAd.CanShowAd())
            {
                rewardedAd.Show((Reward reward) =>
                {

                });
                Debug.Log("Show AD");
            }
            else
            {
                LoadRewardedAd();
                return false;
            }
        }
        else
        {
            Debug.Log("ADBLOCK");
            afterAdAction?.Invoke();
        }
        return true;
    }


    private static void RegisterEventHandlers(InterstitialAd ad)
    {
        // Raised when the ad is estimated to have earned money.
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("Interstitial ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
        };
        // Raised when an impression is recorded for an ad.
        ad.OnAdImpressionRecorded += () =>
        {

        };
        // Raised when a click is recorded for an ad.
        ad.OnAdClicked += () =>
        {

        };
        // Raised when an ad opened full screen content.
        ad.OnAdFullScreenContentOpened += () =>
        {

        };
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            LoadInterstitialAd();
            _interstitialAction?.Invoke();
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            LoadInterstitialAd();
            _interstitialAction?.Invoke();
        };
    }
    private static void RewardRegisterReloadHandler(RewardedAd ad)
    {
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            LoadRewardedAd();
            _rewardAction?.Invoke();
        };
        //// Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            _rewardAction?.Invoke();
            LoadRewardedAd();
        };
    }

}