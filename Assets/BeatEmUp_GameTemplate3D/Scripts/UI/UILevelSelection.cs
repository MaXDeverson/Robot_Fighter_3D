using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UILevelSelection : MonoBehaviour {

	#if UNITY_EDITOR
	[HelpAttribute("Open the 'Level Data' Dropdown to add/edit level information. If you want to change the layout go to the 'LevelGrid' Gameobject and edit the settings of the 'Grid Layout Group'.", UnityEditor.MessageType.Info)]
	#endif

	public GameObject LevelItemPrefab;
	public GameObject UIGrid;
	public LevelData[] levelData;
	private bool selectFirstLevel = true;

	void Start(){
		int levelCount = 0;

		//Create level items in UIgrid
		foreach(LevelData _leveldata in levelData){

			//Save level list in globalGameData for later use
			GlobalGameSettings.LevelData.Add(_leveldata);

			//Create a UI level item
			GameObject UILevelItem = GameObject.Instantiate(LevelItemPrefab, UIGrid.transform) as GameObject;

			//Fill UI level item with data
			if(UILevelItem != null){
				UILevelItem.name = _leveldata.levelTitle;

				//pass level data to level item
				UILevelItem levelItem = UILevelItem.GetComponent<UILevelItem>();
				if(levelItem != null){ 
					levelItem.levelData = _leveldata;
					levelItem.levelData.levelId = levelCount;
					levelCount ++;
				}

				//Select the 1st level
				if(selectFirstLevel) {
					EventSystem.current.SetSelectedGameObject(UILevelItem);
					selectFirstLevel = false;
				}

				//Set level text
				Text levelTitle = UILevelItem.GetComponentInChildren<Text>();
				if(levelTitle != null)  levelTitle.text = _leveldata.levelTitle;

				//Load level sprite...if there is one
				Image levelImg = UILevelItem.GetComponent<Image>();
				if(levelImg == null) return;

				if(_leveldata.levelSprite == null) {
					levelImg.enabled = false;
					Debug.Log("No level sprite assigned for " + _leveldata.levelTitle);
					 
				} else {
					levelImg.sprite = _leveldata.levelSprite;
				}
			}
		}
	}
}

[System.Serializable]
public class LevelData {
	public string levelTitle = "";
	public Sprite levelSprite;
	public string sceneToLoad = "";
	[HideInInspector] public int levelId = 0;
}