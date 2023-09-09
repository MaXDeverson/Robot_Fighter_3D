using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIButtonEvents : UISceneLoader, IPointerDownHandler, ISelectHandler, ISubmitHandler {

	public bool selectOnStart;
	public string SFXOnButtonPress = "";
	public string SFXOnButtonSelect = "";
	private InputManager inputManager;
	[HideInInspector]
	public float menuOpenTime;

	void OnEnable() {
		menuOpenTime = Time.time;
		InputManager.onInputEvent += OnInputEvent;
		if(inputManager == null) inputManager = GameObject.FindObjectOfType<InputManager>();
		if(selectOnStart) EventSystem.current.SetSelectedGameObject(gameObject);
	}

	void OnDisable() {
		InputManager.onInputEvent -= OnInputEvent;
	}
		
	void OnInputEvent(string action, BUTTONSTATE buttonState){
		if(buttonState != BUTTONSTATE.PRESS) return;
		
		//only apply the following actions if this UI gameobject is currently selected
		if(EventSystem.current.currentSelectedGameObject != gameObject) return;

		//move navigation up
		if(action == "Up"){
			Selectable elementUp = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnUp();
			if(elementUp != null) StartCoroutine(selectUIItem(elementUp.gameObject));
		}

		//move navigation down
		if(action == "Down"){
			Selectable elementDown = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnDown();
			if(elementDown != null) StartCoroutine(selectUIItem(elementDown.gameObject));
		}

		//move navigation left
		if(action == "Left"){
			Selectable elementLeft = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnLeft();
			if(elementLeft != null) StartCoroutine(selectUIItem(elementLeft.gameObject));
		}

		//move navigation right
		if(action == "Right"){
			Selectable elementRight = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnRight();
			if(elementRight != null) StartCoroutine(selectUIItem(elementRight.gameObject));
		}
	}

	//select another ui gameobject
	IEnumerator selectUIItem(GameObject GO){
		yield return null; //skip 1 frame, otherwise the OnControllerInputEvent triggers all ui items in a row
		EventSystem.current.SetSelectedGameObject(GO);
	}

	void Update(){

		//ensure a button select even when a mouse takes the focus of a button away
		if(inputManager == null) return;
		if(inputManager.inputType == INPUTTYPE.JOYPAD || inputManager.inputType == INPUTTYPE.KEYBOARD){
			if(EventSystem.current.currentSelectedGameObject == null && selectOnStart) EventSystem.current.SetSelectedGameObject(gameObject);
		}
	}

	//Play a sfx when this button is selected or on mouseover
	public virtual void OnSelect(BaseEventData eventData){
		if(Time.time - menuOpenTime > .1f) GlobalAudioPlayer.PlaySFX(SFXOnButtonSelect);
	}

	//Play a sfx when this button is activated by a controller
	public virtual void OnSubmit(BaseEventData eventData){
		GlobalAudioPlayer.PlaySFX(SFXOnButtonPress);
	}

	//Play a sfx when this button is clicked or touched
	public virtual void OnPointerDown(PointerEventData eventData){
		GlobalAudioPlayer.PlaySFX(SFXOnButtonPress);
	}
}