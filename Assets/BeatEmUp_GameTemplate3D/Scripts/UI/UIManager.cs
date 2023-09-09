using UnityEngine;

public class UIManager : MonoBehaviour {

	public UIFader UI_fader;
	public UI_Screen[] UIMenus;

	void Awake(){
		DisableAllScreens();

		//don't destroy
		//DontDestroyOnLoad(gameObject);///////////////////////////////////////////////
	}
		
	//shows a menu by name
	public void ShowMenu(string name, bool disableAllScreens){
		if(disableAllScreens) DisableAllScreens();

		foreach (UI_Screen UI in UIMenus){
			if (UI.UI_Name == name) {
				if (UI.UI_Gameobject != null) {
					UI.UI_Gameobject.SetActive (true);
					SetTouchScreenControls(UI);

				} else {
					Debug.Log ("no menu found with name: " + name);
				}
			}
		}

		//fadeIn
		if (UI_fader != null) UI_fader.gameObject.SetActive (true);
		UI_fader.Fade (UIFader.FADE.FadeIn, .5f, .3f);
	}

	public void ShowMenu(string name){
		ShowMenu(name, true);
	}

	//close a menu by name
	public void CloseMenu(string name){
		foreach (UI_Screen UI in UIMenus){
			if (UI.UI_Name == name)	UI.UI_Gameobject.SetActive (false);
		}
	}
		
	//disable all the menus
	public void DisableAllScreens(){
		foreach (UI_Screen UI in UIMenus){ 
			if(UI.UI_Gameobject != null) 
				UI.UI_Gameobject.SetActive(false);
			else 
				Debug.Log("Null ref found in UI with name: " + UI.UI_Name);
		}
	}

	//show or hide touch screen controls
	void SetTouchScreenControls(UI_Screen UI){
		if(UI.UI_Name == "TouchScreenControls") return;
		InputManager inputManager = GameObject.FindObjectOfType<InputManager>();
		if(inputManager != null && inputManager.inputType == INPUTTYPE.TOUCHSCREEN){
			if(UI.showTouchControls){
				ShowMenu("TouchScreenControls", false);
			} else {
				CloseMenu("TouchScreenControls");
			}
		}
	}
}
	
[System.Serializable]
public class UI_Screen {
	public string UI_Name;
	public GameObject UI_Gameobject;
	public bool showTouchControls;
}
