using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;


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

    public void GenerateInitialPapers(int paperCount)
    {
        for (int i = 0; i < paperCount; i++)
        {
            GameObject newPaper = PoolManager.Instance.GetObjectFromPool(ObjectPoolTypes.PAPER);
            if (newPaper != null)
            {
                newPaper.transform.position = paperSendPoint.position;
                newPaper.transform.rotation = Quaternion.identity;
                newPaper.transform.SetParent(paperSendPoint);
                if (paperStack.Count > 0)
                {
                    // Stack the new paper on top of the previous paper in the stack
                    Vector3 previousPosition = paperStack.Peek().transform.position;
                    newPaper.transform.position = previousPosition + new Vector3(0f, paperStackSpacing, 0f);
                }
                paperStack.Push(newPaper);
                onGetPaper.Invoke();
            }
        }
    }

    private void OnWorkerProceedWork()
    {
        if (paperStack.Count > 0)
        {
            // Dequeue the oldest paper from the queue and return it
            GameObject oldPaper = paperStack.Pop();
            PoolManager.Instance.ReturnObjectToPool(ObjectPoolTypes.PAPER, oldPaper);
        }
    }

    public override void PerformAction(PlayerManager playerManager)
    {
        base.PerformAction(playerManager);
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
                    playerManager.UnStackingPaper((newPaper) => {

                        if (newPaper != null)
                        {
                            newPaper.transform.DOMove(paperSendPoint.position, 0.3f).OnComplete(() =>
                            {
                                newPaper.transform.rotation = Quaternion.identity;
                                newPaper.transform.SetParent(paperSendPoint);
                                if (paperStack.Count > 0)
                                {
                                    // Stack the new paper on top of the previous paper in the stack
                                    Vector3 previousPosition = paperStack.Peek().transform.position;
                                    newPaper.transform.position = previousPosition + new Vector3(0f, paperStackSpacing, 0f);
                                }
                                paperStack.Push(newPaper);
                                onGetPaper.Invoke();
                            });                        
                        }
                    });
                }

            });
        }
    }


    public override void StopAction()
    {
        base.StopAction();
        if (sendPaper != null)
        {
            StopCoroutine(sendPaper);
            sendPaper = null;
        }
    }
}
