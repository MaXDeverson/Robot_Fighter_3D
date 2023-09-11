﻿using UnityEngine;

public class PlayerSpawnPoint : MonoBehaviour {

	//public GameObject defaultPlayerPrefab;
	public GameObject[] Players;
    public Weapon[] _weapons;
    private GameObject _currentPlayer;
	void Awake(){
        //PlayerPrefs.SetInt("SelectedCharacter", 1);//////////////////.....................


        int selectedChar = PlayerPrefs.GetInt("SelectedCharacter");
        ////////////////////////////////////////////////
        ////get selected player from character selection screen
        //if(GlobalGameSettings.Player1Prefab) {
        //	loadPlayer(GlobalGameSettings.Player1Prefab);
        //	return;
        //}	
        ///////////////////////////////////////////////////


        loadPlayer(Players[selectedChar]);


        //      //otherwise load default character
        //      if (defaultPlayerPrefab) {
        //	//loadPlayer(defaultPlayerPrefab);
        //	loadPlayer(Players[selectedChar]);
        //} else {
        //	Debug.Log("Please assign a default player prefab in the  playerSpawnPoint");
        //}
    }

	//load a player prefab
	void loadPlayer(GameObject playerPrefab){
		_currentPlayer = GameObject.Instantiate(playerPrefab) as GameObject;
		_currentPlayer.transform.position = transform.position;
	}

    public void TakeWeapon(int index)
    {
        _currentPlayer.GetComponent<PlayerCombat>().equipWeapon(_weapons[index]);
    }
}