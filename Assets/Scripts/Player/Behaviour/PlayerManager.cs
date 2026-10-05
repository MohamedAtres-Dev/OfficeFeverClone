using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;


public class PlayerManager : MonoBehaviour
{
    public PlayerData playerData;
    public float attractionStrength = 10f;
    public float attractionDistance = 5f;
    public float attractionDuration = 0.01f;
    public Transform holdPaperPoint;

    [Header("Sound")]
    [SerializeField] private AudioClip coinSound;
    [SerializeField] private AudioClip paperSound;


    private bool isPaperHanging;
    private bool stackRestored;
    public static UnityAction<bool> onHangingPaper = delegate { };

    /// <summary>
    /// The single source of truth for the carried papers (top of the stack = Peek).
    /// A paper is pushed the moment it is accepted, even while it is still flying to the hold point.
    /// playerData.currentPaperStackCount is only a mirror of this and is written in SyncPaperCount().
    /// </summary>
    private readonly Stack<GameObject> paperStack = new Stack<GameObject>();

    [Space]
    public GameObject maxStackText;
    private void OnEnable()
    {
        SpawnManager.onInstantiatingPools += GeneratePaperStacking;
        // The pools may already be ready if this component is enabled after SpawnManager.Start.
        if (SpawnManager.PoolsReady)
            GeneratePaperStacking();
    }

    private void OnDisable()
    {
        SpawnManager.onInstantiatingPools -= GeneratePaperStacking;
    }


    /// <summary>
    /// Rebuilds the carried stack from the persisted count. The stored value is validated first
    /// (it is a ScriptableObject, so it can be stale or out of range), and the real stack is rebuilt
    /// paper by paper so the count and the stack can never disagree.
    /// </summary>
    private void GeneratePaperStacking()
    {
        if (stackRestored) return;
        stackRestored = true;

        int max = Mathf.Max(0, playerData.maxPaperStackCount);
        int stored = playerData.currentPaperStackCount;
        int target = Mathf.Clamp(stored, 0, max);
        if (target != stored)
            Debug.LogWarning($"PlayerData.currentPaperStackCount ({stored}) was out of range, using {target}.");

        SyncPaperCount(); // mirror starts from the real (empty) stack

        for (int i = 0; i < target; i++)
        {
            GameObject paper = PoolManager.Instance.GetObjectFromPool(ObjectPoolTypes.PAPER);
            if (paper == null)
            {
                Debug.LogWarning("Paper pool returned no paper while restoring the carried stack.");
                break;
            }
            if (!TryStackPaper(paper))
            {
                PoolManager.Instance.ReturnObjectToPool(ObjectPoolTypes.PAPER, paper);
                break;
            }
        }
    }

    private void Update()
    {
        bool shouldHangPaper = paperStack.Count > 0 && !isPaperHanging;
        bool shouldUnhangPaper = paperStack.Count == 0 && isPaperHanging;

        if (shouldHangPaper)
        {
            onHangingPaper.Invoke(true);
            isPaperHanging = true;
        }
        else if (shouldUnhangPaper)
        {
            onHangingPaper.Invoke(false);
            isPaperHanging = false;
        }
    }

    /// <summary>
    /// Writes the real stack size into the PlayerData mirror and hides the max-stack text once there is room again.
    /// This is the only place that writes playerData.currentPaperStackCount.
    /// </summary>
    private void SyncPaperCount()
    {
        int count = paperStack.Count;
        playerData.currentPaperStackCount = count;

        if (maxStackText != null && count < playerData.maxPaperStackCount && maxStackText.activeSelf)
            maxStackText.SetActive(false);
    }


    /// <summary>
    /// Only answers "is there room?". It no longer changes any count; the count changes when the paper is actually stacked.
    /// </summary>
    public void CollectPaper(int collectedPaper, Action<bool> callback)
    {
        if (paperStack.Count >= playerData.maxPaperStackCount)
        {
            if (maxStackText != null && !maxStackText.activeSelf)
                maxStackText.SetActive(true);
            callback?.Invoke(false);
            return;
        }
        callback?.Invoke(true);
    }

    public void CollectMoney(GameObject moneyObject, Action callback)
    {
        // calculate the attraction force based on the distance and strength
        float distance = Vector3.Distance(transform.position, moneyObject.transform.position);
        float attractionForce = attractionStrength / distance;

        // use DOTween to move the money object to the player
        moneyObject.transform.DOMove(transform.position, attractionDuration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                // update the coins when the collection is finished
                CurrencyManager.Instance.UpdateCoins(playerData.moneyIncreaseRate);
                AudioManager.Instance.PlaySFX(coinSound);
                // destroy the money object after collecting it
                callback.Invoke();
            });
    }


    /// <summary>
    /// Only answers "is there a paper to send?". Nothing is decremented here; the paper leaves the
    /// stack in UnStackingPaper, so a transfer can never remove a count without removing a paper.
    /// </summary>
    public void TransferPaper(Action<bool> callback)
    {
        if (paperStack.Count == 0)
        {
            callback?.Invoke(false);
            return;
        }

        callback?.Invoke(true);
    }


    /// <summary>
    /// Kept for compatibility. Callers that must not lose the paper on rejection should use TryStackPaper.
    /// </summary>
    public void StackingPaper(GameObject paper)
    {
        TryStackPaper(paper);
    }

    /// <summary>
    /// Adds the paper to the carried stack immediately (count and stack change together), then animates it
    /// to its slot. Returns false, without touching the paper, when it is null or the stack is full.
    /// </summary>
    public bool TryStackPaper(GameObject paper)
    {
        if (paper == null) return false;

        if (paperStack.Count >= playerData.maxPaperStackCount)
        {
            if (maxStackText != null && !maxStackText.activeSelf)
                maxStackText.SetActive(true);
            return false;
        }

        // The slot is fixed now, so the final position never depends on the order the tweens finish in.
        int slot = paperStack.Count;
        float paperHeight = paper.transform.lossyScale.y;
        GameObject paperBelow = slot > 0 ? paperStack.Peek() : null;

        paperStack.Push(paper);
        SyncPaperCount();

        AudioManager.Instance.PlaySFX(paperSound);
        paper.transform.DOKill();
        paper.transform.DOMove(holdPaperPoint.position, 0.3f).OnComplete(() =>
        {
            if (paper == null) return;

            paper.transform.SetParent(holdPaperPoint);
            paper.transform.position = holdPaperPoint.position + Vector3.up * (paperHeight * slot);

            // keep the rotation of the new paper aligned with the one below it
            if (paperBelow != null)
                paper.transform.rotation = Quaternion.AngleAxis(paperBelow.transform.eulerAngles.y, Vector3.up);
        });

        return true;
    }

    public void UnStackingPaper(Action<GameObject> onPaperUnstack)
    {
        // skip any paper that was destroyed externally
        GameObject topPaper = null;
        while (topPaper == null && paperStack.Count > 0)
            topPaper = paperStack.Pop();

        SyncPaperCount();

        if (topPaper == null) return;

        // the paper may still be flying to the hold point; its arrival must not re-parent it to the player
        topPaper.transform.DOKill();
        AudioManager.Instance.PlaySFX(paperSound);
        onPaperUnstack?.Invoke(topPaper);
    }
}
