# AdMob implementation

The AdMob network of the [Ads Manager Module](../../../Modules/AdsManagerModule/README.md): one `IAdsProvider`, in an
assembly of its own (`Game.Ads.AdmobImplementation`, let in by `InternalsVisibleTo` in the module's
`AssemblyInfo.cs`). It is the only code in the game that calls the Google Mobile Ads SDK
(`com.google.ads.mobile`). Game code shows ads through `IAdsManagerService` as before.

**Interstitials only.** Banners and rewarded ads do not come from AdMob: `IsAdAvailable` is `false` for them,
`ShowBanner` / `HideBanner` do nothing, `ShowRewarded` answers `NotReady` at once.

---

## Setup

1. `Assets/Modules/AdsManagerModule/Prefabs/AdmobAdsProvider.prefab` holds `AdmobAdsProvider` on its root, with the
   ad unit IDs.
2. On `AdsManagerInstaller` (`Assets/_Pefabs/GlobalServices/AdsService.prefab`) assign that prefab to
   **Network Provider Prefab** and set **Provider** to `Network`. The manager then creates AdMob under the
   `ProjectContext` instead of the stub; `Base` brings the stub back.
3. App IDs: `Assets > Google Mobile Ads > Settings` (`Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset`).
   Every platform the game ships on needs one; the iOS app crashes on start without it.

## Inspector

| Field | Meaning |
| --- | --- |
| `Android Interstitial Unit Id`, `Ios Interstitial Unit Id` | The interstitial ad units from the AdMob console. |
| `Test Ads In Development Builds` | On (default): the editor and development builds load Google's test ad unit instead. Release builds always use the IDs above. |
| `Retry Delay` | Seconds before the next attempt after a failed load; doubles with every failure in a row. |
| `Max Retry Delay` | The longest wait between two attempts. |

`Network` is `admob` — the name the ad events carry to analytics.

## How it works

* `Initialize` (called by the manager at the start of the `ProjectContext`) starts the SDK; the first interstitial is
  loaded once it is up.
* One interstitial is kept loaded. The next one is loaded as soon as the previous one has closed or failed to show; a
  loaded one older than 55 minutes is thrown away and reloaded, since AdMob expires it after an hour.
* The SDK answers from its own thread; every answer goes through `MobileAdsEventExecutor.ExecuteInUpdate` to the main
  thread before the manager hears of it.
* In the editor the SDK shows its own placeholder ad (`1024x768`), which closes with its button.

Not done here: the consent form (Google UMP) that the EEA and the UK require before ads are requested.
