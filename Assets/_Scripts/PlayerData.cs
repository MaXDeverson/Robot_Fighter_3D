using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerData
{
    public int AllCash { get => PlayerPrefs.GetInt("TotalScore"); private set => PlayerPrefs.SetInt("TotalScore", value); }
    public int CurrentCash { get => _currentLevelCash; }
    private Action<int,int> _currentCashUpdate;
    private static PlayerData _currentPlayerData;
  
    private int _currentLevelCash;
    private PlayerData()
    {
        Debug.Log("Player Data Init");
    }

    public static PlayerData GetPlayerData()
    {
        if (_currentPlayerData == null)
        {
            _currentPlayerData = new PlayerData();
        }
        return _currentPlayerData;
    }
    public void DoubleCash()
    {
        AllCash += _currentLevelCash;
        _currentLevelCash *= 2;
        _currentCashUpdate.Invoke(AllCash, _currentLevelCash);
    }

    public void AddCash(int cashCount)
    {
        AllCash += cashCount;
        _currentLevelCash += cashCount;
        _currentCashUpdate.Invoke(AllCash,_currentLevelCash);
        Debug.Log("Add Cash");
    }

    public void SetActionCashUpdate(Action<int,int> action)
    {
        Debug.Log("Set Action");
        _currentCashUpdate += action;
    }
    public void RemoveActionCashUpdate(Action<int,int> action)
    {
        Debug.Log("Remove acton");
        _currentCashUpdate -= action;
    }
}
