using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSpawnPoint : MonoBehaviour
{

    //public GameObject defaultPlayerPrefab;
    [SerializeField] private Transform _playerSpawnPosiiton;
    [SerializeField] private Manager _manager;
    public GameObject[] Players;
    public Weapon[] _weapons;
    public Text[] _choisesText;
    private GameObject _currentPlayer;
    void Awake()
    {
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
    private void Start()
    {
        for (int i = 0; i < _choisesText.Length; i++)
        {
            _choisesText[i].text = _weapons[i].weaponName;
        }
    }

    void loadPlayer(GameObject playerPrefab)
    {
        _currentPlayer = GameObject.Instantiate(playerPrefab, _playerSpawnPosiiton.position,Quaternion.identity) as GameObject;
       // _currentPlayer.transform.position = _playerSpawnPosiiton.position;
    }

    public void TakeWeapon(int index)
    {
        if (!MyMobileAds.ShowRewardedSucssesCheck(() => { _currentPlayer.GetComponent<PlayerCombat>().equipWeapon(_weapons[index]); _manager.HideWeaponChoice();}))
        {
            _manager.ShowDialogWindow("No internet connection :(");
        }

    }
}