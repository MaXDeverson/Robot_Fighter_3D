using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Ads;
using Zenject;

public class EnableRespectivePanel : MonoBehaviour {

    [Inject] private IAdsManagerService _ads;

    public static int menuPanelNum;

    public GameObject MenuPanel, GunSelectionPanel, MissionSelectionPanel;
   
    void Awake()
    {
        Time.timeScale = 1f;
    }
    private void Start()
    {
        // no banner in the menu
        _ads.HideBanner();
    }
    private void OnEnable()
    {
        if (menuPanelNum == 1)
        {
            MenuPanel.SetActive(true);
            GunSelectionPanel.SetActive(false);
            MissionSelectionPanel.SetActive(false);
        }
        else if (menuPanelNum == 2)
        {
            MenuPanel.SetActive(false);
            GunSelectionPanel.SetActive(true);
            MissionSelectionPanel.SetActive(false);
        }
        else if (menuPanelNum == 3)
        {
            MenuPanel.SetActive(false);
            GunSelectionPanel.SetActive(false);
            MissionSelectionPanel.SetActive(true);
        }
        else
        {
            MenuPanel.SetActive(true);
            GunSelectionPanel.SetActive(false);
            MissionSelectionPanel.SetActive(false);
        }

    }
}
