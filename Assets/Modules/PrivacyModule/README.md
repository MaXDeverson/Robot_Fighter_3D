# Privacy Module

The game's privacy: the privacy policy page and the player's consent. Google Play requires a privacy policy in Play
Console **and** inside the game; the GDPR (EEA, UK, Switzerland) and the privacy laws of some US states require the
player's choice before the ads and the analytics collect anything, and a way to change it later.

Wired into the game: see [Integration](#integration) for what is where.

---

## How it works

```
 PrivacyConfig ──► PrivacyInstaller (ProjectContext)
 (policy URL,          │ binds
  email, timeout,      ▼
  test settings)   PrivacyService ──IPrivacyConsentProvider──► PrivacyConsentProvider (prefab, under the installer)
                   │ IPrivacyService                              ├─ BasePrivacyConsentProvider — stub, this module
                   │ timeout (stands still while the form         └─ UmpPrivacyConsentProvider — Google's UMP,
                   │ is up) · one request at a time ·                Assets/_Scripts/Privacy/UmpImplementation
                   │ callbacks once · ConsentChanged
                   ▼
   boot: GatherConsent ──► ads (CanRequestAds) · analytics (AnalyticsAllowed)
   settings: OpenPrivacyPolicy · ShowPrivacyOptions (only when IsPrivacyOptionsRequired)
```

* **The consent platform** keeps the player's answer, decides whether the region needs a form and shows it. In the
  game it is Google's UMP, which comes with the AdMob SDK: the GDPR form in the EEA, the UK and Switzerland, nothing
  anywhere else — there the game starts at once. The game shows no window of its own.
* **The policy page** is a web page, `PolicySite~/index.html` (the `~` keeps Unity from importing the folder). It is
  hosted on GitHub Pages; its address goes into `PrivacyConfig`, Play Console and the AdMob consent messages.
* **`PrivacyService`** sits between the game and the platform: it adds a timeout so boot never hangs (but not while
  the player is reading the form), shares one request among callers, answers every callback exactly once and raises
  `ConsentChanged` when anything changes — also when the platform answers after the timeout.

### What the player's answer does

| Region and answer | Ads | Analytics |
| --- | --- | --- |
| No form (most of the world) | personalized | on |
| GDPR form: "Consent" | personalized | on |
| GDPR form: "Do not consent" | limited ads — no advertising ID, nothing stored on the device | off |
| GDPR form: "Manage options", storage on the device allowed, personalization off | non-personalized | on only with purpose 8 (see below) |
| Form not answered yet / no connection on the first launch | not requested | events wait |

The ads need nothing from the game but `CanRequestAds`: UMP saves the choices on the device as an IAB TCF string and
the Google Mobile Ads SDK reads it before every request. **Never add `npa=1` to an ad request** — it would make the ads
non-personalized for the players who consented too.

The analytics are not in the form, so the UMP provider reads the TCF purposes: allowed where the GDPR does not apply,
and under it only with consent to purpose 1 (store and access information on a device) and purpose 8 (measure content
performance).

---

## Using it

Game code injects `IPrivacyService` and nothing else:

```csharp
[Inject] private IPrivacyService _privacy;

// Boot, before the ads and the analytics start. Answered when the platform is done, failed, or timed out.
_privacy.GatherConsent(() => ContinueBoot());

// Ads and analytics follow the answer, now and whenever it changes.
_privacy.ConsentChanged += () => Apply(_privacy.CanRequestAds, _privacy.AnalyticsAllowed);

// Settings: the policy is always there; the options button only where the law requires it.
_policyButton.onClick.AddListener(_privacy.OpenPrivacyPolicy);
_optionsButton.gameObject.SetActive(_privacy.IsPrivacyOptionsRequired);
_optionsButton.onClick.AddListener(() => _privacy.ShowPrivacyOptions(RefreshButtons));
```

| Member | What it is |
| --- | --- |
| `PrivacyPolicyUrl`, `HasPrivacyPolicy` | The page from the config; `HasPrivacyPolicy` is false unless it is https. |
| `ContactEmail` | Where players write about their data — for a "Contact" or "Delete my data" button. |
| `OpenPrivacyPolicy()` | Opens the page in the browser; logs an error and opens nothing without one. |
| `ConsentStatus` | `Unknown`, `NotRequired`, `Required`, `Obtained` — the same states as UMP. |
| `CanRequestAds` | The consent is in — whatever the player chose — or the region needs none. A refusal still allows ads: limited. |
| `AnalyticsAllowed` | The region needs no consent, or the player consented to what the analytics does. |
| `IsPrivacyOptionsRequired` | Show the "Privacy options" button. |
| `GatherConsent(onCompleted)` | Update the answer and show the form if needed. After a success later calls are answered at once; after a failure the next call tries again. |
| `ShowPrivacyOptions(onClosed)` | Show the form again; calls back at once when the region needs none or the form is already up. |
| `ResetConsent()` | For testing: forget the answer, so the next `GatherConsent` asks as on a first launch. Refused while a request or a form is out. |
| `ConsentChanged` | Any of status, ads, analytics, options has changed. |

The values are read from the answer saved on the device, so on every launch but the first they are right before
`GatherConsent` has finished.

---

## The config — `Configs/PrivacyConfig.asset`

| Field | What it does |
| --- | --- |
| **Privacy Policy Url** | The policy page. Public https, not a PDF: `https://neonwhitepink.github.io/Robot_Fighter-privacy_policy/`. |
| **Contact Email** | The address the policy page names. |
| **Gather Timeout** | Seconds boot waits for the platform (10 by default), not counting the time the form is on the screen. The platform keeps going after it. |
| **Tag For Under Age Of Consent** | Treats every player as a child: no form, limited ads. Off while the target audience is 13+. |
| **Debug Geography** | Pretend the device is in the EEA / a regulated US state / elsewhere. Editor and development builds only. |
| **Test Device Hashed Ids** | The devices the debug geography applies to; UMP logs a device's id to logcat on its first request. Editor and development builds only. |
| **Log Requests** | Log every request and its outcome. Editor and development builds only. |

What the forms say — the GDPR message, the US state regulations message, the ad partners — is set in the AdMob console,
not here.

---

## Providers

`PrivacyInstaller` creates the platform from a prefab, under its own GameObject:

* **Provider = Base** → `Prefabs/BasePrivacyConsentProvider`, the stub. No form, no network; its **Simulation** says
  what happens: *Not Required* (a region with no form), *Consent Given* / *Consent Refused* (a regulated region and the
  player's answer), *Offline* (every request fails). **Answer Delay** stands in for the network and the player reading.
  To try a player changing their mind, switch Simulation in Play Mode and press "Privacy options".
* **Provider = Network** → **Network Provider Prefab**, the real platform. Without a prefab the stub is used and an
  error is logged. In this game it is `Prefabs/UmpPrivacyConsentProvider`.

A real platform derives from `PrivacyConsentProvider` (public, so it can live outside the module), answers on the
main thread exactly once per call and reports `IsShowingForm` while its form is up.

### The UMP provider

`Assets/_Scripts/Privacy/UmpImplementation/UmpPrivacyConsentProvider.cs`, assembly `Game.Privacy.UmpImplementation`:

* `ConsentInformation.Update` with `TagForUnderAgeOfConsent` and, from `DebugGeography` and `TestDeviceHashedIds`, a
  `ConsentDebugSettings`; then, only when the status is `Required`, `ConsentForm.Load` + `Show` — apart, rather than
  `LoadAndShowConsentFormIfRequired`, so the provider knows when the form is on the screen.
* `ConsentStatus`, `CanRequestAds()`, `PrivacyOptionsRequirementStatus`, `ShowPrivacyOptionsForm`, `Reset` — straight
  from UMP, read again after every answer.
* `AnalyticsAllowed` from `IABTCF_gdprApplies` and `IABTCF_PurposeConsents`. On Android they live in the app's
  default SharedPreferences (not in PlayerPrefs, which use a file of their own) and are read through JNI; on iOS they
  are in the standard user defaults, where PlayerPrefs read them. A development build logs what it read:
  `[Privacy] UMP saved gdprApplies 1, purpose consents '1111...': analytics allowed.`
* In the editor UMP shows a placeholder picture of the form and saves no TCF string: there an answered form allows the
  analytics.

---

## Testing the form on a phone

1. Development build, run it once, and find the line UMP writes to logcat:
   `adb logcat | findstr /i "addTestDeviceHashedId"` (the id is in quotes).
2. Put the id into **Test Device Hashed Ids** and set **Debug Geography** to *EEA*. Build again.
3. The GDPR form comes up on launch. To see it again, clear the app's data (Android Settings → Apps → the game →
   Storage → Clear data) and launch it again.
4. Check the logcat line `[Privacy] UMP saved ...`: "Consent" should allow the analytics, "Do not consent" should not.
5. Set **Debug Geography** back to *Disabled* (a release build ignores it anyway).

---

## Publishing the policy page

1. Create a **public** GitHub repository for the page (the game's repository is private, and GitHub Pages on a free
   account needs a public one): `Robot_Fighter-privacy_policy` on the `neonwhitepink` account.
2. Put `PolicySite~/index.html` at its root. Its contact address must match **Contact Email** in the config.
3. Settings → Pages → Deploy from a branch → `main`, `/ (root)`. The page appears at
   `https://neonwhitepink.github.io/Robot_Fighter-privacy_policy/`.
4. Put that address into `PrivacyConfig`, Play Console (App content → Privacy policy) and the AdMob messages.

When the game or its SDKs change what is collected, update the page, its "Last updated" date and the Data safety form
in Play Console together — Google checks that they agree.

## AdMob console

Privacy & messaging:

* **European regulations (GDPR)** message: English, the policy address, the ad partners (Google's default list), with
  a "Do not consent" button — consent may not be the price of playing in the EEA.
* **US state regulations** message, if on: shows nothing at launch; the player opts out through "Privacy options".

---

## Integration

In the game:

* `Assets/Resources/ProjectContext.prefab` — the `PrivacyService` child (this module's prefab), second in the Mono
  Installers, right after `BootInstaller`. Its Provider is Network, with `Prefabs/UmpPrivacyConsentProvider`.
* `GameBoot.RunBoot` — waits for `GatherConsent`, and only then raises `GameLaunched` and loads the menu.
* `Assets/_Scripts/Ads/PrivacyAdsConsentSource.cs` — the ads module's `IAdsConsentSource`: the ad network starts only
  when `CanRequestAds` is true (see the ads module README, Consent).
* `Assets/_Scripts/Privacy/PrivacyAnalyticsConsent.cs` — hands `AnalyticsAllowed` to `IAnalyticsConsent`: granted when
  allowed, reset (events wait) while the platform or the player has not answered, denied (events dropped) on a refusal.
  `AnalyticsConfig` has **Require Consent** on for this. It initializes at order -200, before the analytics sinks
  attach (-100), so a stale saved answer never starts an SDK.
* Both bound in `BootInstaller`.
* `GameAnalyticsSink` starts the GameAnalytics SDK only on the first `SetCollectionEnabled(true)`:
  `GameAnalytics.Initialize()` reads the advertising ID and calls the GameAnalytics servers even with event submission
  off.
* `PrivacySettingsUI` (`Assets/_Scripts/Menu/`) on `Canvas/MainMenu/Settings` in `_Scenes/Menu.unity` — "Privacy
  policy" (always) and "Privacy options" (only where `IsPrivacyOptionsRequired`) under the sound and music sliders.
* `Main_Menu.Privacy` — the main menu's privacy button (switched off in the scene) opens the same page.

---

## Assemblies

| Assembly | What it holds | References |
| --- | --- | --- |
| `Game.Privacy` | `IPrivacyService`, `PrivacyConfig`, `PrivacyConsentProvider` and the stub, the service, the installer. | Zenject |
| `Game.Privacy.Tests` | EditMode suite: the service (timeout and its pause for the form, shared requests, late answers, reset, callbacks), the config, the stub, the installer. | the above, Zenject-TestFramework |
| `Game.Privacy.UmpImplementation` | The UMP provider. | the above, the Google Mobile Ads SDK |
