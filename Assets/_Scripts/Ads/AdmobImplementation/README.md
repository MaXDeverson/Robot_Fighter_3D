# AdMob implementation

The AdMob network of the [Ads Manager Module](../../../Modules/AdsManagerModule/README.md): one `IAdsProvider`, in an
assembly of its own (`Game.Ads.AdmobImplementation`, let in by `InternalsVisibleTo` in the module's
`AssemblyInfo.cs`). It is the only code in the game that calls the Google Mobile Ads SDK
(`com.google.ads.mobile`). Game code shows ads through `IAdsManagerService` as before.

Banners, interstitials and rewarded ads all come from AdMob. A type of ad with no ad unit ID for the platform is
never loaded: an error says so at start, `IsAdAvailable` is `false` for it and showing it answers `NotReady`.

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
| `Android Banner Unit Id`, `Ios Banner Unit Id` | The banner ad units from the AdMob console. |
| `Android Interstitial Unit Id`, `Ios Interstitial Unit Id` | The interstitial ad units from the AdMob console. |
| `Android Rewarded Unit Id`, `Ios Rewarded Unit Id` | The rewarded ad units from the AdMob console. |
| `Banner Position` | The edge the banner is anchored to; `Bottom` by default. |
| `Test Ads In Development Builds` | On (default): the editor and development builds load Google's test ad units instead. Release builds always use the IDs above. |
| `Retry Delay` | Seconds before the next attempt after a failed load; doubles with every failure in a row. |
| `Max Retry Delay` | The longest wait between two attempts. |

`Network` is `admob` — the name the ad events carry to analytics.

## How it works

* `Initialize` (called by the manager at the start of the `ProjectContext`) starts the SDK; the first interstitial and
  the first rewarded ad are loaded once it is up.
* One interstitial and one rewarded ad are kept loaded. The next one of a type is loaded as soon as the previous one
  has closed or failed to show; a loaded one older than 55 minutes is thrown away and reloaded, since AdMob expires it
  after an hour. A failed load is tried again after `Retry Delay`, doubled with every failure in a row.
* One full screen ad at a time: asked for one while another is up, the provider answers `Failed`.
* A rewarded ad answers `Completed` once the SDK has granted its reward, `Cancelled` when it was closed before.
* The banner is an [anchored adaptive](https://developers.google.com/admob/unity/banner/anchored-adaptive) one: as wide
  as the screen's safe area, its height picked by AdMob. Its view is created and loaded the first time the manager shows
  the banner (or as soon as the SDK is up, if that comes later), and `HideBanner` only hides it. A failed first load is
  tried again on the same view, as Google advises; once it has an ad, AdMob refreshes it at the rate set for the ad unit
  in the console, and only while it is on screen. Its size is chosen for the orientation the game has when it is created.
* The SDK answers from its own thread; every answer goes through `MobileAdsEventExecutor.ExecuteInUpdate` to the main
  thread before the manager hears of it.
* In the editor the SDK shows its own placeholder ads: the full screen ones close with their button.

Not done here: the consent form (Google UMP) that the EEA and the UK require before ads are requested.
