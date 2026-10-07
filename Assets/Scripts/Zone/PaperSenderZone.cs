using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;


public class PaperSenderZone : Zone
{
    private float sendInterval = 0.05f;
    private WaitForSeconds sendWait;
    public Stack<GameObject> paperStack = new Stack<GameObject>();
    private Coroutine sendPaper;
    private int currentSendPaper;
    public Transform paperSendPoint;
    public GameObject paperPrefab;
    public float paperStackSpacing = 0.02f; // the amount of spacing between the stacked papers

    public Office currentOffice;


    private void OnEnable()
    {
       
    }

    private void OnDisable()
    {
        
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
                currentOffice.OnGetPaper();
               
            }
        }
    }

    public void OnWorkerProceedWork(OfficeWorker worker)
    {
        if (paperStack.Count > 0)
        {
            // Dequeue the oldest paper from the queue and return it
            GameObject oldPaper = paperStack.Pop();
            ConsumePaper(oldPaper);
        }
    }

    /// <summary>
    /// The worker takes the paper: it shrinks into the desk instead of vanishing. The paper is already off the
    /// stack, so nothing else can touch it; the pool resets its scale and tweens when it goes back.
    /// </summary>
    private void ConsumePaper(GameObject paper)
    {
        if (paper == null) return;

        Transform t = paper.transform;
        t.DOKill();
        t.DOScale(t.localScale * 0.05f, 0.15f).SetEase(Ease.InQuad)
            .SetTarget(t)
            .SetRecyclable(true)
            .SetLink(paper, LinkBehaviour.KillOnDisable)
            .OnComplete(() => PoolManager.Instance.ReturnObjectToPool(ObjectPoolTypes.PAPER, paper));
    }

    public override void PerformAction(PlayerManager playerManager)
    {
        base.PerformAction(playerManager);
        if (sendPaper == null)
            sendPaper = StartCoroutine(GetPapers(playerManager));
    }


    private int papersLaunched;   // monotonic tickets, so papers in flight line up behind the ones already on the desk
    private int papersLanded;
    [SerializeField] private float deliveryFlightTime = 0.3f;
    [SerializeField] private float deliveryArcHeight = 0.35f;

    /// <summary>
    /// Flies a paper from the player's stack to its slot on the desk. The paper leaves the player first (so it
    /// no longer inherits the player's motion), follows a short arc, turns to the desk's alignment while flying
    /// and lands exactly on its slot: no snap on arrival.
    /// </summary>
    private void DeliverPaper(GameObject paper)
    {
        Transform t = paper.transform;
        t.SetParent(null, true);
        Vector3 startPosition = t.position;
        Quaternion startRotation = t.rotation;
        int ticket = papersLaunched++;

        DOVirtual.Float(0f, 1f, Mathf.Max(0.01f, deliveryFlightTime), progress =>
            {
                if (paper == null) return;

                // the slot is re-read each frame: the stack below may shrink while the worker consumes papers
                Vector3 target = GetDeskSlot(paperStack.Count + (ticket - papersLanded));
                Vector3 position = Vector3.LerpUnclamped(startPosition, target, progress);
                position.y += deliveryArcHeight * Mathf.Sin(progress * Mathf.PI);
                t.position = position;
                t.rotation = Quaternion.Slerp(startRotation, Quaternion.identity, progress);
            })
            .SetEase(Ease.InOutSine)
            .SetTarget(t)
            .SetRecyclable(true)
            .SetLink(paper, LinkBehaviour.KillOnDisable)
            .OnComplete(() =>
            {
                t.SetParent(paperSendPoint, true);
                t.rotation = Quaternion.identity;
                t.position = GetDeskSlot(paperStack.Count);
                paperStack.Push(paper);
                currentOffice.OnGetPaper(); // the landing itself is the feedback: no particles on routine deliveries
            })
            .OnKill(() => papersLanded++); // runs on completion and when the paper is pulled away mid-flight
    }

    private Vector3 GetDeskSlot(int index)
    {
        return paperSendPoint.position + Vector3.up * (paperStackSpacing * index);
    }

    private IEnumerator GetPapers(PlayerManager playerManager)
    {
        while (true)
        {
            yield return sendWait ??= new WaitForSeconds(sendInterval);

            playerManager.TransferPaper((canTransfer) =>
            {
                if (canTransfer)
                {
                    playerManager.UnStackingPaper((newPaper) => {

                        if (newPaper != null)
                            DeliverPaper(newPaper);
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
