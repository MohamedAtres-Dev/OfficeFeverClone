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

    public Transform[] paperSpawnPoints; // an array of 3 predefined paper spawn points
    public float paperStackSpacing = 0.001f; // the amount of spacing between the stacked papers
    private int[] currentTowerPapers = new int[10]; // an array to keep track of the number of papers in each tower
    public float yOffset = 0.2f;
    private Stack<GameObject> paperStack = new Stack<GameObject>();

    private void Start()
    {
        currentTowerPapers = new int[paperSpawnPoints.Length];
    }
    public void GenerateInitialMoney(int count)
    {
        for (int i = 0; i < count; i++)
        {
            OnWorkerProceedWork();
        }
    }

    private void Replace()
    {

        // Instantiate a new paper with the generated attributes
        int spawnIndex = moneyQueue.Count % paperSpawnPoints.Length; // use modulo to cycle through the spawn points
        Vector3 spawnPosition = paperSpawnPoints[spawnIndex].position; // get the spawn position from the chosen spawn point
        int towerIndex = spawnIndex; // use the spawn index as the tower index
        spawnPosition.y = paperStackSpacing * currentTowerPapers[towerIndex] + yOffset; // stack the papers on top of each other with the given spacing
        GameObject newPaper = PoolManager.Instance.GetObjectFromPool(ObjectPoolTypes.MONEY);
        newPaper.transform.position = spawnPosition;
        newPaper.transform.rotation = Quaternion.identity;
        newPaper.transform.SetParent(transform);
        paperStack.Push(newPaper);
        currentTowerPapers[towerIndex]++; // increment the number of papers in the current tower

    }

    public void OnWorkerProceedWork(OfficeWorker worker)
    {
        if (worker.GetpaperCount() <= 0) return;
        GameObject newMoney = Instantiate(moneyPrefab, moneyGeneratePoint.position, Quaternion.identity);
        newMoney.transform.SetParent(transform);
        moneyQueue.Enqueue(newMoney);
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
