
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using System;
using UnityEngine.Purchasing.Extension;

public class Purchaser : MonoBehaviour
{
    [SerializeField] private InAppScr _inApp;

    private void Start()
    {
        //PlayerPrefs.DeleteAll();
        //for (int i = 0; i < 5; i++)
        //{
        //    PlayerPrefs.SetInt("Unlocked" + i, 0);
        //}
    }
    public void OnPurchaseCompleted(Product product)
    {
        Debug.Log("On purchas " + product.definition.id);
        switch (product.definition.id)
        {
            case "adblock":
                PurchaseAdBlock();
                break;
            case "characters":
                BuyAllCharacters();
                break;
            case "levels":
                UnlockLevels();
                break;
            case "all":
                All();
                break;
        }
        _inApp.OnEnable();
    }
    public void OnPurchaseFailed(Product product,PurchaseFailureReason reason)
    {
       
    }

    private void BuyAllCharacters()
    {
        for(int count = 0; count <= 5; count++)
        {
            PlayerPrefs.SetInt("Unlocked" + count, count);
        }
    }
    private void UnlockLevels()
    {
        PlayerPrefs.SetInt("UnlockedLevels", 20);
    }
    private void PurchaseAdBlock()
    {
        PlayerPrefs.SetInt("ADSUNLOCK", 1);
    }

    private void All()
    {
        PurchaseAdBlock();
        BuyAllCharacters();
        UnlockLevels();
    }
}
