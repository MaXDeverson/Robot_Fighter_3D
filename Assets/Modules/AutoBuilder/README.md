# AutoBuilder Module

**One button for the Google Play bundle.** `Tools > AutoBuilder` (or the AutoBuilder button on the main toolbar) checks
the project, builds the Android App Bundle into its own named file, and — only when the build succeeded — raises the
game version and the version code for the next upload. A failed or cancelled build leaves the project as it was.
The module is editor only: nothing of it ends up in a build.

```
 checks ──ok──> BuildPipeline (AAB, signed, symbols inside) ──succeeded──> 0.0.1 (1) → 0.0.2 (2) ──> history
   │                                                │
   └─ an error: nothing is built                    └─ failed / cancelled: version untouched, failure in history
```

---

## The window

| Section | What it does |
| --- | --- |
| Header | Product, package name, git branch @ commit and how many files are uncommitted. ↻ re-reads git. |
| **Version** | The version and the code, editable in place (Enter applies). *Raise now* bumps by hand: + Patch / + Minor / + Major also raise the code, + Code raises only the code. *After a build* shows what the project will become. |
| **Build** | *Release* (for Google Play) or *Development* (Development Build + script debugging, for our phones only). Which part of the version is raised after a build (Patch by default) or not raising at all. *Build & Run* installs and starts the bundle on the USB phone. *Native symbols in the bundle* lets Play Console show native crash stacks. Output folder (default `build/aab`, already in .gitignore). |
| **Signing** | The keystore and alias from Player Settings, and both passwords. |
| **Release notes** | "What's new" text for Play Console. *Copy* puts it on the clipboard; it is saved with the build. |
| **Checks** | Everything that is wrong, with a button to fix it where one exists (switch to Android, open Player Settings, open the scene list). Red stops the build, yellow does not. |
| **Build AAB** | Asks to save modified scenes, builds, beeps when done. |
| **Last build / History** | The last 30 builds on this machine: version, code, size, time, branch @ commit (`*` = built with uncommitted changes), the notes (*Notes* copies them) and the file (📁 shows it in Explorer). |

Files are named `PortalQuest_v0.0.1_c1_2026-10-03_1530.aab` (`_dev` after the code for development builds), so builds
never overwrite each other and the name says which upload it is.

### What is checked

Errors (no build): Play Mode or compiling; the active platform is not Android; the version is not `major.minor.patch`;
no scene enabled; the keystore file, the alias or a password is missing. For **Release** also: the backend is not
IL2CPP, ARM64 is off, or the bundle would be signed with Unity's debug key — Google Play takes none of those.
For Development these three are only warnings.

Warnings: unsaved scenes, uncommitted git changes, a version code not higher than the last successful build's (Google
Play rejects such an upload), a development build.

---

## Versions

- The version is read as major.minor.patch; `1.4` is read as 1.4.0, so after a bump it is written as `1.4.1`.
- Patch → 0.0.**2**, Minor → 0.**1**.0, Major → **1**.0.0. The version code always goes +1.
- The new values are written both to Player Settings in memory and as exactly two lines of
  `ProjectSettings/ProjectSettings.asset` (`bundleVersion`, `AndroidBundleVersionCode`). Unity has no call that saves
  only the player settings, and `AssetDatabase.SaveAssets` would also save every other unsaved asset in the editor.
- **Commit the bump yourself.** The module never runs git commands that change anything.
- *Build & Run*: if the install on the phone fails, Unity reports the whole build as failed, so the version is not
  raised even though the .aab is on disk. Install it again or build once more.

---

## Settings and passwords

Everything the window remembers lives in `UserSettings/` — Unity's folder for per-user state, kept out of git by the
project's .gitignore:

- `UserSettings/AutoBuilderUserSettings.asset` — the window's choices, the release notes draft and both keystore
  passwords **in plain text** (Unity itself forgets them on every restart). Whoever can read this file and the keystore
  can sign as the game.
- `UserSettings/AutoBuilderHistory.asset` — the build history.

The build sets `buildAppBundle` and, with symbols on, the debug symbol level/format only for its own duration and puts
the previous values back afterwards, so plain `File > Build` keeps behaving as before.

---

## Command line

The same build without the window, e.g. for CI:

```
Unity.exe -batchmode -projectPath <project> -buildTarget Android -logFile -
  -executeMethod Game.AutoBuilder.AutoBuilderCommandLine.Build
  [-abKind Release|Development] [-abBump Patch|Minor|Major|None] [-abOutput <folder>]
```

Anything not given comes from this machine's window settings. Passwords come from `AUTOBUILDER_KEYSTORE_PASS` and
`AUTOBUILDER_KEYALIAS_PASS` when those environment variables are set. The editor exits with 0 when the bundle is
built, 1 otherwise. Build & Run is always off here.

---

## Contents

| File | What it is |
| --- | --- |
| `Editor/Window/AutoBuilderWindow.cs` | The window, its menu item and the main toolbar button. |
| `Editor/Build/AabBuilder.cs` | The build: checks, BuildPipeline, the version raise, the history record. |
| `Editor/Build/BuildPreflight.cs` | The checks. |
| `Editor/Build/AabBuildRequest.cs` | What to build (`AabBuildRequest`) and what came out (`AabBuildResult`). |
| `Editor/Build/AutoBuilderCommandLine.cs` | `-executeMethod` entry point. |
| `Editor/Versioning/AppVersion.cs` | major.minor.patch: parse and bump. |
| `Editor/Versioning/ProjectSettingsVersionFile.cs` | Writes the version and the code to Player Settings and the settings file. |
| `Editor/Settings/*` | The window's settings and the history in `UserSettings/`. |
| `Editor/Git/GitInfo.cs` | Branch, commit and uncommitted files — read only. |
| `Editor/AutoBuilderPaths.cs` | Project root, output folder, the .aab file name. |
| `Tests/*` | EditMode tests for versions, file names and the settings file rewrite; none of them builds. |

Assembly `Game.AutoBuilder.Editor` (namespace `Game.AutoBuilder`), editor only, auto-referenced, no dependencies.
