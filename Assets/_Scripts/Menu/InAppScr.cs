using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InAppScr : MonoBehaviour
{
    public GameObject removeAds, unlock_players, unlock_levels, unlock_everything;

    public void OnEnable()
    {
        bool unlockCharacters = true;
        for (int count = 0; count <= 5; count++)
        {
            if(PlayerPrefs.GetInt("Unlocked" + count) != count)
            {
                unlockCharacters = false;
                break;
            }
        }
        unlock_players.GetComponent<Button>().interactable = !unlockCharacters;
        if (PlayerPrefs.GetInt("ADSUNLOCK") != 0)
        {
            removeAds.GetComponent<Button>().interactable = false;
        }
        if (PlayerPrefs.GetInt("UnlockedLevels") >= 19)
        {
            unlock_levels.GetComponent<Button>().interactable = false;
        }
        if (PlayerPrefs.GetInt("UnlockedLevels") >= 19 && PlayerPrefs.GetInt("ADSUNLOCK") != 0 && unlockCharacters)
        {
            unlock_everything.GetComponent<Button>().interactable = false;
        }
    }
    
}
