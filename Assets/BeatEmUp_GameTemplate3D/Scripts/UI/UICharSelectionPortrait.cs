using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UICharSelectionPortrait : MonoBehaviour {

	public bool Selected;

	[Header("The Player Character Prefab")]
	public GameObject PlayerPrefab;

	[Header("HUD Portrait")]
	public Sprite HUDPortrait;

	public void OnClick(){
		Selected = true;

		//set selected player prefab
		UICharSelection characterSelectionScrn = GameObject.FindObjectOfType<UICharSelection>();
		if(characterSelectionScrn) characterSelectionScrn.SelectPlayer(PlayerPrefab);

	}
}