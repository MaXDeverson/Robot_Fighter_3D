using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class InputManager : MonoBehaviour {
	
	[Header("Input Type")]
	public INPUTTYPE inputType;
	public List<InputControl> keyBoardControls = new List<InputControl>(); // a list of keyboard control input
	public List<InputControl> joypadControls = new List<InputControl>(); // a list of joypad control input

	[Header("Double Tap Settings")]
	public float doubleTapSpeed = .3f;
	private float  lastInputTime = 0f;
	private string lastInputAction = "";

	//delegates
	public delegate void DirectionInputEventHandler(Vector2 dir, bool doubleTapActive);
	public static event DirectionInputEventHandler onDirectionInputEvent;
	public delegate void InputEventHandler(string action, BUTTONSTATE buttonState);
	public static event InputEventHandler onInputEvent;

	[Space(15)]
	public static bool defendKeyDown;
	private float doubleTapTime;

	void Start(){

		//automatically enable touch controls on IOS or android
		#if UNITY_IOS || UNITY_ANDROID
			inputType = INPUTTYPE.TOUCHSCREEN;
		#endif
	}

	public static void DirectionEvent(Vector2 dir, bool doubleTapActive){
		if( onDirectionInputEvent != null) onDirectionInputEvent(dir, doubleTapActive);
	}
		
	void Update(){

		//use keyboard
		if (inputType == INPUTTYPE.KEYBOARD) KeyboardControls();

		//use joypad
		if (inputType == INPUTTYPE.JOYPAD) JoyPadControls();

	}

	void KeyboardControls(){
		float x = 0;
		float y = 0;
		bool doubleTapState = false;

		foreach(InputControl inputControl in keyBoardControls){
			if(onInputEvent == null) return;

			//on keyboard key down
			if(Input.GetKeyDown(inputControl.key)){
				doubleTapState = DetectDoubleTap(inputControl.Action);
				onInputEvent(inputControl.Action, BUTTONSTATE.PRESS);
			}

			//on keyboard key up
			if(Input.GetKeyUp(inputControl.key)){
				onInputEvent(inputControl.Action, BUTTONSTATE.RELEASE);
			}
				
			//convert keyboard direction keys to x,y values (every frame)
			if(Input.GetKey(inputControl.key)){
				if(inputControl.Action == "Left") x = -1f;
				else if(inputControl.Action == "Right") x = 1f;
				else if(inputControl.Action == "Up") y = 1;
				else if(inputControl.Action == "Down") y = -1;
			}

			//defend key exception (checks the defend state every frame)
			if(inputControl.Action == "Defend") defendKeyDown = Input.GetKey(inputControl.key);
		}

		//send a direction event
		DirectionEvent(new Vector2(x,y), doubleTapState);
	}

	void JoyPadControls(){
		if(onInputEvent == null) return;

		//on Joypad button press
		foreach(InputControl inputControl in joypadControls){
			if(Input.GetKeyDown(inputControl.key)) onInputEvent(inputControl.Action,BUTTONSTATE.PRESS);

			//defend key exception (checks the defend state every frame)
			if(inputControl.Action == "Defend") defendKeyDown = Input.GetKey(inputControl.key);
		}

		//get Joypad  direction axis
		float x = Input.GetAxis("Joypad Left-Right");
		float y = Input.GetAxis("Joypad Up-Down");

		//send a direction event
		DirectionEvent(new Vector2(x,y).normalized, false);
	}

	//this function is called when a touch screen button is pressed
	public void OnTouchScreenInputEvent(string action, BUTTONSTATE buttonState){
		onInputEvent(action, buttonState);

		//defend exception
		if(action == "Defend") defendKeyDown = (buttonState == BUTTONSTATE.PRESS);
	}

	//this function is used for the touch screen thumb-stick
	public void OnTouchScreenJoystickEvent(Vector2 joystickDir){
		DirectionEvent(joystickDir.normalized, false);
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
[System.Serializable]
public class InputControl {
	public string Action;
	public INPUTTYPE inputType;
	public KeyCode key;
}

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

//-------------
//   EDITOR SCRIPT
//-------------
#if UNITY_EDITOR
[CustomEditor(typeof(InputManager))]
public class InputManagerEditor : Editor {

	public override void OnInspectorGUI(){
		InputManager inputManager = (InputManager)target;
		EditorGUIUtility.labelWidth = 120;
		EditorGUIUtility.fieldWidth = 100;

		//input type
		GUILayout.Space(10);
		EditorGUILayout.LabelField("Input Type", EditorStyles.boldLabel);
		inputManager.inputType = (INPUTTYPE)EditorGUILayout.EnumPopup("Input Type:", inputManager.inputType);
		GUILayout.Space(15);

		//keyboard controls	
		if(inputManager.inputType == INPUTTYPE.KEYBOARD) {
			EditorGUILayout.LabelField("Keyboard Keys", EditorStyles.boldLabel);
			foreach(InputControl inputControl in inputManager.keyBoardControls){
				GUILayout.BeginHorizontal();
				inputControl.Action = EditorGUILayout.TextField("Action:", inputControl.Action);
				inputControl.key = (KeyCode)EditorGUILayout.EnumPopup("Key:", inputControl.key, GUILayout.Width(350));
				GUILayout.EndHorizontal();
			}
		}

		//joypad controls	
		if(inputManager.inputType == INPUTTYPE.JOYPAD){
			EditorGUILayout.LabelField("Joypad Keys", EditorStyles.boldLabel);
			EditorGUILayout.LabelField("* The direction keys are mapped onto the joypad thumbstick.");

			foreach(InputControl inputControl in inputManager.joypadControls){
				GUILayout.BeginHorizontal();
				inputControl.Action = EditorGUILayout.TextField("Action:", inputControl.Action);
				inputControl.key = (KeyCode)EditorGUILayout.EnumPopup("Key:", inputControl.key, GUILayout.Width(350));
				GUILayout.EndHorizontal();
			}
		}

		//touch Screen controls
		if(inputManager.inputType == INPUTTYPE.TOUCHSCREEN){
			EditorGUILayout.LabelField("* You can edit the touchscreen buttons in the 'UI' prefab in the project folder.");
			EditorGUILayout.LabelField("   Inside the prefab go to: UI/Canvas/TouchScreenControls");
		}
		GUILayout.Space(15);

		if(inputManager.inputType == INPUTTYPE.KEYBOARD || inputManager.inputType == INPUTTYPE.JOYPAD){
			GUILayout.BeginHorizontal();
			
			//button: add a new action 
			if(GUILayout.Button("Add Input Action", GUILayout.Width(130), GUILayout.Height(25))){
				if(inputManager.inputType == INPUTTYPE.KEYBOARD) inputManager.keyBoardControls.Add(new InputControl());
				if(inputManager.inputType == INPUTTYPE.JOYPAD) inputManager.joypadControls.Add(new InputControl());
			}

			//button: delete last action 
			bool showDeleteButton = (inputManager.inputType == INPUTTYPE.KEYBOARD && inputManager.keyBoardControls.Count>0) || (inputManager.inputType == INPUTTYPE.JOYPAD && inputManager.joypadControls.Count>0) ? true : false;
			if(showDeleteButton && GUILayout.Button ("Delete Input Action", GUILayout.Width(130), GUILayout.Height(25))){
				if(inputManager.inputType == INPUTTYPE.KEYBOARD && inputManager.keyBoardControls.Count>0) inputManager.keyBoardControls.RemoveAt(inputManager.keyBoardControls.Count-1);
				if(inputManager.inputType == INPUTTYPE.JOYPAD && inputManager.joypadControls.Count>0) inputManager.joypadControls.RemoveAt(inputManager.joypadControls.Count-1);
			}

			GUILayout.EndHorizontal();
			GUILayout.Space(15);
		}

		//double tap settings
		EditorGUILayout.LabelField("Double Tap Settings", EditorStyles.boldLabel);
		inputManager.doubleTapSpeed = EditorGUILayout.FloatField("Double Tap Speed:", inputManager.doubleTapSpeed);
		EditorUtility.SetDirty (inputManager);
	}
}
#endif