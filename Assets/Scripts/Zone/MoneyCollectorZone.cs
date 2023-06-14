using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoneyCollectorZone : Zone
{
    private float collectInterval = 0.008f;
    public GameObject moneyPrefab;
    public Transform moneyGeneratePoint;

    private Queue<GameObject> moneyQueue = new Queue<GameObject>();
    private Coroutine collectMoneyCoroutine;

    //Here I need to get the money from worker
    private void OnEnable()
    {
        OfficeWorker.onProceedWork += OnWorkerProceedWork;
    }

    private void OnDisable()
    {
        OfficeWorker.onProceedWork -= OnWorkerProceedWork;
    }

    private void OnWorkerProceedWork()
    {
        GameObject newPaper = Instantiate(moneyPrefab, moneyGeneratePoint.position, Quaternion.identity);
        moneyQueue.Enqueue(newPaper);
    }



    public override void PerformAction(PlayerManager playerManager)
    {
        base.PerformAction(playerManager);
        Debug.Log("Collect Money ");
        if (collectMoneyCoroutine == null)
            collectMoneyCoroutine = StartCoroutine(CollectMoney(playerManager));
    }


    private IEnumerator CollectMoney(PlayerManager playerManager)
    {
        while (true)
        {
            yield return new WaitForSeconds(collectInterval);

            if (moneyQueue.Count > 0)
            {
                GameObject newMoney = moneyQueue.Dequeue();
                playerManager.CollectMoney(newMoney, () => { Destroy(newMoney); });
            }
        }
    }

    public override void StopAction()
    {
        base.StopAction();
        if (collectMoneyCoroutine != null)
        {
            StopCoroutine(collectMoneyCoroutine);
            collectMoneyCoroutine = null;
        }
    }
}
