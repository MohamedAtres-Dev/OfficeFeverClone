using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Data/PlayerData")]

public class PlayerData : ScriptableObject
{
    //Hold Money and any thing relate to data of the player like upgrade stacking limit 
    public int currentPaperStackCount;
    public int maxPaperStackCount;

}
