using UnityEngine;
using UnityEngine.InputSystem;

// Bridges the new Input System (GameInput / Resources/GameControls.inputactions) and the touch screen controls
// to the template's input events. Keyboard, gamepad and touch all end up in onInputEvent / onDirectionInputEvent.
public class InputManager : MonoBehaviour {

	[Header("Input Type")]
	[Tooltip("The kind of device the player uses (touch screen, keyboard or joypad).")]
	public INPUTTYPE inputType;
	[Tooltip("Follow the device the player used last (touch, keyboard or gamepad). Turn off to force the input type above.")]
	public bool autoDetectInputType = true;
	[Tooltip("Hide the touch screen controls while the player uses a gamepad. They come back on the next touch, key press or click.")]
	public bool hideTouchControlsWithGamepad = true;

	[Header("Double Tap Settings")]
	public float doubleTapSpeed = .3f;
	private float  lastInputTime = 0f;
	private string lastInputAction = "";

	//delegates
	public delegate void DirectionInputEventHandler(Vector2 dir, bool doubleTapActive);
	public static event DirectionInputEventHandler onDirectionInputEvent;
	public delegate void InputEventHandler(string action, BUTTONSTATE buttonState);
	public static event InputEventHandler onInputEvent;
	public delegate void InputTypeEventHandler(INPUTTYPE inputType);
	public static event InputTypeEventHandler onInputTypeChanged;

	[Space(15)]
	public static bool defendKeyDown;
	private bool touchDefendDown; //defend button of the touch screen controls
	private bool doubleTapState;
	private Vector2 touchScreenJoystickDir; //last direction of the touch screen thumb-stick
	private Vector2 lastDigitalDir; //Move action as -1/0/1 per axis in the previous frame

	private static InputManager instance;

	//the input type of the current InputManager, or the last used device when there is none (e.g. in the menu scene)
	public static INPUTTYPE CurrentInputType {
		get { return instance != null ? instance.inputType : GameInput.CurrentInputType; }
	}

	//the touch screen controls are shown on touch screens and on PC (they can be clicked with the mouse), only a gamepad hides them
	public static bool ShowTouchControls {
		get {
			if(CurrentInputType != INPUTTYPE.JOYPAD) return true;
			return instance != null && !instance.hideTouchControlsWithGamepad;
		}
	}

	//the input actions that are sent as button events, their names are the action names used by the game
	private static InputAction[] ButtonActions {
		get { return new InputAction[] { GameInput.Punch, GameInput.Kick, GameInput.Jump, GameInput.Defend, GameInput.Run }; }
	}

	void OnEnable(){
		instance = this;
		if(autoDetectInputType) SetInputType(GameInput.CurrentInputType);
		GameInput.onInputTypeChanged += OnDeviceChanged;

		if(!GameInput.IsReady) return;
		foreach(InputAction action in ButtonActions){
			action.performed += OnButtonPerformed;
			action.canceled += OnButtonCanceled;
		}
	}

	void OnDisable(){
		if(instance == this) instance = null;
		GameInput.onInputTypeChanged -= OnDeviceChanged;
		defendKeyDown = false;

		if(!GameInput.IsReady) return;
		foreach(InputAction action in ButtonActions){
			action.performed -= OnButtonPerformed;
			action.canceled -= OnButtonCanceled;
		}
	}

	public static void DirectionEvent(Vector2 dir, bool doubleTapActive){
		if( onDirectionInputEvent != null) onDirectionInputEvent(dir, doubleTapActive);
	}

	void Update(){
		if(!GameInput.IsReady) return;

		//keyboard (WASD / arrows) and gamepad (left stick / d-pad) direction
		Vector2 moveDir = GameInput.Move.ReadValue<Vector2>().normalized;
		SendDirectionButtonEvents(moveDir);

		//when no direction is pressed, keep the touch screen joystick direction instead of overwriting it with zero
		Vector2 dir = moveDir != Vector2.zero ? moveDir : touchScreenJoystickDir;
		DirectionEvent(dir, doubleTapState);
		doubleTapState = false;

		//defend is a hold action, its state is checked every frame
		defendKeyDown = touchDefendDown || GameInput.Defend.IsPressed();
	}

	//input action pressed (keyboard / gamepad)
	void OnButtonPerformed(InputAction.CallbackContext context){
		if(Time.timeScale <= 0f) return; //the game is paused or a dialog is open, the button belongs to the UI
		DetectDoubleTap(context.action.name);
		SendButtonEvent(context.action.name, BUTTONSTATE.PRESS);
	}

	//input action released (keyboard / gamepad)
	void OnButtonCanceled(InputAction.CallbackContext context){
		SendButtonEvent(context.action.name, BUTTONSTATE.RELEASE);
	}

	//sends Left, Right, Up and Down button events when the Move direction changes, used for double tap running and UI navigation
	void SendDirectionButtonEvents(Vector2 dir){
		Vector2 digitalDir = new Vector2(ToDigital(dir.x), ToDigital(dir.y));

		if(digitalDir.x != lastDigitalDir.x){
			if(lastDigitalDir.x != 0) SendButtonEvent(lastDigitalDir.x < 0? "Left" : "Right", BUTTONSTATE.RELEASE);
			if(digitalDir.x != 0) PressDirection(digitalDir.x < 0? "Left" : "Right");
		}

		if(digitalDir.y != lastDigitalDir.y){
			if(lastDigitalDir.y != 0) SendButtonEvent(lastDigitalDir.y < 0? "Down" : "Up", BUTTONSTATE.RELEASE);
			if(digitalDir.y != 0) PressDirection(digitalDir.y < 0? "Down" : "Up");
		}

		lastDigitalDir = digitalDir;
	}

	void PressDirection(string action){
		if(DetectDoubleTap(action)) doubleTapState = true;
		SendButtonEvent(action, BUTTONSTATE.PRESS);
	}

	static float ToDigital(float value){
		if(value > .5f) return 1f;
		if(value < -.5f) return -1f;
		return 0f;
	}

	void SendButtonEvent(string action, BUTTONSTATE buttonState){
		if(onInputEvent != null) onInputEvent(action, buttonState);
	}

	//this function is called when a touch screen button is pressed
	public void OnTouchScreenInputEvent(string action, BUTTONSTATE buttonState){

		//defend exception
		if(action == "Defend") {
			touchDefendDown = (buttonState == BUTTONSTATE.PRESS);
			defendKeyDown = touchDefendDown || (GameInput.IsReady && GameInput.Defend.IsPressed());
		}

		SendButtonEvent(action, buttonState);
	}

	//this function is used for the touch screen thumb-stick
	public void OnTouchScreenJoystickEvent(Vector2 joystickDir){
		touchScreenJoystickDir = joystickDir.normalized;
		DirectionEvent(touchScreenJoystickDir, false);
	}

	//the player used another kind of device
	void OnDeviceChanged(INPUTTYPE type){
		if(autoDetectInputType) SetInputType(type);
	}

	public void SetInputType(INPUTTYPE type){
		if(inputType == type) return;
		inputType = type;
		if(onInputTypeChanged != null) onInputTypeChanged(type);
	}

	//returns true if a key double tap is detected
	bool DetectDoubleTap(string action){
		bool doubleTapDetected = ((Time.time - lastInputTime < doubleTapSpeed) && (lastInputAction == action));
		lastInputAction = action;
		lastInputTime = Time.time;
		return doubleTapDetected;
	}
}

//---------------
//    ENUMS
//---------------
public enum INPUTTYPE {
	KEYBOARD = 0,
	JOYPAD = 5,
	TOUCHSCREEN = 10,
}

public enum BUTTONSTATE {
	PRESS = 0,
	RELEASE = 5,
	HOLD = 10,
}
