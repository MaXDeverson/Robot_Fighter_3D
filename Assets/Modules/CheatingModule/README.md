# Cheating Module

The admin mode and the cheats behind it. Cheat buttons, debug toggles and dev-only windows are seen only while the
admin mode is on, and the admin mode can be switched on only on the team's devices: the editor, desktop builds and the
phones listed in the config. Players never see the switch.

---

## How it works

```
 CheatingConfig ──► AdminAccessPolicy ──► CheatingService (ProjectContext) ◄── IAdminStorage (Cheating/admin_enabled)
 (allowed ids)        Editor / Desktop /      │  IsAdminAvailable: may this device switch it on
                      AllowedDevice /         │  IsAdminEnabled:   is it on (available && switched on)
                      DevelopmentBuild        │  Register / Execute cheats, hotkeys
                                              ▼
       AdminToggle ─────────── CheatingContentActivator ─────── CheatButton ──► CheatDefinition ◄── CheatHandler
   (settings; hidden on       (shows / hides its objects by     (runs a cheat)     (asset)          (in the scene,
    a player's phone)          the admin mode)                                                       does the cheat)
```

Two levels:

* **Available** — worked out once at start from the device: the editor and Windows/macOS/Linux builds always; a phone
  or tablet when its id is in `CheatingConfig`; any development build when the config allows it.
* **Enabled** — available *and* the admin switch is on. The switch is kept in its own file,
  `<persistentDataPath>/Cheating/admin_enabled`: "Reset game" wipes the files at the top of that folder and all
  PlayerPrefs, and the admin mode survives it. A saved switch means nothing where the mode is not available, so editing
  the files opens nothing.

### Why not the IMEI

No store app may read it: Android 10+ gives it only to system apps, iOS has no API for it at all. The id is
`SystemInfo.deviceUniqueIdentifier` instead:

* **Android** — MD5 of `ANDROID_ID`, which depends on the key the build is signed with. The same phone has one id for
  a debug build, another for a build signed locally with the upload key, and a third for the build from Google Play.
  Add the id of the build the phone really runs. A factory reset changes it too.
* **iOS** — `identifierForVendor`; it changes when every game of this vendor is deleted from the phone.

---

## Assemblies

| Assembly | What it holds | References |
| --- | --- | --- |
| `Game.Cheating` | `ICheatingService`, `CheatDefinition`, `CheatingConfig`, `CheatHandler`, the policy, the service, the hotkeys, the installer. | Zenject, Input System |
| `Game.Cheating.UI` | `CheatingContentActivator`, `AdminToggle`, `CheatButton`, `DeviceIdView`, `MultiTapReveal`. | the above, UGUI, TMP |
| `Game.Cheating.Tests` | EditMode suite. | all of the above |

The game side lives in `Assets/_Scripts/Cheats/` (Assembly-CSharp, which no module can see): the handlers that do the
cheats with the game's own classes, and UI the module's components do not cover.

---

## Setup in this game

Already done:

* `Assets/Resources/ProjectContext.prefab` has a `Cheating` child with `CheatingInstaller`, last in the
  **Mono Installers** list. Its config is `Assets/Resources/Configs/CheatingConfig.asset`.
* **Boot**: `GameBoot` (Boot scene) takes `ICheatingService` and writes the admin mode to the log before the menu loads.
* **Settings** (`Menu` scene, `Canvas/MainMenu/Settings`): the `ADMIN` checkbox in the bottom left (`IsAdminToggle`,
  `AdminToggle`), and a `CheatingContentActivator` on `Settings` that shows the `Admin` object (*Add 10K*,
  *Reset Game*, *Ad Block*) only in the admin mode.
* **Device ID panel**: 5 quick taps on the game version in the bottom right of the settings (`GameVersion`:
  `GameVersionText` + `MultiTapReveal` → `DeviceIdPanel`). Works on every device, the admin mode is not needed. The menu
  is built on legacy `Text`, so it uses the game's `DeviceIdPanel` (`Assets/_Scripts/Cheats/`) in place of the module's
  TextMeshPro `DeviceIdView`.
* No cheats yet: the admin buttons call `Main_Menu` directly.

## Adding a phone

1. Install the build the phone will use (see "Why not the IMEI" — the id depends on the signing key).
2. Settings → tap the game version 5 times → **Copy**, and send the id to yourself.
3. Add an entry to `CheatingConfig` → **Allowed Devices**: a label ("Max — Pixel 7") and the id. Spaces and letter case
   do not matter.
4. Make a new build. The `ADMIN` row appears in the settings on that phone.

The id also goes to the log at start (`[Cheating] Device id: ...`) — the IngameDebugConsole or `adb logcat -s Unity`.

## Adding a cheat

1. `Create > Game > Cheating > Cheat`, in `Assets/Resources/Cheating/Cheats/`. Fill in the title and, if wanted, a
   hotkey.
2. The handler, on the game side — one class with the code of the cheat:

   ```csharp
   public sealed class AddCoinsCheatHandler : CheatHandler
   {
       [Inject] private GameplayResourcesService _resources;

       protected override void Execute() => _resources.Add(ResourceType.Coins, 1000);
   }
   ```

   Put it in the scene (or the scene context prefab) where the cheat makes sense and pick the cheat asset. It registers
   itself while it lives, so the cheat exists exactly where its handler is. A plain class can do the same with
   `ICheatingService.Register(cheat, action)` and dispose the result when it goes.
3. The button: any `Button` + `CheatButton` (pick the cheat, optionally its label) + a `CheatingContentActivator`
   that lists the button itself or its panel.

## Hiding anything else behind the admin mode

Put a `CheatingContentActivator` on the element or a parent and list the objects. It has two lists:

* **Shown In Admin** — active only while the admin mode is on (cheat buttons, debug panels). Keep them inactive in the
  prefab, so nothing shows if the activator is ever not injected.
* **Hidden In Admin** — active only while the mode is off (what a player sees in place of the admin content, or what
  gets in the way while testing). Keep them active in the prefab.

Either list may hold the activator's own object: it follows the admin mode from injection on, also while switched off.
The same object in both lists is warned about in the editor.

Code that needs the mode asks `ICheatingService.IsAdminEnabled` and listens to `AdminEnabledChanged`, as
`ConsoleManager` does.

---

## Rules

* **Refused when off.** `Execute` does nothing while the admin mode is off, whatever calls it — the button is not the
  only way in.
* **Hotkeys** run only in the admin mode, at most one cheat a frame, and only cheats with a handler in the loaded
  scenes. The keys are read with no regard for focus: a letter also fires while typing into an input field of a scene
  that handles that cheat.
* **Several handlers** for one cheat: the last registered runs; when it goes, the previous one is back.
* **Failures.** A handler that throws is logged and the cheat counts as not run. A listener of `AdminEnabledChanged`
  that throws is logged; the others still hear the change.
* **Injection.** Every component here must be injected — a scene object, or instantiated through the container. One
  that is not logs an error on `Start`.

## Tests

`Window > General > Test Runner > EditMode`, assembly `Game.Cheating.Tests`: the access policy, the service, the
hotkeys with a fake keyboard, the installer in a real container, the file storage, and the UI components injected
through a container. The settings window and the Win level button are covered by running the game.
