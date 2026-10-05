using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PaperCollectZone : Zone
{
    public GameObject paperPrefab;
    private float spawnInterval = 0.2f;
    private float collectInterval = 0.1f;
    private int maxGeneratedPapers = 30;

    private int currentGeneratedPapers;
    public Transform[] paperSpawnPoints; // an array of 3 predefined paper spawn points
    public float paperStackSpacing = 0.001f; // the amount of spacing between the stacked papers
    private int[] currentTowerPapers = new int[10]; // an array to keep track of the number of papers in each tower
    private Stack<GameObject> paperStack = new Stack<GameObject>();
    public float yOffset = 0.2f;
    private Coroutine collectPaper;
    private Coroutine spawnPapers;


    // Awake (not Start) so the tower counters exist before the first paper can be spawned.
    private void Awake()
    {
        int towers = paperSpawnPoints != null ? paperSpawnPoints.Length : 0;
        currentTowerPapers = new int[Mathf.Max(1, towers)];
    }

    private void OnEnable()
    {
        SpawnManager.onInstantiatingPools += StartSpawnPapers;
        // The pools may already be ready if this zone is enabled after SpawnManager.Start.
        if (SpawnManager.PoolsReady)
            StartSpawnPapers();
    }

    private void OnDisable()
    {
        SpawnManager.onInstantiatingPools -= StartSpawnPapers;

        // Disabling only the component does not stop its coroutines, so stop them and clear the handles
        // to make sure the zone can start cleanly again when it is re-enabled.
        StopAllCoroutines();
        spawnPapers = null;
        collectPaper = null;
    }

    private void StartSpawnPapers()
    {
        if (spawnPapers == null)
            spawnPapers = StartCoroutine(SpawnPapers());
    }

    private IEnumerator SpawnPapers()
    {
        if (paperSpawnPoints == null || paperSpawnPoints.Length == 0)
        {
            Debug.LogError("PaperCollectZone has no paper spawn points assigned.", this);
            spawnPapers = null;
            yield break;
        }

        var wait = new WaitForSeconds(spawnInterval);
        while (true)
        {
            currentGeneratedPapers = paperStack.Count;
            if (currentGeneratedPapers < maxGeneratedPapers)
            {
                // Instantiate a new paper with the generated attributes
                int spawnIndex = currentGeneratedPapers % paperSpawnPoints.Length; // use modulo to cycle through the spawn points
                Transform spawnPoint = paperSpawnPoints[spawnIndex];
                GameObject newPaper = spawnPoint != null ? PoolManager.Instance.GetObjectFromPool(ObjectPoolTypes.PAPER) : null;
                if (newPaper != null)
                {
                    Vector3 spawnPosition = spawnPoint.position; // get the spawn position from the chosen spawn point
                    int towerIndex = spawnIndex; // use the spawn index as the tower index
                    spawnPosition.y = paperStackSpacing * currentTowerPapers[towerIndex] + yOffset; // stack the papers on top of each other with the given spacing
                    newPaper.transform.position = spawnPosition;
                    newPaper.transform.rotation = Quaternion.identity;
                    newPaper.transform.SetParent(transform);
                    paperStack.Push(newPaper);
                    currentTowerPapers[towerIndex]++; // increment the number of papers in the current tower
                }
            }
            // Wait for the spawn interval before spawning the next paper
            yield return wait;
        }
    }

    public override void PerformAction(PlayerManager playerManager)
    {
        base.PerformAction(playerManager);

        if (collectPaper == null)
            collectPaper = StartCoroutine(CollectPaper(playerManager));
    }



    private IEnumerator CollectPaper(PlayerManager playerManager)
    {
        var wait = new WaitForSeconds(collectInterval);
        while (true)
        {
            yield return wait;

            // A failure while collecting one paper must never end this coroutine, otherwise the zone
            // stays marked as "collecting" and the player is stuck until it is re-entered.
            try
            {
                TryCollectOnePaper(playerManager);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e, this);
            }
        }
    }

    private void TryCollectOnePaper(PlayerManager playerManager)
    {
        if (paperStack.Count == 0) return;

        playerManager.CollectPaper(1, (canCollect) =>
        {
            if (!canCollect) return;

            GameObject paper = paperStack.Peek();
            if (paper == null)
            {
                // a destroyed paper can never be collected, drop it so it does not block the stack
                paperStack.Pop();
                ReleaseTowerSlot();
                return;
            }

            // The paper only leaves this zone when the player actually accepted it.
            if (!playerManager.TryStackPaper(paper)) return;

            paperStack.Pop();
            ReleaseTowerSlot();
        });
    }

    /// <summary>
    /// Frees the tower slot of the paper that was just removed. Papers are spawned with
    /// tower = (stack size before the push) % towers and removed in LIFO order, so the tower of the
    /// removed paper is (stack size after the pop) % towers.
    /// </summary>
    private void ReleaseTowerSlot()
    {
        int towerIndex = paperStack.Count % currentTowerPapers.Length;
        if (currentTowerPapers[towerIndex] > 0)
            currentTowerPapers[towerIndex]--;
    }


    public override void StopAction()
    {
        base.StopAction();
        if (collectPaper != null)
        {
            StopCoroutine(collectPaper);
            collectPaper = null;
        }
    }
}
