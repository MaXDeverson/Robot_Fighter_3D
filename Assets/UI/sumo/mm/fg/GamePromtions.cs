using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GamePromtions : MonoBehaviour
{
    public Image icon_place;
    public Text name_txt;
    public Sprite[] icons;
    public string[] names;
    public string[] Links;
    private string current_link;
    private void OnEnable()
    {
        if (PlayerPrefs.GetInt("ADSUNLOCK") == 0)
        {
            icon_place.sprite = icons[0];
            name_txt.text = names[0];
            current_link = Links[0];
            StartCoroutine(change_links());
        }
        else
        {
            this.gameObject.SetActive(false);
        }
        
    }
    IEnumerator change_links()
    {
        yield return new WaitForSecondsRealtime(1.8f);
        int temp = Random.Range(0, icons.Length);
       
        icon_place.sprite = icons[temp];
        name_txt.text = names[temp];
        current_link = Links[temp];
        StartCoroutine(change_links());
    }
   

    public void links_btn_clicked()
    {
        Application.OpenURL(current_link);
    }
}
