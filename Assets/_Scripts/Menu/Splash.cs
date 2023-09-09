using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Splash : MonoBehaviour
{
    public float time = 2f;
    public GameObject privacyPanel;
    
    void Awake()
    {
        //PlayerPrefs.DeleteAll();
        Invoke("EnablePrivacy", 0.2f);
        //EnablePrivacy();
        //privacyPanel.GetComponent<Animator>().SetBool("enablePanel", true);
        
    }
    private void Start()
    {
        
    }
    void LoadMenu()
    {
        SceneManager.LoadScene("Menu");
    }

    public void EnablePrivacy()
    {
        privacyPanel.GetComponent<Animator>().SetBool("enablePanel", true);
        //privacyPanel.SetActive(true);
    }

    public void Close()
    {
        //privacyPanel.SetActive(false);
        //privacyPanel.GetComponent<Animator>().enabled = true;
        privacyPanel.GetComponent<Animator>().SetBool("disablePanel", true);
        GameObject.Find("ClickSound").GetComponent<AudioSource>().Play();
        Invoke("LoadMenu", time);
    }

}
