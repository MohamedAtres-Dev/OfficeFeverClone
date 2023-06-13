using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public PlayerData playerData;


    public void CollectPaper(int collectedPaper , Action<bool> callback)
    {
        if (playerData.currentPaperStackCount >= playerData.maxPaperStackCount)
        {
            callback.Invoke(false);
            return;
        }
        callback.Invoke(true);
        playerData.currentPaperStackCount++;
    }

    public void CollectMoney()
    {

    }

    public void CreateOffice()
    {
        Debug.Log("Office Created ");
    }

    public void TransferPaper()
    {

    }
}
