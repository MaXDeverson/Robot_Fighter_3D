using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Main_Menu : MonoBehaviour {
    
    public GameObject removeAdBtn, unlockallBtn, SelectionPanel;
	public GameObject MissionSelectionPanel;
	public GameObject quitPanel;
	public GameObject settingsPanel, inappPanel;
	public Text TotalEarning;

    public Image soundB, musicB;
    public AudioSource music;
    public static int inappCounterVar = 0;

    // Use this for initialization
    void OnEnable () {
        //GameObject.Find("Canvas").gameObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        //PlayerPrefs.DeleteAll();
        
        EnableRespectivePanel.menuPanelNum = 1;
        if (PlayerPrefs.GetInt("ADSUNLOCK") != 0)
        {
            removeAdBtn.SetActive(false);
        }
        if (PlayerPrefs.GetInt("UnlockedLevels") >= 19 && PlayerPrefs.GetInt("ADSUNLOCK") != 0
            && PlayerPrefs.GetInt("jetPrice_01") == 0 && PlayerPrefs.GetInt("jetPrice_02") == 0
            && PlayerPrefs.GetInt("jetPrice_03") == 0 && PlayerPrefs.GetInt("jetPrice_04") == 0)
        {
            unlockallBtn.SetActive(false);
        }

        if(inappCounterVar % 3 == 0)
        {
            print("Counter Panel : " + inappCounterVar);
            if (PlayerPrefs.GetInt("UnlockedLevels") < 19 || PlayerPrefs.GetInt("ADSUNLOCK") == 0
            || PlayerPrefs.GetInt("jetPrice_01") != 0 || PlayerPrefs.GetInt("jetPrice_02") != 0
            || PlayerPrefs.GetInt("jetPrice_03") != 0 || PlayerPrefs.GetInt("jetPrice_04") != 0)
            {
                Invoke("InAppPanel", 0.5f);
            }
            
        }
        inappCounterVar++;
       
        //iconAd.SetActive(true);
    }

    void Start()
	{ 
        if (PlayerPrefs.GetInt("ADSUNLOCK") == 0)
        {
            //McFairyAdsMediation.Instance.HideBanner();
            // McFairyAdsMediation.Instance.ShowBanner(0);
        }
       


        if (PlayerPrefs.GetInt("FirstTime") != 1)
        {
            FirstTime();
        }
        music = GameObject.Find("AudioSource").GetComponent<AudioSource>();

        soundB.fillAmount = PlayerPrefs.GetFloat("Volume");
        musicB.fillAmount = PlayerPrefs.GetFloat("Music");

     

      //  PlayerPrefs.SetInt("TotalScore", 750000);
        //PlayerPrefs.SetInt("jetPrice_01", 0); PlayerPrefs.SetInt("jetPrice_02", 0);

    }

    void FirstTime()
    {
        PlayerPrefs.SetInt("FirstTime", 1);
        PlayerPrefs.SetInt("UnlockedLevels", 0);
        PlayerPrefs.SetFloat("Volume", 1f);
        PlayerPrefs.SetFloat("Music", 1f);
        PlayerPrefs.SetInt("jetPrice_01", 15000); PlayerPrefs.SetInt("jetPrice_02", 25500);
        PlayerPrefs.SetInt("jetPrice_03", 32000); PlayerPrefs.SetInt("jetPrice_04", 45500);
        //PlayerPrefs.SetInt("jetPrice_03", 18000);
        //PlayerPrefs.SetInt("jetPrice_04", 28500); PlayerPrefs.SetInt("jetPrice_05", 35000);
    }
    void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			GameObject.Find ("ClickSound").GetComponent<AudioSource> ().Play ();
			settingsPanel.SetActive (false);
			quitPanel.SetActive (true);
		}

        AudioListener.volume = soundB.fillAmount;
        music.volume = musicB.fillAmount;
        PlayerPrefs.SetFloat("Volume", soundB.fillAmount);
        PlayerPrefs.SetFloat("Music", musicB.fillAmount);

        TotalEarning.text = PlayerPrefs.GetInt ("TotalScore").ToString () + "$";
	}
	
	public void Play()
	{
        EnableRespectivePanel.menuPanelNum = 2;
        GameObject.Find ("ClickSound").GetComponent<AudioSource> ().Play ();
        gameObject.SetActive(false);
        GetComponent<Main_Menu>().enabled = false;
        SelectionPanel.SetActive(true);
        SelectionPanel.GetComponent<Garrage>().enabled = true;
    }
    
    public void More()
	{
		GameObject.Find ("ClickSound").GetComponent<AudioSource> ().Play ();
        Application.OpenURL("https://play.google.com/store/apps/dev?id=5231112994305838965");
    }

    public void Privacy()
    {
        GameObject.Find("ClickSound").GetComponent<AudioSource>().Play();
        Application.OpenURL("https://doc-hosting.flycricket.io/sumo-robots-fight-wresling-3d-privacy-policy/a4309460-c91c-4296-b51d-8ffa1a3e9eef/privacy");
    }

    public void RateUs()
    {
        GameObject.Find("ClickSound").GetComponent<AudioSource>().Play();
        Application.OpenURL("https://play.google.com/store/apps/details?id=com.developmax.SumoRobotsFighter3D");
    }

    public void Exit()
	{
		GameObject.Find ("ClickSound").GetComponent<AudioSource> ().Play ();
		quitPanel.SetActive (true);
	}
	public void Quit_Yes()
	{
        GameObject.Find ("ClickSound").GetComponent<AudioSource> ().Play ();
		Application.Quit();
	}
	public void Quit_No()
	{
        GameObject.Find("ClickSound").GetComponent<AudioSource>().Play();
        quitPanel.SetActive (false);
	}

	public void Settings()
	{
		GameObject.Find ("ClickSound").GetComponent<AudioSource> ().Play ();
		settingsPanel.SetActive (true);
	}
	public void BackFromSettings()
	{
		GameObject.Find ("ClickSound").GetComponent<AudioSource> ().Play ();
		settingsPanel.SetActive (false);
	}

    void InAppPanel()
    {
       // inappPanel.SetActive(true);
      
    }
    public void BackFromInApp()
    {
        GameObject.Find("ClickSound").GetComponent<AudioSource>().Play();
        inappPanel.SetActive(false);

    }

    public void Add10K()
    {
         PlayerPrefs.SetInt("TotalScore", PlayerPrefs.GetInt("TotalScore") + 10000);
    }
    public void IncSound()
    {
        GameObject.Find("ClickSound").GetComponent<AudioSource>().Play();
        if (soundB.fillAmount < 1)
            soundB.fillAmount += 0.2f;
    }
    public void DecSound()
    {
        GameObject.Find("ClickSound").GetComponent<AudioSource>().Play();
        if (soundB.fillAmount > 0)
            soundB.fillAmount -= 0.2f;
    }

    public void IncMusic()
    {
        GameObject.Find("ClickSound").GetComponent<AudioSource>().Play();
        if (musicB.fillAmount < 1)
            musicB.fillAmount += 0.2f;
    }
    public void DecMusic()
    {
        GameObject.Find("ClickSound").GetComponent<AudioSource>().Play();
        if (musicB.fillAmount > 0)
            musicB.fillAmount -= 0.2f;
    }

}
