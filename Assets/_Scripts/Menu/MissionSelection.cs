using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase.Analytics;

public class MissionSelection : MonoBehaviour {
    public static MissionSelection instance;

    public GameObject iconAd,SelectionPanel;
    public GameObject loadingUI, unlockallBtn, p1, p2;//, p3;
	public Image loadingBar;
    public Text TotalEarning;
	private int unlockedLevels;

    public GameObject[] characters;
    public GameObject[] Buttons;

    int panelNum;

	void OnEnable()
	{
        if (PlayerPrefs.GetInt("ADSUNLOCK") == 0)
        {
            //ConsoliAds.Instance.DestoryIconAd(iconAd, 0);
        }
        //GameObject.Find("Canvas").gameObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        EnableRespectivePanel.menuPanelNum = 3;

        if (PlayerPrefs.GetInt("UnlockedLevels") >= 0 && PlayerPrefs.GetInt("UnlockedLevels") < 15)
            panelNum = 1;
        else if (PlayerPrefs.GetInt("UnlockedLevels") >= 15 && PlayerPrefs.GetInt("UnlockedLevels") < 19)
            panelNum = 2;

        if (panelNum == 1)
        {
            p1.SetActive(true);
            p2.SetActive(false);
        }
        else if (panelNum == 2)
        {
            p1.SetActive(false);
            p2.SetActive(true);
        }

        if (PlayerPrefs.GetInt("UnlockedLevels") >= 19)
            unlockallBtn.SetActive(false);
    }

	// Use this for initialization
	void Start () {
		instance = this;
        //PlayerPrefs.SetInt("UnlockedLevels", 19);

        unlockedLevels = PlayerPrefs.GetInt ("UnlockedLevels");
        if(unlockedLevels >= Buttons.Length)
        {
            PlayerPrefs.SetInt("UnlockedLevels", Buttons.Length - 1);
            unlockedLevels = PlayerPrefs.GetInt("UnlockedLevels");
        }
		General.CurrentLevel = unlockedLevels;//-1;
        Buttons[PlayerPrefs.GetInt("UnlockedLevels")].gameObject.transform.GetChild(0).gameObject.SetActive(true);

    }
    
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			GameObject.Find ("ClickSound").GetComponent<AudioSource> ().Play ();
			GetComponent<MissionSelection> ().enabled = false;
			gameObject.SetActive (false);
			SelectionPanel.SetActive (true);
            SelectionPanel.GetComponent<Garrage> ().enabled = true;

            if (PlayerPrefs.GetInt("ADSUNLOCK") == 0)
            {
                //UnityAdsManager.Instance.ShowUnityVideoAd();
               // MobileAds.Instance.Show_InterstitialAd();
                //      McFairyAdsMediation.Instance.ShowInterstitialAd(0);

            }
            //GameObject.Find("Canvas").gameObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceCamera;
        }
		TotalEarning.text = PlayerPrefs.GetInt ("TotalScore").ToString () + "$";

        foreach (var item in characters)
        {
            item.SetActive(false);
        }
        
        for (int i = 0; i < Buttons.Length; i++)
		{
			if (i <= unlockedLevels) {
				Buttons [i].GetComponent<Button> ().interactable = true;
                //Image img = Buttons[i].gameObject.transform.GetChild(1).gameObject.GetComponent<Image>();
                //img.color = new Color(img.color.r, img.color.g, img.color.b, 1f);
                //Buttons[i].gameObject.transform.GetChild(1).gameObject.GetComponent<TweenRotation>().enabled = true;
                Buttons[i].gameObject.transform.GetChild(0).gameObject.SetActive(false);
			}
            //else
            //{
            //    Buttons[i].gameObject.transform.GetChild(0).gameObject.SetActive(false);
            //}
		}
	}

    public void RIGHT()
    {
        GameObject.Find("ClickSound").GetComponent<AudioSource>().Play();
        panelNum++;
        if (panelNum > 2)
            panelNum = 2;

        if (panelNum == 1)
        {
            p1.SetActive(true);
            p2.SetActive(false);
        }
        else if (panelNum == 2)
        {
            p1.SetActive(false);
            p2.SetActive(true);
        }
    }

    public void LEFT()
    {
        GameObject.Find("ClickSound").GetComponent<AudioSource>().Play();
        panelNum--;
        if (panelNum < 1)
            panelNum = 1;

        if (panelNum == 1)
        {
            p1.SetActive(true);
            p2.SetActive(false);
        }
        else if (panelNum == 2)
        {
            p1.SetActive(false);
            p2.SetActive(true);
        }
    }

    public void Back()
	{
		GameObject.Find ("ClickSound").GetComponent<AudioSource> ().Play ();
        SelectionPanel.SetActive (true);
		gameObject.SetActive (false);
        SelectionPanel.GetComponent<Garrage> ().enabled = true;
		GetComponent<MissionSelection> ().enabled = false;

        if (PlayerPrefs.GetInt("ADSUNLOCK") == 0)
        {
            //UnityAdsManager.Instance.ShowUnityVideoAd();
           //MobileAds.Instance.Show_InterstitialAd();
            //  McFairyAdsMediation.Instance.ShowInterstitialAd(0);
        }
        //GameObject.Find("Canvas").gameObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceCamera;
    }

	public void LevelSelected(int num)
	{
        GameObject.Find("ClickSound").GetComponent<AudioSource>().Play();

        General.CurrentLevel = num;
        StartCoroutine(AsynchronousLoad("GamePlay"));
        loadingUI.SetActive(true);
        GetComponent<MissionSelection>().enabled = false;
        string log = "level_" + num;
        if (Application.isEditor)
        {
            Debug.Log(log);
        }
        else
        {
            FirebaseAnalytics.LogEvent(log, log, 0);
            FirebaseAnalytics.LogEvent("level_up", "level_up", 0);
        }
        //MyMobileAds.ShowAd(()=> {

        //        GameObject.Find("ClickSound").GetComponent<AudioSource>().Play();

        //        General.CurrentLevel = num;
        //        StartCoroutine(AsynchronousLoad("GamePlay"));
        //        loadingUI.SetActive(true);
        //        GetComponent<MissionSelection>().enabled = false;
        //    });
    }
    
	IEnumerator AsynchronousLoad (string scene)
	{
		yield return null;

		AsyncOperation ao = SceneManager.LoadSceneAsync(scene);
		ao.allowSceneActivation = false;

		while (! ao.isDone)
		{
			// [0, 0.9] > [0, 1]
			float progress = Mathf.Clamp01(ao.progress / 0.99f);
            //Debug.Log("Loading progress: " + (progress * 100) + "%");
            loadingBar.fillAmount += (ao.progress);

			// Loading completed
			if (ao.progress == 0.9f)
			{
				ao.allowSceneActivation = true;
			}
			yield return null;
		}
	}



}
