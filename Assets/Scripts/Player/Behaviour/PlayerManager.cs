using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;


public class PlayerManager : MonoBehaviour
{
    public PlayerData playerData;
    public float attractionStrength = 10f;
    public float attractionDistance = 5f;
    public float attractionDuration = 0.01f;
    public Transform holdPaperPoint;

    [Header("Sound")]
    [SerializeField] private AudioClip coinSound;
    private bool isPaperHanging;
    public static UnityAction<bool> onHangingPaper = delegate { };
    private Stack<GameObject> paperStack = new Stack<GameObject>();

    [Space]
    public GameObject maxStackText;

    private void Update()
    {
        bool shouldHangPaper = playerData.currentPaperStackCount > 0 && !isPaperHanging;
        bool shouldUnhangPaper = playerData.currentPaperStackCount == 0 && isPaperHanging;

        if (shouldHangPaper)
        {
            onHangingPaper.Invoke(true);
            isPaperHanging = true;
        }
        else if (shouldUnhangPaper)
        {
            onHangingPaper.Invoke(false);
            isPaperHanging = false;
        }
    }


    public void CollectPaper(int collectedPaper, Action<bool> callback)
    {
        if (playerData.currentPaperStackCount >= playerData.maxPaperStackCount)
        {
            if (!maxStackText.activeSelf)
                maxStackText.SetActive(true);
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
                AudioManager.Instance.PlaySFX(coinSound);
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

        if (maxStackText.activeSelf)
            maxStackText.SetActive(false);

        callback.Invoke(true);
        playerData.currentPaperStackCount--;
    }


    public void StackingPaper(GameObject paper)
    {
        if (playerData.currentPaperStackCount >= playerData.maxPaperStackCount)
        {
            Debug.Log("Max Stack ");
            return;
        }

        paper.transform.position = holdPaperPoint.position;

        paper.transform.SetParent(holdPaperPoint);

        if (paperStack.Count > 0)
        {
            // get the top paper in the stack
            GameObject topPaper = paperStack.Peek();

            // calculate the position for the new paper based on the position and size of the top paper
            Vector3 newPosition = topPaper.transform.position + new Vector3(0f, topPaper.transform.lossyScale.y, 0f);

            // set the position of the new paper and add it to the stack
            paper.transform.position = newPosition;

            // keep the rotation of the new paper constant
            paper.transform.rotation = Quaternion.AngleAxis(topPaper.transform.eulerAngles.y, Vector3.up);

            paperStack.Push(paper);
        }
        else
        {
            // if the stack is empty, just add the paper at the player holder point
            paperStack.Push(paper);
        }
    }

    public void UnStackingPaper(Action<GameObject> onPaperUnstack)
    {
        if (paperStack.Count > 0)
        {
            // get the top paper in the stack
            GameObject topPaper = paperStack.Pop();
            onPaperUnstack?.Invoke(topPaper);
        }
    }
}
