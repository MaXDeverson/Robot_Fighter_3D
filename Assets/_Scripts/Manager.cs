﻿using Firebase.Analytics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class Manager : MonoBehaviour
{
    [SerializeField] private UIManager _uiManager;
    public GameObject barish;
    public static Manager instance;
    public GameObject iconAd;
    public GameObject HUD, TouchPanel, dia_pause, objective, loadingScreenBG, LevelsParent, btn_pause, btn_okay, completeSound, failSound,weaponChoise;
    public Text objText, scoreText;

    public AudioSource tictic;
    public AudioClip tic;
    public float letterPause;
    string message;
    Text textComp;

    public float health, totalHealth;
    public int[] healthArray;
    public Material[] skyboxArr;
    public GameObject[] levels, environments;
    public int[] scoreArray;
    public string[] objTextArray;

    public int score;
    AsyncOperation ao;
    [Header("Dialog Window")]
    [SerializeField] private Transform _dialogWindow;
    [SerializeField] private Text _dialogWindowText;
    [Header("Settings")]
    [SerializeField] private bool _spawnLevel = true;
    [SerializeField] private bool _spawnEnviroment = false;
    [SerializeField] private int _currentLevelIndex;
    private static int _choiseCounter;
    void Awake()
    {
        if(_currentLevelIndex>=0)General.CurrentLevel = _currentLevelIndex;
        instance = this;
        Time.timeScale = 0f;
        if (_spawnLevel)
        {
            if (levels.Length == 0) return;
            RenderSettings.skybox = skyboxArr[General.CurrentLevel];
            if (_spawnEnviroment)
            {
               GameObject env = Instantiate(environments[General.CurrentLevel], environments[General.CurrentLevel].transform.position, environments[General.CurrentLevel].transform.rotation);
               if(env.transform.GetChild(0).TryGetComponent<Openable>(out Openable openable))
                {
                    levels[General.CurrentLevel].transform.GetChild(0).GetComponent<EnemyWaveSystem>().SetFinishOpenable(openable);
                }
            }

            levels = new GameObject[LevelsParent.transform.childCount];
            for (int i = 0; i < LevelsParent.transform.childCount; i++)
            {
                levels[i] = LevelsParent.transform.GetChild(i).gameObject;
            }
            for(int i = 0;i < levels.Length; i++)
            {
                levels[i].gameObject.SetActive(i == General.CurrentLevel);
            }
            levels[General.CurrentLevel].SetActive(true);
            levels[General.CurrentLevel].transform.GetChild(0).GetComponent<EnemyWaveSystem>().Init();
        }
        barish.SetActive(General.CurrentLevel % 2 != 0);
        health = totalHealth = healthArray[General.CurrentLevel];
        score = scoreArray[General.CurrentLevel];
        objText.text = objTextArray[General.CurrentLevel];


    }

    void Start()
    {
        if(General.CurrentLevel > 0)
        {
            MyMobileAds.LoadBannerAd();
        }
        HUD.SetActive(true);
        TouchPanel.SetActive(true);

        AudioListener.pause = false;
        loadingScreenBG.SetActive(false);

        textComp = objText.GetComponent<Text>();
        message = textComp.text;
        textComp.text = "";
        tictic = GetComponent<AudioSource>();
        btn_okay.GetComponent<Button>().interactable = false;
        if(_choiseCounter++ % 2 == 0)
        {
            ShowObjective();
        }
        else
        {
            ShowWeaponChoice();
        }
        Debug.Log("Current Level:" + General.CurrentLevel);
    }

    void MissionCompleteSound()
    {
        Instantiate(completeSound, completeSound.transform.position, completeSound.transform.rotation);
    }
    void MissionFailSound()
    {
        Instantiate(failSound, failSound.transform.position, failSound.transform.rotation);
    }

    public void ShowDialogWindow(string text)
    {
        _dialogWindow.gameObject.SetActive(true);
        _dialogWindowText.text = text;
    }

    public void HideDialogWindow()
    {
        _dialogWindow.gameObject.SetActive(false);
    }

    public void PauseFunction()
    {
        Time.timeScale = 0;
        dia_pause.SetActive(true);

        print("LEVEL PAUSED...");

        if (PlayerPrefs.GetInt("ADSUNLOCK") == 0)
        {
            //UnityAdsManager.Instance.ShowUnityVideoAd();
            //MobileAds.Instance.Show_InterstitialAd();
            //  McFairyAdsMediation.Instance.ShowInterstitialAd(1);

        }
    }
    void ShowObjective()
    {
        StartCoroutine(TypeText());
        objective.SetActive(true);
    }

    void ShowWeaponChoice()
    {
        weaponChoise.SetActive(true);
    }

    IEnumerator TypeText()
    {
        foreach (char letter in message.ToCharArray())
        {
            textComp.text += letter;
            tictic.PlayOneShot(tic);
            yield return new WaitForSeconds(letterPause);
        }
        btn_okay.GetComponent<Button>().interactable = true;
        //Time.timeScale = 0;
    }

    public void Btn_MainMenu()
    {
        //MobileAds.Instance.Show_InterstitialAd();
        EnableRespectivePanel.menuPanelNum = 1;
        Time.timeScale = 1f;
        StartCoroutine(AsynchronousLoad("Menu"));
        loadingScreenBG.SetActive(true);
        AudioListener.pause = false;

    }

    public void Btn_Paused()
    {
        MyMobileAds.ShowAd(()=> {
            AudioListener.pause = true;
            GameObject.Find("AudioSource").GetComponent<AudioSource>().enabled = false;

            PauseFunction();
        });

    }

    public void Btn_Restart()
    {
        MyMobileAds.ShowAd(()=>
        {
            Time.timeScale = 1f;
            StartCoroutine(AsynchronousLoad("GamePlay"));
            loadingScreenBG.SetActive(true);
            AudioListener.pause = false;
        });


    }

    public void Btn_Resume()
    {
        MyMobileAds.ShowAd(()=> {
            AudioListener.pause = false;
            GameObject.Find("AudioSource").GetComponent<AudioSource>().enabled = true;
            Time.timeScale = 1;
            dia_pause.SetActive(false);
        });


    }

    public void Btn_Next()
    {
        loadingScreenBG.SetActive(true);
        MyMobileAds.ShowAd(() =>
        {
            Time.timeScale = 1;
            EnableRespectivePanel.menuPanelNum = 3;
            General.CurrentLevel++;
            StartCoroutine(AsynchronousLoad("GamePlay"));
            AudioListener.pause = false;
            PlayerData.GetPlayerData().RessetLevelCash();
        });

    }
    public void DoubleCash()
    {
        if(!MyMobileAds.ShowRewardedSucssesCheck(() => { _uiManager.DisableDoubleCash(); FirebaseAnalytics.LogEvent("double_cash", "double_cash", 0); PlayerData.GetPlayerData().DoubleCash(); }))
        {
            ShowDialogWindow("No Internet connection :(");
        }
    }
    public void HideWeaponChoice()
    {
        Time.timeScale = 1;
        weaponChoise.SetActive(false);
        //GameObject.Find("AudioSource").GetComponent<AudioSource>().enabled = true;
    }

    public void BtnOk()
    {
        Time.timeScale = 1;
        objective.SetActive(false);
        GameObject.Find("AudioSource").GetComponent<AudioSource>().enabled = true;
    }

    public void LoadTheLevel(int a)
    {
        loadingScreenBG.SetActive(true);
        StartCoroutine(LoadLevelWithRealProgressBar(a));
    }

    IEnumerator LoadLevelWithRealProgressBar(int a)
    {
        ao = SceneManager.LoadSceneAsync(a);
        ao.allowSceneActivation = true;
        yield return null;
        //		while (!ao.isDone) {
        //			fillImage.fillAmount = ao.progress;
        //			if (ao.progress == 0.9f) {
        //				fillImage.fillAmount = 1f;
        //			}
        //			yield return null;
        //		}
    }

    IEnumerator AsynchronousLoad(string scene)
    {
        yield return null;

        AsyncOperation ao = SceneManager.LoadSceneAsync(scene);
        ao.allowSceneActivation = false;

        while (!ao.isDone)
        {
            // [0, 0.9] > [0, 1]
            float progress = Mathf.Clamp01(ao.progress / 0.9f);
            //			Debug.Log("Loading progress: " + (progress * 100) + "%");totalWarRobots
            //loadingBar.fillAmount += (ao.progress);

            // Loading completed
            if (ao.progress == 0.9f)
            {
                ao.allowSceneActivation = true;
            }
            yield return null;
        }
    }

}
