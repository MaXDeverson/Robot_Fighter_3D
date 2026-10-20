# Global Game Events Module

A hierarchical event service for Unity, wired entirely through Zenject. Gameplay publishes events; analytics, the
assistant commentator, UI and achievements subscribe to them without knowing each other exists.

The module is self-contained: it has no reference to any game code, and copying
`Assets/Modules/GlobalGameEventsModule/` into another project is the whole installation.

---

## Contents

| Assembly | What it holds | References |
| --- | --- | --- |
| `GlobalGameEvents.Core` | The service, paths, registry, queue, history. Pure C#, `noEngineReferences`. | — |
| `GlobalGameEvents.Zenject` | `GlobalGameEventsInstaller`, config asset, Unity logger/clock, lifetime service, late-tick pump. | Core, Zenject |
| `GlobalGameEvents.Editor` | `Window > Global Game Events Debugger`, installer inspector. | Core, Zenject integration |
| `GlobalGameEvents.Tests` | EditMode suite: core tests plus container integration tests. | all of the above |

Namespaces are `Game.Events` (core), `Game.Events.DI` (integration) and `Game.Events.EditorTools`. The
integration namespace is not called `Game.Events.Zenject` on purpose: that name would shadow the `Zenject`
namespace inside its own files and every `using Zenject;` would need qualifying.

The module shipped with a `Samples/` folder — three demo events, two subscribers and a demo scene. It was
deleted when the service was wired into this game, because its demo events claimed the `Level.*` paths the game
itself needs. Recover it from git history if the demo is ever wanted back.

---

## Setup

### 1. Add the installer

Pick **one** of these. Which one you want is a real decision, not a detail — see
[One service or one per scene](#one-service-or-one-per-scene).

**Project-wide service (survives scene loads):**

1. Open `Assets/Resources/ProjectContext.prefab`.
2. Add a child GameObject, e.g. `GlobalGameEvents`.
3. Add the `GlobalGameEventsInstaller` component to it.
4. On the `ProjectContext` component, add that component to the **Mono Installers** list.

**Scene-scoped service:**

1. Add a child GameObject under the scene's `SceneContext`.
2. Add `GlobalGameEventsInstaller` to it.
3. Add it to the `SceneContext`'s **Mono Installers** list.

### 2. Optional shared config

`Create > Game > Events > Game Events Config` makes a `ScriptableObject` with the same fields as the installer.
Assign it to the installer's **Config** field and several contexts can share one set of limits. With the field
empty the installer's own inline fields are used.

| Setting | Meaning |
| --- | --- |
| `Max Dispatch Depth` | How deep publishes may nest before the chain is treated as runaway recursion. |
| `Max Events Per Flush` | Upper bound on one late-tick flush, a guard against a runaway producer. |
| `History Capacity` | How many events the debugger window can show. |
| `Record History` | Keep that ring buffer. Costs one boxing allocation per published event. |
| `Log Event Flow` | Write every dispatch to the console. Editor and development builds only. |
| `Verify Main Thread` | Throw when the service is touched off its own thread. Editor and development builds only. |
| `Nested Events Policy` | What to do when a parent context already has a service. |
| `Validate Event Paths` | Resolve every event path at startup so problems surface once, not per publish. |

---

## Declaring an event

An event is a `struct` with an `[EventPath]` attribute. Nothing else.

```csharp
using Game.Events;

[EventPath("Level.Entered")]
public struct LevelEntered : IGameEvent
{
    public string LevelId;
    public int Attempt;
}
```

Rules:

* Paths are dot separated. Each segment starts with a letter or `_` and contains letters, digits and `_`.
* Paths must be unique across the game. Two types on one path is an error, reported at startup.
* The path is read by reflection **once per type** and cached inside the registry instance, never on publish.
* There is no string-based publish API. `Publish` always takes a typed value.

---

## Publishing

```csharp
public sealed class LevelFlow
{
    private readonly IEventPublisher _events;

    public LevelFlow(IEventPublisher events)
    {
        _events = events;
    }

    public void Enter(string levelId)
    {
        _events.Publish(new LevelEntered { LevelId = levelId, Attempt = 1 });
    }
}
```

`Publish` delivers before it returns. `Enqueue` defers to the end of the frame:

```csharp
_queue.Enqueue(new PlayerDied { Cause = "spikes" });
```

The queue is flushed once per frame by an `ILateTickable`, after every `Update` and every Zenject `ITickable`.
Events queued *from inside a handler* roll over to the next frame rather than extending the current flush, so a
handler that re-queues its own event cannot stall the frame.

---

## Subscribing

### A concrete event, typed

```csharp
IDisposable token = _events.Subscribe<LevelEntered>(e => Debug.Log(e.LevelId));
```

This is the allocation-free path. No boxing, no per-publish garbage.

### A whole branch

```csharp
_events.SubscribeBranch("Level", OnAnyLevelEvent);   // Level.Entered, Level.Completed, Level.Entered.Boss
_events.SubscribeBranch("*", OnEverything);          // every event; what analytics wants
```

Branch matching compares **whole segments**, never string prefixes, so `"Level"` never receives
`"LevelUp.Something"`. `null`, `""` and `"*"` all mean the root, and `"Level.*"` is a synonym of `"Level"`.

A branch handler takes `Action<IGameEvent>`, so the event is boxed — but only once per dispatch, and only if a
branch handler actually matches.

### Priority

Both overloads take an optional `priority`. **Lower runs first**, matching Unity's script execution order
convention; equal priorities keep subscription order. Typed and branch handlers share one ordering, so a branch
handler at `-10` runs before a typed handler at `0` for the same event.

---

## Injecting the service

Systems never reach for a global. There isn't one: the service is an ordinary object in a container.

Pick the narrowest interface that fits — the signature then documents what the system does:

| Interface | For |
| --- | --- |
| `IEventPublisher` | Systems that only send. |
| `IEventSubscriber` | Systems that only listen: analytics, commentary, achievements. |
| `IEventQueue` | Systems that defer. |
| `IGlobalGameEvents` | All of the above. |
| `IGameEventsDiagnostics` | Tooling. |

**Plain C# class — constructor injection:**

```csharp
public sealed class AnalyticsService : IInitializable
{
    private readonly IEventSubscriber _events;
    private readonly IEventSubscriptionScope _scope;

    public AnalyticsService(IEventSubscriber events, IEventSubscriptionScope scope)
    {
        _events = events;
        _scope = scope;
    }

    public void Initialize()
    {
        _scope.Add(_events.SubscribeBranch("*", OnAnyEvent));
    }

    private void OnAnyEvent(IGameEvent gameEvent) { /* ... */ }
}
```

Bind it with `Container.BindInterfacesTo<AnalyticsService>().AsSingle();`.

**MonoBehaviour — `[Inject]` method:**

```csharp
public sealed class LevelHud : MonoBehaviour
{
    [Inject]
    private void Construct(IEventSubscriber events, ISubscriptionLifetime lifetime)
    {
        lifetime.Bind(events.Subscribe<LevelEntered>(OnLevelEntered), this);
    }

    private void OnLevelEntered(LevelEntered e) { /* ... */ }
}
```

`FindObjectOfType` is never used in runtime code, and the module exposes no static entry point of any kind.

---

## Unsubscribing

Every subscribe call returns an `IDisposable`. Disposing it twice, or after the service is gone, is safe.

| Owner | Use | Released when |
| --- | --- | --- |
| MonoBehaviour | `ISubscriptionLifetime.Bind(token, this)` | the GameObject is destroyed |
| Plain C# system | `IEventSubscriptionScope.Add(token)` | the context is destroyed |
| Anything narrower | your own `SubscriptionBag`, disposed by you | you dispose it |

`ISubscriptionLifetime.Bind` exists because the natural spelling — an extension method
`token.AddTo(gameObject)` — would need a static class, which this module does not use. It attaches a hidden
`SubscriptionOwner` component to the object and disposes the tokens in `OnDestroy`. Binding to a `null` or
already destroyed owner disposes the token immediately and logs a warning, rather than leaking it.

Handlers may subscribe and unsubscribe from inside a handler. A handler removed mid-dispatch is not called
again; one added mid-dispatch starts with the next dispatch, not the one in flight. No
`InvalidOperationException`, no skipped or doubled handlers.

---

## One service or one per scene

Both are supported, and mixing them by accident is the failure mode worth understanding.

* **Installer in `ProjectContext`** — one service for the whole game, alive across scene loads. Right when
  analytics, save data or the assistant must not lose their subscriptions between levels.
* **Installer in a `SceneContext`** — the service dies with the scene. Right when a scene is a closed world and you
  want its listeners gone when it unloads.

**Two services by accident.** A `SceneContext` container's parent is the `ProjectContext` container. If both
install a service, a scene system resolves the scene's service and a project system resolves the project's, and each
half stops hearing the other. The symptom is a published event with zero handlers and no error at all.

The installer guards against this with **Nested Events Policy**:

| Policy | Behaviour |
| --- | --- |
| `ReuseParent` *(default)* | A parent already has a service → skip installing, log a warning, use the parent's. |
| `InstallOwn` | Install a second, scene-local service deliberately. Events do not cross. |
| `Fail` | Treat it as a setup error and stop the context from building. |

Two `GlobalGameEventsInstaller` components on the *same* context are always skipped with a warning; that case is never
deliberate.

The debugger window labels each context **Owns the service** or **Inherits the service from a parent context**, which is
the fastest way to confirm what you actually got.

---

## Debugging

**`Window > Global Game Events Debugger`** (Play Mode). It finds the running `ProjectContext` and every `SceneContext`,
resolves `IEventHistory` and `IGameEventsDiagnostics` from that context's container, and shows:

* the last N events with timestamp, path and field values;
* a branch filter using the same segment-wise matching as a real subscription;
* every live subscription, grouped by path with counts, with the owner and priority of each.

With no context running it says so instead of failing. There is no static path to the service, so with the game
stopped there is genuinely nothing to show.

The installer's inspector has the same **Open Global Game Events Debugger** button plus **Validate Event Paths**, which
scans the loaded assemblies for `IGameEvent` structs and reports every missing, malformed or duplicated path at
once.

---

## Threading

The service is a **main-thread component**. It records the thread that constructed it and, in the editor and in
development builds, throws `GameEventsThreadException` if touched from another one. In release builds that check
is compiled out, so publishing from a worker thread there is undefined behaviour, not a caught error. Marshal
back yourself — `await UniTask.SwitchToMainThread()` — before publishing.

---

## Errors

| Situation | Behaviour |
| --- | --- |
| A handler throws | Logged with the path and the handler's name; the remaining handlers still run. |
| An observer throws | Logged; handlers still run. |
| Publishing nests past `MaxDispatchDepth` | `GameEventsRecursionException` with the full path chain, rethrown through every nested dispatch to the original caller. Never swallowed by the per-handler catch. |
| The same recursion inside a flush | Logged; the rest of the queue still drains. |
| An event type without `[EventPath]` | `EventPathFormatException` at subscribe, publish or enqueue — at the call site, not a frame later. |
| Two types on one path | `EventPathConflictException`, reported at startup by the validation pass. |
| Using a disposed service | `ObjectDisposedException` naming the likely cause. |

---

## When not to use the service

An event service decouples by hiding the call graph, and that is a cost as well as a benefit.

* **Per-frame, per-entity chatter.** A bullet telling its own trail it moved is a method call, not an event.
* **You need a return value.** The service is fire-and-forget. If the publisher needs an answer, it wants an
  interface, not an event.
* **Ordering between two specific systems matters.** Priorities make the order deterministic, but if system B
  must always run after system A, say that in code: have one call the other, or sequence both from an owner.
* **There is exactly one listener, and it is obvious which.** Injecting the dependency is clearer and it is
  checked by the compiler.
* **The "event" is really state.** Something a late joiner needs to read is a property on a service. An event is
  a notification about a moment in time; whoever was not listening has missed it.

Good fits: notifications that cross system boundaries, that several unrelated systems want, or whose listeners
you do not want the publisher to know about — which is exactly the analytics and assistant case.

---

## Common mistakes

* **Leaked subscriptions.** A handler that captures a MonoBehaviour keeps that object's closure alive for as
  long as the service lives. Always hand the token to `ISubscriptionLifetime` or an `IEventSubscriptionScope`; never
  drop it on the floor. The debugger's subscription count climbing over a session is the tell.
* **Two services.** See above.
* **Subscribing in `Awake`, publishing in `Awake`.** Zenject injects before `Start`; a publisher that fires in
  its own `Awake` may beat a subscriber that subscribes in its `[Inject]` method. Publish from `Start` or later,
  or use `Enqueue`.
* **Assuming `Enqueue` is instant.** It is not; it lands at the end of the frame.
* **A branch typo.** `SubscribeBranch("Levels")` silently matches nothing. The debugger's subscription list
  shows the branch you actually registered.
* **Boxing where you did not expect it.** Turning `Record History` on, or adding any observer, boxes every
  published event. That is fine in the editor and usually fine in a build; it is not free.

---

## Tests

`Window > General > Test Runner > EditMode`, or from the command line:

```bash
Unity.exe -runTests -batchmode -projectPath <project> -testPlatform EditMode -testFilter "Game.Events.Tests"
```

The core tests build the service with its constructor, no container in sight. The integration tests use
`ZenjectUnitTestFixture`, install through the real `GlobalGameEventsInstaller` and drive Zenject's `TickableManager`,
`InitializableManager` and `DisposableManager` directly.

---

## No static

The module contains no static fields, properties, methods, classes, constructors or singletons, no
`[RuntimeInitializeOnLoadMethod]`, and no extension methods. `const` literals are used. Every cache — resolved
paths, branch match lists, the subscription registry, the queue — lives in an instance the container owns.

The single exception is `Editor/GameEventsDebuggerMenu.cs`: Unity registers main-menu entries only through static
methods. That method holds no state and only calls `GetWindow`. Delete the file and the same window is still
reachable from the installer's inspector button, leaving a module with zero occurrences of the keyword.

---

## Where this game uses it

The service is installed project wide, by the `GlobalGameEventsInstaller` on the `GlobalGameEvents` child of
`Assets/Resources/ProjectContext.prefab`. **No `SceneContext` may install a second one** — see
[One service or one per scene](#one-service-or-one-per-scene) for what that silently breaks.

The game's own events live in `Assets/_Scripts/Events/`, namespace `Game.Gameplay.Events`:

| Path | Raised by | When |
| --- | --- | --- |
| `Level.Start` | `GameplayLevelsController.LoadLevel` | A level is loaded and the player is on its spawn point. Queued, because the load runs from `Awake`. |
| `Level.Completed` | `WinGameState.Enter` | The run has been scored and saved. Carries the duration, stars, coins and whether it was a first completion or a new best. |
| `Level.Failed` | `LooseGameState.Enter` | The player died and the game over screen is up. |
| `Level.AllCompleted` | `WinGameState.Enter` | Right after `Level.Completed`, when the completed level is the last one the build has. |

`Level.*` is therefore the game's namespace: do not declare events on it from a module.
