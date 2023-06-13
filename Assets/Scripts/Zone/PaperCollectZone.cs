using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PaperCollectZone : Zone
{
    public GameObject paperPrefab;
    private float spawnInterval = 0.2f;
    private float collectInterval = 0.1f;
    private int maxGeneratedPapers = 20;
    private int currentGeneratedPapers;
    public Transform paperSpawnPoint;

    private Queue<GameObject> paperQueue = new Queue<GameObject>();

    private Coroutine collectPaper;

    private void Start()
    {
        StartCoroutine(SpawnPapers());
    }

    private IEnumerator SpawnPapers()
    {
        
        while (true)
        {
            currentGeneratedPapers = paperQueue.Count;
            if (currentGeneratedPapers < maxGeneratedPapers)
            {
                // Instantiate a new paper with the generated attributes
                GameObject newPaper = Instantiate(paperPrefab, paperSpawnPoint.position, Quaternion.identity);
                paperQueue.Enqueue(newPaper);
            } 
            // Wait for the spawn interval before spawning the next paper
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    public override void PerformAction(PlayerManager playerManager)
    {
        base.PerformAction(playerManager);
        Debug.Log("Perform Action on Paper Zone ");

        collectPaper = StartCoroutine(CollectPaper(playerManager));
    }



    private IEnumerator CollectPaper(PlayerManager playerManager)
    {
        while (true)
        {
            yield return new WaitForSeconds(collectInterval);
            if (paperQueue.Count > 0)
            {
                playerManager.CollectPaper(1, (canCollect) =>
                {
                    if (canCollect)
                    {
                        // Dequeue the oldest paper from the queue and return it
                        GameObject oldPaper = paperQueue.Dequeue();
                        Destroy(oldPaper); //replace it with animation transfering it to the player
                    }
                });
               
            }
        }
    }


    public override void StopAction()
    {
        base.StopAction();
        if (collectPaper != null)
            StopCoroutine(collectPaper);
    }
}
