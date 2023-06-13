using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// this script will be responsible for control the flow of the currency of the game
/// currently we have one type but we can handle many types on currencies here
/// </summary>
public class CurrencyManager : Singlton<CurrencyManager>
{
    [SerializeField] private int totalCoins;
    private const string coinsKey = "TotalCoins";
    public static UnityAction<int> onUpdateCoins = delegate { };

    public int TotalCoins
    {
        get => totalCoins;
        set
        {
            totalCoins = value;
            onUpdateCoins.Invoke(totalCoins);
        }
    }

    protected override void Awake()
    {
        base.Awake();
        TotalCoins = PlayerPrefs.GetInt(coinsKey, 45);
    }

    /// <summary>
    /// Get The total coins 
    /// </summary>
    /// <returns></returns>
    public int GetCoins()
    {
        return TotalCoins;
    }

    /// <summary>
    /// Update the coins even in positive or negative way
    /// </summary>
    /// <param name="value"></param>
    public void UpdateCoins(int value)
    {
        TotalCoins += value;
    }

    public void ResetCoins()
    {
        TotalCoins = 0;
    }

    private void OnDisable()
    {
        PlayerPrefs.SetInt(coinsKey, totalCoins);
    }

}
