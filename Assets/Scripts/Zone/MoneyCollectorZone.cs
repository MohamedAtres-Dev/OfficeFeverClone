using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class MoneyCollectorZone : Zone
{
    private float collectInterval = 0.04f;
    private float fastCollectInterval = 0.015f;
    private int fastCollectThreshold = 30;
    public GameObject moneyPrefab;
    public Transform moneyGeneratePoint;

    // The visible pile, oldest at the bottom. Bundles are collected from the top so the pile shrinks without holes.
    private Stack<GameObject> moneyQueue = new Stack<GameObject>();

    [Header("Cash Pile Layout")]
    [Tooltip("Bundles per layer along the pile's local X (rows) and Z (columns).")]
    [SerializeField] private int pileRows = 2;
    [SerializeField] private int pileColumns = 2;
    [SerializeField] private float rowSpacing = 0.4f;
    [SerializeField] private float columnSpacing = 0.24f;
    [SerializeField] private float layerHeight = 0.1f;
    [Tooltip("Layers shown before extra bundles share the top layer (keeps a big stash from becoming a tower).")]
    [SerializeField] private int maxVisibleLayers = 6;
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
            SpawnMoney(false); // restored from the save: placed silently, no pop
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
        SpawnMoney(true);
    }

    /// <summary>
    /// Takes a money object from the shared money pool (falling back to the prefab if the pool is missing)
    /// and places it as the next cash bundle of the pile. Animated spawns drop in and pop so the desk visibly
    /// "produces" money; restored ones are placed silently.
    /// </summary>
    private void SpawnMoney(bool animated)
    {
        GameObject newMoney = PoolManager.Instance.GetObjectFromPool(ObjectPoolTypes.MONEY);
        if (newMoney == null)
            newMoney = Instantiate(moneyPrefab);

        Transform t = newMoney.transform;
        t.SetParent(transform, true); // keep the world scale: the zone sits under scaled parents
        GetPileSlot(moneyQueue.Count, out Vector3 slotPosition, out Quaternion slotRotation);
        t.SetPositionAndRotation(slotPosition, slotRotation);
        moneyQueue.Push(newMoney);

        if (!animated) return;

        // The pool reset the scale to the prefab's, so the pop always starts and ends at the right size.
        Vector3 baseScale = t.localScale;
        t.localScale = baseScale * 0.2f;
        t.position = slotPosition + Vector3.up * 0.25f;
        DOTween.Sequence()
            .Join(t.DOScale(baseScale, 0.22f).SetEase(Ease.OutBack))
            .Join(t.DOMove(slotPosition, 0.2f).SetEase(Ease.OutQuad))
            .SetTarget(t)
            .SetLink(newMoney, LinkBehaviour.KillOnDisable)
            .OnKill(() =>
            {
                if (t == null) return;
                t.localScale = baseScale;
                t.position = slotPosition;
            });
    }

    /// <summary>
    /// Slot of the Nth bundle: a compact rows x columns grid per layer, layers stacked upward. Every layer is
    /// shifted and turned a little (deterministically) so the pile reads as hand-stacked cash, not a column of boxes.
    /// </summary>
    private void GetPileSlot(int index, out Vector3 position, out Quaternion rotation)
    {
        int perLayer = Mathf.Max(1, pileRows * pileColumns);
        int layer = Mathf.Min(index / perLayer, Mathf.Max(0, maxVisibleLayers - 1));
        int inLayer = index % perLayer;
        int row = inLayer / Mathf.Max(1, pileColumns);
        int column = inLayer % Mathf.Max(1, pileColumns);

        Quaternion basis = moneyGeneratePoint.rotation;
        float x = (row - (pileRows - 1) * 0.5f) * rowSpacing + ((layer % 2 == 0) ? -0.03f : 0.03f);
        float z = (column - (pileColumns - 1) * 0.5f) * columnSpacing;
        float yaw = (((index * 37) % 7) - 3) * 1.5f; // -4.5 .. +4.5 degrees, stable per slot

        position = moneyGeneratePoint.position + basis * new Vector3(x, layer * layerHeight, z);
        rotation = basis * Quaternion.Euler(0f, yaw, 0f);
    }


    /// <summary>Gives a collected money object back to the pool (or destroys it if it never came from one).</summary>
    private void ReleaseMoney(GameObject money)
    {
        if (money == null) return;

        if (PoolManager.Instance.allObjectPools.ContainsKey(ObjectPoolTypes.MONEY))
            PoolManager.Instance.ReturnObjectToPool(ObjectPoolTypes.MONEY, money);
        else
            Destroy(money);
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
            yield return new WaitForSeconds(moneyQueue.Count > fastCollectThreshold ? fastCollectInterval : collectInterval);

            if (moneyQueue.Count > 0)
            {
                GameObject newMoney = moneyQueue.Pop();
                playerManager.CollectMoney(newMoney, () => ReleaseMoney(newMoney));
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
