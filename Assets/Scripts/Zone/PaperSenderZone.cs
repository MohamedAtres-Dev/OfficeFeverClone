using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public class PaperSenderZone : Zone
{
    private float sendInterval = 0.05f;
    private Stack<GameObject> paperStack = new Stack<GameObject>();
    private Coroutine sendPaper;
    private int currentSendPaper;
    public Transform paperSendPoint;
    public GameObject paperPrefab;
    public float paperStackSpacing = 0.02f; // the amount of spacing between the stacked papers

    //Here I send Paper to worker
    public static UnityAction onGetPaper = delegate { };

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
        if (paperStack.Count > 0)
        {
            // Dequeue the oldest paper from the queue and return it
            GameObject oldPaper = paperStack.Pop();
            Destroy(oldPaper); //replace it with animation transfering it to the player
        }
    }

    public override void PerformAction(PlayerManager playerManager)
    {
        base.PerformAction(playerManager);
        Debug.Log("Send Paper ");
        if (sendPaper == null)
            sendPaper = StartCoroutine(GetPapers(playerManager));
    }


    private IEnumerator GetPapers(PlayerManager playerManager)
    {
        while (true)
        {
            yield return new WaitForSeconds(sendInterval);

            playerManager.TransferPaper((canTransfer) =>
            {
                if (canTransfer)
                {
                    Debug.Log("Transfer Money");
                    GameObject newPaper = Instantiate(paperPrefab, paperSendPoint.position, Quaternion.identity);
                    if (paperStack.Count > 0)
                    {
                        // Stack the new paper on top of the previous paper in the stack
                        Vector3 previousPosition = paperStack.Peek().transform.position;
                        newPaper.transform.position = previousPosition + new Vector3(0f, paperStackSpacing, 0f);
                    }
                    paperStack.Push(newPaper);
                    onGetPaper.Invoke();
                }

            });
        }
    }


    public override void StopAction()
    {
        base.StopAction();
        if (sendPaper != null)
        {
            Debug.Log("Stop Sending Paper ");
            StopCoroutine(sendPaper);
            sendPaper = null;
        }
    }
}
