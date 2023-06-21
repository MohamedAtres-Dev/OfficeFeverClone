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
    

    private void Start()
    {
        currentTowerPapers = new int[paperSpawnPoints.Length];
    }

    private void OnEnable()
    {
        SpawnManager.onInstantiatingPools += StartSpawnPapers;
    }

    private void OnDisable()
    {
        SpawnManager.onInstantiatingPools -= StartSpawnPapers;
    }

    private void StartSpawnPapers()
    {
        StartCoroutine(SpawnPapers());
    }

    private IEnumerator SpawnPapers()
    {

        while (true)
        {
            currentGeneratedPapers = paperStack.Count;
            if (currentGeneratedPapers < maxGeneratedPapers)
            {
                // Instantiate a new paper with the generated attributes
                int spawnIndex = currentGeneratedPapers % paperSpawnPoints.Length; // use modulo to cycle through the spawn points
                Vector3 spawnPosition = paperSpawnPoints[spawnIndex].position; // get the spawn position from the chosen spawn point
                int towerIndex = spawnIndex; // use the spawn index as the tower index
                spawnPosition.y = paperStackSpacing * currentTowerPapers[towerIndex] + yOffset; // stack the papers on top of each other with the given spacing
                GameObject newPaper = PoolManager.Instance.GetObjectFromPool(ObjectPoolTypes.PAPER);
                newPaper.transform.position = spawnPosition;
                newPaper.transform.rotation = Quaternion.identity;
                newPaper.transform.SetParent(transform);
                paperStack.Push(newPaper);
                currentTowerPapers[towerIndex]++; // increment the number of papers in the current tower
            }
            // Wait for the spawn interval before spawning the next paper
            yield return new WaitForSeconds(spawnInterval);
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
        while (true)
        {
            yield return new WaitForSeconds(collectInterval);
            if (paperStack.Count > 0)
            {
                playerManager.CollectPaper(1, (canCollect) =>
                {
                    if (canCollect)
                    {
                        // Pop the top paper from the queue and return it
                        GameObject oldPaper = paperStack.Pop();
                        Vector3 collectedPosition = oldPaper.transform.position;
                        int towerIndex = GetTowerIndex(collectedPosition);
                        playerManager.StackingPaper(oldPaper);

                                            
                        currentTowerPapers[towerIndex]--;
                    }
                });
            }
        }
    }

    private int GetTowerIndex(Vector3 position)
    {
        float distanceThreshold = 0.1f; // adjust this value as needed
        for (int i = 0; i < paperSpawnPoints.Length; i++)
        {
            if (Mathf.Abs(position.x - paperSpawnPoints[i].position.x) < distanceThreshold)
            {
                return i;
            }
        }
        return -1;
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
