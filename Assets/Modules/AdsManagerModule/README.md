# Ads Manager Module

Shows the game's ads — banner, interstitial, rewarded — and decides whether they may be shown at all: the player's ad
block, the level each type of ad starts at, the interstitial cooldown. Today the ads come from `BaseAdsProvider`, a stub
that puts up a black screen with a timer; a real ad network replaces it later without game code noticing.

---

## The rule: everything goes through `AdsManagerService`

**All interaction with this module from outside it goes through `AdsManagerService`, and only through it.** Game code
injects `IAdsManagerService` — the interface `AdsManagerService` implements — and nothing else:

```csharp
[Inject] private IAdsManagerService _ads;
```

That means game code never:

* references a provider — `BaseAdsProvider` today, an SDK provider tomorrow — or calls an ad SDK directly;
* touches the ad canvas, the `BaseAdsProvider` GameObject or anything under the `Ads` object of the `ProjectContext`;
* reads or writes the module's PlayerPrefs keys (`Ads.*`), or keeps its own copy of the ad block;
* checks levels, cooldowns or the ad block itself before asking for an ad — asking is the check, and `IsAdAvailable` is
  there for a button that should only appear when an ad would be shown, or a flow that goes another way without one.

It is enforced, not only written down. The module is one assembly, `Game.Ads`, and everything in it is `internal`
except the manager's interface and the types it speaks in:

| Public type | What it is |
| --- | --- |
| `IAdsManagerService` | The ads manager. |
| `AdResult`, `AdStatus` | What a request for a full screen ad came to. |
| `AdType` | Banner, interstitial, rewarded. |
| `IAdsLevelSource` | How far the player has got — asked of the game (see [Levels](#levels)). |
| `IAdsConsentSource` | Whether the player's privacy choices allow ads — asked of the game (see [Consent](#consent)). |

`BaseAdsProvider`, `IAdsProvider`, `AdsManagerService` itself, the config, the storage and the installer cannot even be
named from outside the module. `AdsModuleBoundaryTests` fails the moment anything else turns public.

```
 game code ──IAdsManagerService──► AdsManagerService ──IAdsProvider──► BaseAdsProvider (stub, today)
                                        │                               SDK provider     (later)
         ad block · levels · cooldown · one full screen ad at a time
                                        │
                     AdBlock ──► PlayerPrefs "Ads.*"        IAdsLevelSource ◄── ProgressionAdsLevelSource
                                                          IAdsConsentSource ◄── PrivacyAdsConsentSource
                                                                                 (game side, BootInstaller)
```

---

## Using it

```csharp
// After a loss. The callback comes when the ad has closed, or at once when it is not shown,
// so the flow goes on either way. The last argument names the placement for analytics.
_ads.ShowInterstitial(result => RestartLevel(), "level_fail");

// For a reward. Give it only on success: the player watched to the end.
_ads.ShowRewarded(result =>
{
    if (result.IsSuccess)
        _resources.Add(ResourceType.Coins, 50);
}, "double_coins");

// A "watch an ad" button that only appears when an ad would really be shown.
_watchAdButton.SetActive(_ads.IsAdAvailable(AdType.Rewarded));

// Banner: a request that stays until withdrawn.
_ads.ShowBanner();
_ads.HideBanner();

// Where it is, to keep UI clear of it: screen pixels like Screen.safeArea, empty while there is none.
Rect banner = _ads.BannerScreenArea;

// Ad block: the "remove ads" purchase, a promo code, a reward.
_ads.AddPermanentAdBlock();
_ads.AddAdBlock(days: 7);
_ads.AdBlockChanged += RefreshShopButtons;
```

Every callback is invoked **exactly once**: when the ad closes, or straight away — possibly before the `Show` call
returns — when it is not shown. A second answer from a misbehaving provider is ignored; a provider that throws counts as
`Failed`; a callback that throws is logged and does not break the manager. The game is running again by the time the
callback runs (see [Pausing](#pausing)). Only when the game quits with an ad on screen is the callback dropped.

### What the callback gets

`AdResult.IsSuccess` is true for `Completed` only. `AdResult.Status` says why not:

| `AdStatus` | Interstitial | Rewarded |
| --- | --- | --- |
| `Completed` | Was on screen — closed early too, an interstitial has no reward to miss. | Watched to the end: **give the reward**. |
| `Cancelled` | — | Closed before the end. No reward. |
| `AdBlock` | The player has an ad block. | — (an ad block never stops a rewarded ad) |
| `LevelLocked` | The player has not reached the interstitial level. | The player has not reached the rewarded level. |
| `Cooldown` | The previous interstitial was too recent. | — |
| `NotReady` | The network has no ad loaded. | The network has no ad loaded. |
| `Busy` | Another full screen ad is on screen. | Another full screen ad is on screen. |
| `Failed` | The network failed to show it. | The network failed to show it. |

The rules are checked in the order of the table — one full screen ad at a time first, then the ad block, the level, the
cooldown and the network — and the first one that stops the ad is the status.

`AdResult` also names the **placement** — what the call passed, or `interstitial` / `rewarded` when it passed none —
and the **network**, the provider's name (`stub` for `BaseAdsProvider`). Both are there for analytics: placements are
lower case letters, digits and `_`, and keep them stable once data is being collected.

### Reporting

`IAdsManagerService.FullscreenAdFinished` is raised once for every request for a full screen ad, with the same
`AdResult` its callback gets, just before the callback — also when the rules held the ad back. It is how the ads are
reported without the module knowing who listens: in this game `AdsEventsPublisher` (`Assets/_Scripts/Ads/`) turns it
into the global events `Ads.Shown`, `Ads.RewardEarned` and `Ads.ShowFailed`, which the analytics module tracks. A
listener that throws is logged; the other listeners and the callback still run.

---

## The rules

### Levels

Each type of ad starts at a level (config: banner 10, interstitial 6, rewarded 1). The level is **the furthest level the
player has reached** — one past the highest level ever completed — not the level being played: a player who has got to
level 20 and replays level 3 sees ads.

The module cannot see the game's progress (it lives in `Assembly-CSharp`, which no module can reference), so it asks
through `IAdsLevelSource`. `ProgressionAdsLevelSource` (in `Assets/_Scripts/Ads/`) answers from
`LevelsProgressionController.ReachedLevelIndex` and is bound in `BootInstaller`. Without a binding there are no level
limits, and the manager says so with a warning on start. A level source that throws holds every ad back rather than
show one too early, and is reported once.

### Consent

An ad SDK collects device identifiers from the moment it starts, so the network must not start before the player has
accepted the privacy notice and, where the law requires it, answered the consent form. The module asks through
`IAdsConsentSource`; `PrivacyAdsConsentSource` (in `Assets/_Scripts/Ads/`) answers from `IPrivacyService.CanRequestAds`
and is bound in `BootInstaller`.

While `CanRequestAds` is false the provider is not initialized, every full screen ad is `NotReady` without the provider
being asked, and a requested banner stays hidden. When the source raises `Changed` with ads allowed, the provider starts
and a requested banner comes up at once. Without a binding the provider starts on the context's `Initialize`, as before.

### Ad block

* **Permanent** (`AddPermanentAdBlock`) or **temporary** (`AddAdBlock(days)`, or `AddAdBlock(TimeSpan)` for hours or, in
  a test, minutes). Days add up: seven more on top of three left make ten; after one has run out, a new one counts from
  now.
* Stops **banners and interstitials**. Rewarded ads still show — the player chooses them for a reward.
* Takes the banner off screen at once. When a temporary block runs out, a requested banner comes back by itself.
* Kept in PlayerPrefs (`Ads.AdBlock.*`) and written through at once. That is apart from the game's save files on
  purpose: resetting the progress in the settings does not take away an ad block the player paid for. A reinstall does —
  restoring a purchase is the store's job, and the game calls `AddPermanentAdBlock` again when it does.
* Measured on the device clock, because it has to run out while the game is closed. A player who winds the clock back
  gets the time back as well; only a server clock would prevent that.
* `RemoveAdBlock` clears both kinds — for a refund, a cheat or a test. `AdBlockChanged` is raised on every add, remove
  and running out.

### Interstitial cooldown

The game may ask for an interstitial after every loss; the cooldown (config: 180 s) is what keeps it at one every three
minutes at most.

* Counted from the moment the previous interstitial **closed**, in real time: pauses and slow motion do not stretch it,
  scene loads do not reset it. A restart of the app does.
* Only an interstitial that was on screen starts it — one that failed or was not ready does not.
* A rewarded ad restarts it as well (config, on by default), so a player who has just watched an ad by choice is not
  shown another one right after.

### Banner

`ShowBanner` is a request, not a command. The banner is on screen whenever the request stands and the rules allow it:
it waits below its level and during an ad block, and appears on its own when the player gets there or the block ends.
The rules are looked at again once a second. `HideBanner` withdraws the request.

`BannerScreenArea` is the part of the screen the banner covers, in pixels from the bottom left corner like
`Screen.safeArea`, and an empty `Rect` while there is none. It stays empty until the network has an ad to put in the
banner — later than `IsBannerVisible` turns true — and nothing announces when it changes, so UI that keeps clear of it
looks every frame. In this game that is `SafeAreaFitter` (`Assets/_Scripts/UI/`), on the HUD and the pause button.

### Pausing

While an interstitial or a rewarded ad is up, `Time.timeScale` is 0 and the sound is muted (`AudioListener.volume`);
both are put back as they were before the callback runs, so a pause menu or a slow motion that was on stays on. iOS keeps
the game running under a network's full screen ad, so without this the level timer and the physics would go on behind
it. The sound is muted rather than paused because menu music and UI sounds ignore the listener pause. Turn it off in the
config if a network pauses the game itself.

---

## Setup

Already done in this project:

* `Assets/Resources/ProjectContext.prefab` has an `Ads` child with `AdsManagerInstaller` (config
  `Configs/AdsConfig.asset`, provider prefab `Prefabs/BaseAdsProvider.prefab`), last in the context's **Mono
  Installers** list.
* `BootInstaller` binds `IAdsLevelSource` to `ProgressionAdsLevelSource` and `IAdsConsentSource` to
  `PrivacyAdsConsentSource`.

It has to be the `ProjectContext`: every death reloads the gameplay scene, and the cooldown and the banner request must
survive that. A second `AdsManagerInstaller` in the same or a child context is skipped with a warning.

`Create > Game > Ads > Ads Config`:

| Setting | Default | Meaning |
| --- | --- | --- |
| `Banner From Level` | 10 | The level the player has to reach before banners. |
| `Interstitial From Level` | 6 | The same for interstitials. |
| `Interstitial Cooldown` | 180 | Seconds after an interstitial closes before the next one. |
| `Rewarded From Level` | 1 | The level for rewarded ads; 1 means from the start. |
| `Rewarded Restarts Interstitial Cooldown` | on | A rewarded ad restarts the interstitial cooldown. |
| `Pause Game` | on | Time and sound held while a full screen ad is up. |
| `Log Requests` | on | Every request and what came of it in the console — editor and development builds only. |

With logging on, the console tells why an ad was not shown:

```
[Ads] Interstitial (level_fail) not shown: cooldown, 95 s left.
[Ads] Banner asked for, held back: the player has reached level 4, Banner starts at level 10.
[Ads] Showing Rewarded (double_coins).
[Ads] Rewarded (double_coins) closed: Completed.
```

---

## BaseAdsProvider — the stub

A stand-in network for development: nothing is loaded from anywhere. The installer creates it from
`Prefabs/BaseAdsProvider.prefab` as a GameObject of its own, `ProjectContext/Ads/BaseAdsProvider`, so it lives across
scenes like a real network's overlay. Its canvas (Screen Space Overlay, sorting order 30000) is over every screen of the
game and blocks clicks while an ad is up.

* **Interstitial / rewarded:** a black screen saying `Interstitial Ad` or `Rewarded Ad` over a countdown, closing itself
  when the timer runs out. `Interstitial Duration` and `Rewarded Duration` are on the component — 2 s each; change them
  in the prefab, or on the live object in Play Mode. The countdown runs on unscaled time, so it works while the manager
  holds the game paused.
* **Banner:** a black strip along the bottom edge saying `Banner Ad`.
* **Simulation**, to take the game's callbacks through every branch: `Success` (shown for the full time), `Closed Early`
  (closes halfway — an interstitial still counts, a rewarded ad earns nothing), `Not Ready` (nothing to show), `Failed`
  (fails to show).

It is internal to the module, like every provider.

---

## Adding a real ad network

A network is one more implementation of `IAdsProvider`, inside the module:

| Member | Contract |
| --- | --- |
| `Network` | The network's name for analytics: `levelplay`, `admob`. Lower case letters, digits and `_`, at most 32 characters. |
| `Initialize()` | Called once, before anything else. Bring the SDK up, start loading ads. |
| `IsAdAvailable(type)` | Whether an ad of this type is loaded now. Asked for full screen ads only. |
| `ShowBanner()`, `HideBanner()` | Put the banner up, take it down. |
| `BannerScreenArea` | Where the banner is while it is on screen with an ad, in screen pixels; empty otherwise. Asked every frame: no SDK call. |
| `ShowInterstitial(onClosed)` | `Completed` once it was on screen and closed; `NotReady`; `Failed`. |
| `ShowRewarded(onClosed)` | `Completed` when the reward is earned; `Cancelled` when closed before; `NotReady`; `Failed`. |

The callbacks are to be invoked exactly once and **on the main thread** — SDKs that answer from their own thread have to
hop back first. The provider applies no rules: the ad block, the levels, the cooldown and "one at a time" are the
manager's. Reload the next ad after each show inside the provider.

Put the provider and a small installer in an assembly of their own under this folder (for example
`Providers/LevelPlay/`, referencing `Game.Ads`, `Zenject` and the SDK), add
`[assembly: InternalsVisibleTo("<that assembly>")]` to `Scripts/AssemblyInfo.cs`, and bind it in that installer:

```csharp
Container.Bind<IAdsProvider>().To<LevelPlayAdsProvider>().AsSingle();
```

Put that installer **before** `AdsManagerInstaller` in the `ProjectContext`'s **Mono Installers** list: a provider that is
already bound is used, and the stub is not created. Nothing in game code, and nothing in the manager, changes. Switch
`Pause Game` off if the SDK pauses the game on its own.

---

## Tests

`Window > General > Test Runner > EditMode`, assembly `Game.Ads.Tests`:

* `AdsManagerServiceTests` — full screen ads, callbacks and `FullscreenAdFinished`, placements, pausing, the cooldown,
  the levels, the banner;
* `AdBlockTests` — what an ad block stops, how days add up and run out, what survives a restart;
* `AdsInstallerTests` — the installer in a real container, the config asset and the stub prefab;
* `AdsModuleBoundaryTests` — the rule above: nothing but the manager's interface and its types is public.

The manager is built over test doubles for the network, the clocks, the storage and the pause, so no test touches
PlayerPrefs, `Time.timeScale` or the audio listener.

---

## Outside the module

* `Assets/_Scripts/Ads/ProgressionAdsLevelSource.cs` — the game's `IAdsLevelSource`.
* `Assets/_Scripts/Ads/PrivacyAdsConsentSource.cs` — the game's `IAdsConsentSource`, over the privacy module.
* `Assets/_Scripts/UI/SafeAreaFitter.cs` — keeps the gameplay HUD inside the safe area and clear of the banner.
* `Assets/_Scripts/Ads/AdsEventsPublisher.cs` — puts `FullscreenAdFinished` on the global event bus as `Ads.*`
  (`Assets/_Scripts/Events/Meta/AdEvents.cs`).
* `LevelsProgressionController.ReachedLevelIndex` — the furthest level reached, zero based.
* `BootInstaller` — binds the level source, the consent source and the events publisher.
* `ProjectContext.prefab` — the `Ads` child with the installer.
* `LooseGameState` — the interstitial after a loss, placement `level_fail`.

Where the banner and the rewarded ads go is for the game to decide, one call on `IAdsManagerService` each.
