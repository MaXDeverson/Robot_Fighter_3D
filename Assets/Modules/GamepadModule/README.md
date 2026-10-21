# Gamepad Module

Playing with a pad: the prompts that show which pad button does what, walking the menus with the stick and the
buttons, and the buttons that answer a pad button of their own (back, pause). The gameplay controls themselves — move,
punch, kick, jump, defend — are in the game's `GameInput` actions (`Resources/GameControls.inputactions`) and the
template's `InputManager`; this module only knows how to show them and how to drive the menus.

Everything here appears only while a pad is plugged in. A phone with no pad looks and behaves exactly as before.

Ported from Portal Quest. The only change to the module's code: a shortcut switches an action off only if it switched
it on itself (`GamepadNavigation.Release`) — `GameInput` keeps its actions on in every scene and reads them itself — and
`GamepadNavigationScope.FirstSelected` can be set from code.

---

## How it works

```
 InputSystem.onDeviceChange ──► GamepadService (ProjectContext) ──► GamepadPromptBadge      (HUD buttons: the glyph
 Gamepad.current (each frame)    IsConnected / Current / Family      GamepadUIOverlay         of the action's binding)
                                 GetGlyph(action)                    (selection frame,
                                        ▲                             "Select" / "Back")
                                        │                                    ▲
                    GamepadConfig ──────┘                                    │
                    (glyph sets per family,          GamepadNavigation (ProjectContext, ITickable)
                     UI Submit / Cancel)             ├─ GamepadNavigationScope   — a menu; the one switched on last has the focus
                                                     └─ GamepadButtonShortcut    — a Button pressed by a pad button
```

* **Glyphs follow the bindings.** A badge names an action of `GameInput.inputactions`; the service finds which control
  of the pad in hand the action is bound to (`buttonSouth`, `leftStick/left`, `start`) and the family's
  `GamepadGlyphSet` turns that position into what is printed on that pad. Rebinding an action in the asset changes the
  prompt with it. Confirm is wherever the pad puts it: A on Xbox, cross on PlayStation, the right-hand A on a Switch Pro.
* **Families.** `DualShockGamepad` layouts → PlayStation, `XInputController` → Xbox, `SwitchProControllerHID` → Switch.
  Android gives most pads one common layout, so there the name the pad reports decides ("Sony", "Wireless Controller",
  "Nintendo", "Pro Controller"…); anything unknown is drawn as Xbox.
* **Focus.** The `GamepadNavigationScope` switched on last has the focus. While a pad is connected the focused menu
  always has a button selected (its `First Selected`, or the first button that can be pressed, or the one selected
  there last), and the selection is kept inside it — Unity's automatic navigation would otherwise walk off a popup onto
  the window behind it. With no menu open the selection is cleared, so the confirm button (which is also jump) cannot
  press a HUD button tapped earlier. Scroll views are scrolled to the selected button. An open dropdown list is left
  to the dropdown.
* **Gameplay input is off while a menu has the focus** (`InputManager` asks `GamepadNavigation.Focused`): the A that
  confirms Next on the level complete screen must not land as a jump, and the stick that walks a menu must not walk the
  fighter. A button still held when the menu closes is not taken for a new press.
* **Shortcuts.** A `GamepadButtonShortcut` presses its `Button` (as a confirm would: it flashes and raises `onClick`)
  when one of its actions goes down — but only while its menu has the focus, and, for a button outside every menu like
  the HUD pause button, only while no menu is open. At most one button a frame. A button locked for a finger — not
  interactable, or under a `CanvasGroup` that blocks raycasts — is locked for the pad too (`UIReach`).

### Gameplay side (Assembly-CSharp)

* `InputManager` (BeatEmUp template, created from `Resources/InputManager.prefab` by `LevelInit`) turns `GameInput` into
  the template's input events. **The touch controls stay on with a pad** (`Hide Touch Controls With Gamepad` off): each
  shows the pad button that does the same, and `MyButtonEvent` plays a button's press animation on the pad press too.
* `StickPull` (aim with a stick, fire on release) came with the module from Portal Quest; this game does not use it.

---

## Assemblies

| Assembly | What it holds | References |
| --- | --- | --- |
| `Game.GamepadSupport` | `IGamepadService`, `GamepadConfig`, `GamepadGlyphSet`, `StickPull`, `UIReach`, `GamepadNavigation`, the components, the installer. | Zenject, Input System, UGUI, TMP |
| `Game.GamepadSupport.Tests` | EditMode suite (`InputTestFixture`, virtual pads). | the above, Input System test framework |

`com.unity.inputsystem` is listed in `testables` in `Packages/manifest.json` — that is what lets the tests use
`InputTestFixture`.

---

## Setup in this game

Already done:

* `Assets/Resources/ProjectContext.prefab` has a `Gamepad` child (`Prefabs/Gamepad.prefab`) with `GamepadInstaller`
  (last in **Mono Installers**) and the `GamepadOverlay` canvas (prompts at the top centre — the menus here keep their
  buttons along the bottom edge; labels in the game's Bebas font). Config: `Settings/GamepadConfig.asset`, glyph sets
  `Settings/GamepadGlyphs_*.asset`.
* Each scene keeps its own `EventSystem` on `InputSystemUIInputModule` with its default UI actions, **Send Navigation
  Events** on. The config's **Submit** / **Cancel** are `Menu/Submit` and `Menu/Back` of `GameControls.inputactions`:
  south (A / ✕) and east (B / ○) on a pad, as the default UI actions have them.
* **HUD** (`GamePlay` scene, `GamePlay_UI/Canvas`): a `GamepadPromptBadge` on the joystick (`Player/Move`), Punch,
  Kick, Jump, Defend, Run (off in the scene) and the pause button (`Menu/Pause`); a `GamepadButtonShortcut` (`Menu/Pause`)
  on the pause button. HUD, touch controls and the pause button have **Navigation** None.
* **Menus, `GamePlay`**: a `GamepadNavigationScope` on Level Objective (OK), Weapon Choice (first weapon), Pause
  (Resume), Level Complete (Next), Level Fail (Restart), Dialog Window (OK) and Loading (nothing to press). The back
  buttons: Pause → Resume (`Menu/Back` and `Menu/Pause`), Dialog Window → OK.
* **Menus, `Menu`**: a scope on Main Menu (Play), Settings, Device ID, Garage (`SelectionPanel`, Play), its not-enough-cash
  dialog, Level Selection (`MissionsPanel` — `MissionSelection` starts it on the last unlocked level), In-App Offer and
  Loading. The back buttons (`Menu/Back`): Settings → Back, Device ID → Close, Garage → Back, cash dialog → OK, Level
  Selection → Back, In-App Offer → Back. These replace the `GameInput.Back` checks the menu scripts had in `Update`,
  so Esc and the Android back button go through the same buttons.
* `Manager` no longer reads Pause / Back itself: the two shortcuts above do, keyboard included (Esc, P).
* The `Splash` scene is not loaded by the game (Boot goes to `Menu`) and has no scopes.

### Pad layout

| Action | Pad | Keyboard (editor) |
| --- | --- | --- |
| Move | left stick, d-pad | WASD, arrows |
| Punch | west (X / □) | J, Z |
| Kick | north (Y / △) | U, X |
| Jump | south (A / ✕) | Space |
| Defend | east (B / ○), LB / L1, LT / L2 | K, C |
| Run (button off in the scene) | RB / R1, RT / R2 | Left Shift |
| Pause | Start / Options | Esc, P |
| Menus | left stick / d-pad, confirm (A / ✕), back (B / ○) | arrows, Enter, Esc / Backspace |

## Adding things

* **A menu**: `GamepadNavigationScope` on the object that is on exactly while the menu is on screen (a panel's root).
  Set **First Selected** if the first button in the hierarchy is not the one to start on.
* **Its back button**: `GamepadButtonShortcut` on that `Button`, action `Menu/Back` (from `GameControls`). The "Back"
  prompt appears by itself.
* **A prompt on a button**: drop `Prefabs/GamepadPromptBadge.prefab` under the button's graphic and set its **Action**.
  All its images have **Raycast Target** off — keep it so, or the badge eats the taps meant for the button.
* **A prompt in place of a touch hint**: `GamepadPromptBadge` on an object that stays on, **Content** = the pad
  version, **Replaces** = the touch version. One of the two is on at a time.

## Art

Glyphs: Kenney "Input Prompts" 1.5 (`Art/Glyphs`, the 128 px "Double" set), **CC0** — free for commercial use, no
credit required (`Art/Glyphs/License_Kenney_InputPrompts.txt`). The badge circle, the selection frame and the prompt
panel (`Art/UI`) were drawn for this module.

## Not covered

* Interstitial ads (shown on pause, resume, restart…) are the ad network's own screens, outside the game's UI: this
  module does not drive them, and closing one with a pad has not been tried on a device.
* Switch Pro controllers on Android come in as a common pad, so their family is told by name only.

## Tests

`Game.GamepadSupport.Tests` (EditMode): `StickPull` (release vs spring-back, slow ease-off, cancel), family
resolution, binding → control on a pad (including confirm on Switch), glyph fallback, a badge swapping a touch hint, the service following pads
plugged in and out, and the navigation: focus order, back only for the focused menu, the HUD pause button only with no
menu open, one press per frame, locked buttons.
