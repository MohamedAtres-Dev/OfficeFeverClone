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

    public void GenerateInitialMoney(int count)
    {
        for (int i = 0; i < count; i++)
        {
            OnWorkerProceedWork();
        }
    }

    private void OnWorkerProceedWork()
    {
        GameObject newMoney = Instantiate(moneyPrefab, moneyGeneratePoint.position, Quaternion.identity);
        newMoney.transform.SetParent(transform);
        moneyQueue.Enqueue(newMoney);
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

    public int GetMoneyGeneratedCount()
    {
        return moneyQueue.Count;
    }
}
