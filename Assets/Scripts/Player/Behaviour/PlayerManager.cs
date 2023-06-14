using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;


public class PlayerManager : MonoBehaviour
{
    public PlayerData playerData;
    public float attractionStrength = 10f;
    public float attractionDistance = 5f;
    public float attractionDuration = 0.01f;

    public void CollectPaper(int collectedPaper, Action<bool> callback)
    {
        if (playerData.currentPaperStackCount >= playerData.maxPaperStackCount)
        {
            callback.Invoke(false);
            return;
        }
        callback.Invoke(true);
        playerData.currentPaperStackCount++;
    }

    public void CollectMoney(GameObject moneyObject, Action callback)
    {
        // calculate the attraction force based on the distance and strength
        float distance = Vector3.Distance(transform.position, moneyObject.transform.position);
        float attractionForce = attractionStrength / distance;

        // use DOTween to move the money object to the player
        moneyObject.transform.DOMove(transform.position, attractionDuration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                // update the coins when the collection is finished
                CurrencyManager.Instance.UpdateCoins(playerData.moneyIncreaseRate);

                // destroy the money object after collecting it
                callback.Invoke();
            });
    }


    public void TransferPaper(Action<bool> callback)
    {
        if (playerData.currentPaperStackCount <= 0)
        {
            callback.Invoke(false);
            return;
        }
        callback.Invoke(true);
        playerData.currentPaperStackCount--;
    }
}
