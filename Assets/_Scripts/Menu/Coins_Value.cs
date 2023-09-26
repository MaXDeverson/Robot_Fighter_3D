using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Coins_Value : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private Text coins_text;
    private void OnEnable()
    {
        coins_text.text = PlayerData.GetPlayerData().AllCash + "$";
    }
}
