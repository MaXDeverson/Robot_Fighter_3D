using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Garrage : MonoBehaviour {
	public GameObject MissionSelectionPanel;
    public GameObject MainMenuPanel, POS_ROT;
    
    private int count;
    public GameObject[] characters;
    public GameObject[] active_characters;
    public GameObject[] specs;
	public float speed = 2.5f;
	public int[] prices;
	public GameObject nextBtn,buyBtn, unlockallBtn;
	public GameObject lockPic;
	private int currCarPrice;
	public Text price, TotalEarning;
	public GameObject cashDialog;
	AsyncOperation ao;

    void OnEnable()
	{
        //GameObject.Find("Canvas").gameObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceCamera;
        EnableRespectivePanel.menuPanelNum = 2;
        count = 0;
        ActiveVehicles(count);

        characters[count].transform.GetChild(0).transform.eulerAngles = POS_ROT.transform.eulerAngles;
        characters[count].transform.GetChild(0).transform.localPosition = POS_ROT.transform.position;

        if (PlayerPrefs.GetInt("jetPrice_01") == 0 && PlayerPrefs.GetInt("jetPrice_02") == 0 && PlayerPrefs.GetInt("jetPrice_03") == 0
            && PlayerPrefs.GetInt("jetPrice_04") == 0)
            unlockallBtn.SetActive(false);

    }
	void Awake()
	{
		PlayerPrefs.SetInt ("SelectedCharacter", 0);
	}
    
	void Update () {
        if (Input.GetKeyDown(KeyCode.Escape))
		{
			GameObject.Find ("ClickSound").GetComponent<AudioSource> ().Play ();
			GetComponent<Garrage> ().enabled = false;
			HideAllVehicles ();
			gameObject.SetActive (false);
			MainMenuPanel.SetActive (true);
			MainMenuPanel.GetComponent<Main_Menu> ().enabled = true;

            //GameObject.Find("Canvas").gameObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        }
		TotalEarning.text = PlayerPrefs.GetInt ("TotalScore").ToString () + "$";
        prices[0] = 0; prices[1] = PlayerPrefs.GetInt("jetPrice_01"); prices[2] = PlayerPrefs.GetInt("jetPrice_02");
        prices[3] = PlayerPrefs.GetInt("jetPrice_03");
        prices[4] = PlayerPrefs.GetInt("jetPrice_04");
        //prices[5] = PlayerPrefs.GetInt("jetPrice_05");

        //for (int i = 0; i < characters.Length; i++)
        //{
        //    if (count == i)
        //    {
        //        specs[i].SetActive(true);
        //    }
        //    else
        //    {
        //        specs[i].SetActive(false);
        //    }
        //}

        if (count < 0)
        {
            count = characters.Length - 1;
        }
        if (count == characters.Length)
        {
            count = 0;
        }

        if (currCarPrice == 0 || PlayerPrefs.GetInt ("Unlocked" + count) == count) {
			price.text = "Owned".ToString ();
        } else {
			price.text = currCarPrice.ToString ();
        }
        //Debug.Log("count is" + count);
		currCarPrice = prices [count];
		if (currCarPrice == 0 || PlayerPrefs.GetInt ("Unlocked" + count) == count) {
			buyBtn.SetActive (false);
            nextBtn.SetActive(true);
            lockPic.SetActive (false);
		} else {
			buyBtn.SetActive (true);
            nextBtn.SetActive(false);
            lockPic.SetActive (true);
		}
	}
   

    public void Next()
	{
		if (buyBtn.activeSelf == false) 
		{
            GameObject.Find("ClickSound").GetComponent<AudioSource>().Play();
            EnableRespectivePanel.menuPanelNum = 3;
            gameObject.SetActive (false);
			MissionSelectionPanel.SetActive (true);
			HideAllVehicles();
			PlayerPrefs.SetInt ("SelectedCharacter", count);
			GetComponent<Garrage> ().enabled = false;
			MissionSelectionPanel.GetComponent<MissionSelection> ().enabled = true;
		}
        //GameObject.Find("Canvas").gameObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

	}
	
	public void Buy()
	{
		GameObject.Find ("ClickSound").GetComponent<AudioSource> ().Play ();
        characters[count].transform.GetChild(0).transform.eulerAngles = POS_ROT.transform.eulerAngles;
        characters[count].transform.GetChild(0).transform.localPosition = POS_ROT.transform.position;

        if (PlayerPrefs.GetInt ("TotalScore") >= currCarPrice) {
			PlayerPrefs.SetInt ("TotalScore", PlayerPrefs.GetInt ("TotalScore") - currCarPrice);
			PlayerPrefs.SetInt ("Unlocked" + count, count);
		} else {
			HideAllVehicles ();
			cashDialog.SetActive (true);
		}
	}
	public void Back()
	{
		GameObject.Find ("ClickSound").GetComponent<AudioSource> ().Play ();
		gameObject.SetActive (false);
		MainMenuPanel.SetActive (true);
		MainMenuPanel.GetComponent<Main_Menu> ().enabled = true;
		GetComponent<Garrage> ().enabled = false;
		HideAllVehicles ();
        

        //GameObject.Find("Canvas").gameObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
    }
    public void LeftArrow()
	{
		GameObject.Find ("ClickSound").GetComponent<AudioSource> ().Play ();

        characters[count].transform.GetChild(0).transform.eulerAngles = POS_ROT.transform.eulerAngles;
        characters[count].transform.GetChild(0).transform.localPosition = POS_ROT.transform.position;

        count = count - 1;
        if (count < 0) {
            count = characters.Length - 1;
		}
        ActiveVehicles(count);
    }
    public void RightArrow()
	{
        GameObject.Find ("ClickSound").GetComponent<AudioSource> ().Play ();

        characters[count].transform.GetChild(0).transform.eulerAngles = POS_ROT.transform.eulerAngles;
        characters[count].transform.GetChild(0).transform.localPosition = POS_ROT.transform.position;

        count = count + 1;
		if (count >= characters.Length) {
            count = 0;
		}
        ActiveVehicles(count);
    }
	public void Ok()
	{
		GameObject.Find ("ClickSound").GetComponent<AudioSource> ().Play ();
		cashDialog.SetActive (false);
        ActiveVehicles(count);
	}

    public void AddMoney()
    {
        PlayerPrefs.SetInt("TotalScore", PlayerPrefs.GetInt("TotalScore") + 10000);
    }

    
    public void ActiveVehicles(int j)
	{
        count = j;
		for (int i = 0; i < characters.Length; i++) {
			if (i == j) {
                characters[i].SetActive (true);
                active_characters[i].SetActive(true);
                specs[i].SetActive(true);
               // Debug.Log(specs[i].name);
			} else {
                characters[i].SetActive (false);
                active_characters[i].SetActive(false);
                specs[i].SetActive(false);
			}
		}
	}

	void HideAllVehicles ()
	{
		for (int i = 0; i < characters.Length; i++) {
            characters[i].SetActive (false);
		}
	}
}
