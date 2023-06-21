using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "OfficeSettings", menuName = "Data/Office Settings")]
public class OfficeSettings : ScriptableObject
{
    [SerializeField] private int minPrice;
    [SerializeField] private int maxPrice;

    /// <summary>
    /// This will use to genrate the office at the first time 
    /// </summary>
    /// <returns></returns>
    public int GetRandomPrice()
    {
        int randomPrice = Random.Range(minPrice, maxPrice + 1);
        randomPrice = randomPrice / 100 * 100; // Round down to nearest hundred
        return randomPrice;
    }
}
