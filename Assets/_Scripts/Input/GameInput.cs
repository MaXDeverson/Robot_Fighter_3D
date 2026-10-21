using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Entry point of the new Input System for the whole game.
/// Loads Resources/GameControls.inputactions, keeps its action maps enabled in every scene,
/// tracks which kind of device the player used last (touch, keyboard or gamepad)
/// and makes sure every EventSystem runs on InputSystemUIInputModule.
/// Walking the menus with a gamepad and the gamepad prompts are in Modules/GamepadModule.
/// </summary>
public static class GameInput
{
    public const string ActionsResourcePath = "GameControls";

    public static InputActionAsset Actions { get; private set; }

    // "Player" map: fighting and movement
    public static InputAction Move { get; private set; }
    public static InputAction Punch { get; private set; }
    public static InputAction Kick { get; private set; }
    public static InputAction Jump { get; private set; }
    public static InputAction Defend { get; private set; }
    public static InputAction Run { get; private set; }

    // "Menu" map: pause and menu shortcuts
    public static InputAction Pause { get; private set; }
    public static InputAction Back { get; private set; }
    public static InputAction Submit { get; private set; }

    public static bool IsReady => Actions != null;

    // the kind of device the player used last
    public static INPUTTYPE CurrentInputType { get; private set; } = INPUTTYPE.KEYBOARD;
    public static event Action<INPUTTYPE> onInputTypeChanged;

    // device changes are detected inside the Input System update and applied in GameInputRunner.Update
    private static INPUTTYPE? _pendingInputType;

    public static bool WasPressedThisFrame(InputAction action) => action != null && action.WasPressedThisFrame();

    // Enter Play Mode Options keep static state between play sessions, so every session starts clean
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        InputSystem.onEvent -= OnInputSystemEvent;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Actions != null) Actions.Disable();

        Actions = null;
        Move = Punch = Kick = Jump = Defend = Run = null;
        Pause = Back = Submit = null;
        onInputTypeChanged = null;
        _pendingInputType = null;
        CurrentInputType = INPUTTYPE.KEYBOARD;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        Actions = Resources.Load<InputActionAsset>(ActionsResourcePath);
        if (Actions == null)
        {
            Debug.LogError("GameInput: Resources/" + ActionsResourcePath + ".inputactions was not found, game input is disabled.");
            return;
        }

        InputActionMap player = Actions.FindActionMap("Player", true);
        Move = player.FindAction("Move", true);
        Punch = player.FindAction("Punch", true);
        Kick = player.FindAction("Kick", true);
        Jump = player.FindAction("Jump", true);
        Defend = player.FindAction("Defend", true);
        Run = player.FindAction("Run", true);

        InputActionMap menu = Actions.FindActionMap("Menu", true);
        Pause = menu.FindAction("Pause", true);
        Back = menu.FindAction("Back", true);
        Submit = menu.FindAction("Submit", true);

        Actions.Enable();

        CurrentInputType = Application.isMobilePlatform ? INPUTTYPE.TOUCHSCREEN : INPUTTYPE.KEYBOARD;
        InputSystem.onEvent += OnInputSystemEvent;

        GameObject runner = new GameObject("[GameInput]");
        UnityEngine.Object.DontDestroyOnLoad(runner);
        runner.AddComponent<GameInputRunner>();
    }

    // switch the input type to the device the player actually pressed something on
    private static void OnInputSystemEvent(InputEventPtr eventPtr, InputDevice device)
    {
        if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>()) return;

        // input that goes to editor windows while the Game view is not focused
        if (InputState.currentUpdateType == InputUpdateType.Editor) return;

        INPUTTYPE type;
        if (device is Touchscreen) type = INPUTTYPE.TOUCHSCREEN;
        else if (device is Gamepad) type = INPUTTYPE.JOYPAD;
        else if (device is Keyboard || device is Mouse) type = INPUTTYPE.KEYBOARD;
        else return; // pen and sensors keep the current input type

        if (type == (_pendingInputType ?? CurrentInputType)) return;

        // the Android back button arrives as a keyboard key, it must not hide the touch controls
        if (type == INPUTTYPE.KEYBOARD && Application.isMobilePlatform) return;

        // a touch is always deliberate; a mouse switches only on a click, not on movement;
        // for keyboards and gamepads ignore stick drift and key releases
        if (device is Mouse && eventPtr.GetFirstButtonPressOrNull() == null) return;
        if (type != INPUTTYPE.TOUCHSCREEN && eventPtr.GetFirstButtonPressOrNull(0.5f, false) == null) return;

        _pendingInputType = type;
    }

    internal static void ApplyPendingInputType()
    {
        if (_pendingInputType == null) return;

        INPUTTYPE type = _pendingInputType.Value;
        _pendingInputType = null;
        if (type == CurrentInputType) return;

        CurrentInputType = type;
        onInputTypeChanged?.Invoke(type);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ReplaceLegacyUIInputModules();
    }

    // StandaloneInputModule reads UnityEngine.Input, which throws when only the new Input System is active.
    // The game scenes are already converted; this keeps any scene or prefab that was missed working.
    private static void ReplaceLegacyUIInputModules()
    {
        StandaloneInputModule[] legacyModules = UnityEngine.Object.FindObjectsByType<StandaloneInputModule>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (StandaloneInputModule legacyModule in legacyModules)
        {
            GameObject eventSystem = legacyModule.gameObject;
            UnityEngine.Object.DestroyImmediate(legacyModule);
            if (!eventSystem.TryGetComponent(out InputSystemUIInputModule _)) eventSystem.AddComponent<InputSystemUIInputModule>();
            Debug.LogWarning("GameInput: replaced StandaloneInputModule with InputSystemUIInputModule on '" + eventSystem.name + "' in scene '" + eventSystem.scene.name + "'. Replace it in the scene to remove this warning.");
        }
    }
}
