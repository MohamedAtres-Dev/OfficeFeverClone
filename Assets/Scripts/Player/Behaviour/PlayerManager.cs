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

    [Header("Pickup Feel")]
    [Tooltip("Seconds a paper takes to fly into the stack (unchanged from before).")]
    [SerializeField] private float pickupDuration = 0.3f;
    [Tooltip("How high, in metres, the flight arcs above the straight line.")]
    [SerializeField] private float pickupArcHeight = 0.35f;
    [Tooltip("Scale multiplier of the pop when a paper lands on the stack.")]
    [SerializeField] private float popScale = 1.25f;
    [SerializeField] private float popUpTime = 0.06f;
    [SerializeField] private float popDownTime = 0.14f;
    [Tooltip("The pickup sound's pitch rises by this much from the first to the last paper of a full stack.")]
    [SerializeField] private float pickupPitchRange = 0.35f;


    [Header("VFX")]
    [Tooltip("Played where a paper lands on the carried stack (PaperPickupVFX).")]
    [SerializeField] private PooledVFX paperPickupVFX;
    [Tooltip("Metres above the landed paper's centre where the effect is played.")]
    [SerializeField] private float pickupVFXHeightOffset = 0.05f;

    [Tooltip("Played above the player when money is credited (MoneyRewardVFX).")]
    [SerializeField] private PooledVFX moneyRewardVFX;
    [Tooltip("Metres above the player's pivot where the money effect plays.")]
    [SerializeField] private float moneyVFXHeightOffset = 1.6f;
    [Tooltip("Minimum seconds between money effects. Coins are credited many times per second, so only the first of each burst plays.")]
    [SerializeField] private float moneyVFXInterval = 0.35f;

    [Header("Money Collection")]
    [Tooltip("Seconds a coin takes to fly into the player.")]
    [SerializeField] private float moneyFlightDuration = 0.28f;
    [SerializeField] private float moneyArcHeight = 0.5f;
    [Tooltip("Metres above the player's pivot that coins fly to (roughly chest height).")]
    [SerializeField] private float moneyTargetHeight = 0.9f;
    [Tooltip("Coins landing within this many seconds of each other count as one chain.")]
    [SerializeField] private float moneyChainWindow = 0.2f;
    [SerializeField] private int moneyChainMax = 10;
    [SerializeField] private float moneyPitchStep = 0.04f;

    private float lastMoneyCollectTime = -10f;
    private int moneyChain;
    private float nextMoneyVFXTime;
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

    /// <summary>Base capacity from PlayerData plus the Carry Capacity upgrade. PlayerData itself is never modified.</summary>
    public int MaxPaperStack => Mathf.Max(0, playerData.maxPaperStackCount) + UpgradeManager.Instance.CapacityBonus;

    /// <summary>(carried papers, capacity). Raised whenever either changes; drives the HUD capacity chip.</summary>
    public static event Action<int, int> onPaperCountChanged;
    private void OnEnable()
    {
        SpawnManager.onInstantiatingPools += GeneratePaperStacking;
        UpgradeManager.onLevelChanged += OnUpgradeChanged;
        // The pools may already be ready if this component is enabled after SpawnManager.Start.
        if (SpawnManager.PoolsReady)
            GeneratePaperStacking();
    }

    private void OnDisable()
    {
        SpawnManager.onInstantiatingPools -= GeneratePaperStacking;
        UpgradeManager.onLevelChanged -= OnUpgradeChanged;
    }

    private void OnUpgradeChanged(UpgradeManager.UpgradeType type, int level)
    {
        // a bigger capacity frees room at once: the carried stack is untouched, the MAX label just goes away
        if (type == UpgradeManager.UpgradeType.Capacity) SyncPaperCount();
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

        int max = Mathf.Max(0, MaxPaperStack);
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
            // restored papers are placed instantly: no flight, pop or sound burst at startup
            if (!TryStackPaper(paper, false))
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

        if (maxStackText != null && count < MaxPaperStack && maxStackText.activeSelf)
            maxStackText.SetActive(false);

        onPaperCountChanged?.Invoke(count, MaxPaperStack);
    }


    /// <summary>
    /// Only answers "is there room?". It no longer changes any count; the count changes when the paper is actually stacked.
    /// </summary>
    public void CollectPaper(int collectedPaper, Action<bool> callback)
    {
        if (paperStack.Count >= MaxPaperStack)
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
        if (moneyObject == null) return;

        Transform moneyTransform = moneyObject.transform;
        moneyTransform.DOKill(true);             // finish the spawn pop, so the base scale below is the real one
        moneyTransform.SetParent(null, true);    // fly in world space, independent of the zone it came from
        Vector3 startPosition = moneyTransform.position;
        Vector3 baseScale = moneyTransform.localScale;

        // A short arc into the player. The target is re-read every frame, so the coin homes in on a moving player.
        DOVirtual.Float(0f, 1f, Mathf.Max(0.01f, moneyFlightDuration), progress =>
            {
                if (moneyObject == null) return;

                Vector3 target = transform.position + Vector3.up * moneyTargetHeight;
                Vector3 position = Vector3.LerpUnclamped(startPosition, target, progress);
                position.y += moneyArcHeight * Mathf.Sin(progress * Mathf.PI);
                moneyTransform.position = position;

                // swells slightly on the way up, then is absorbed by the player
                float scale = progress < 0.4f
                    ? Mathf.Lerp(1f, 1.2f, progress / 0.4f)
                    : Mathf.Lerp(1.2f, 0.35f, (progress - 0.4f) / 0.6f);
                moneyTransform.localScale = baseScale * scale;
            })
            .SetEase(Ease.InSine)                                    // accelerates into the player
            .SetUpdate(UpdateType.Late)                              // after the player moved this frame
            .SetTarget(moneyTransform)
            .SetRecyclable(true)
            .SetLink(moneyObject, LinkBehaviour.KillOnDisable)
            .OnComplete(() =>
            {
                // the coins are credited at the moment the coin lands, so the HUD counter changes exactly then
                CurrencyManager.Instance.UpdateCoins(playerData.moneyIncreaseRate);
                PlayMoneyCollectSound();
                PlayMoneyVFX();
                callback?.Invoke(); // gives the coin back to the pool
            });
    }

    /// <summary>Coins landing in quick succession climb in pitch (a rising "ka-ching" rhythm) and reset after a pause.</summary>
    private void PlayMoneyCollectSound()
    {
        moneyChain = Time.time - lastMoneyCollectTime <= moneyChainWindow ? Mathf.Min(moneyChain + 1, moneyChainMax) : 0;
        lastMoneyCollectTime = Time.time;
        AudioManager.Instance.PlaySFX(coinSound, 0.8f, 1f + moneyChain * moneyPitchStep);
    }


    /// <summary>
    /// Leading-edge throttle: the first credited coin of a burst plays the effect, the rest of the burst
    /// (coins arrive every few milliseconds) are skipped until the interval has passed.
    /// </summary>
    private void PlayMoneyVFX()
    {
        if (moneyRewardVFX == null || Time.time < nextMoneyVFXTime) return;

        nextMoneyVFXTime = Time.time + moneyVFXInterval;
        Vector3 offset = Vector3.up * moneyVFXHeightOffset;
        VFXPool.Instance.Play(moneyRewardVFX, transform.position + offset, transform, offset);
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
        return TryStackPaper(paper, true);
    }

    private bool TryStackPaper(GameObject paper, bool animated)
    {
        if (paper == null) return false;

        if (paperStack.Count >= MaxPaperStack)
        {
            if (maxStackText != null && !maxStackText.activeSelf)
                maxStackText.SetActive(true);
            return false;
        }

        // The slot is fixed now, so the final position never depends on the order the tweens finish in.
        int slot = paperStack.Count;
        Transform paperTransform = paper.transform;
        float paperHeight = paperTransform.lossyScale.y;

        // The slot is described in the hold point's own space, so the stack moves rigidly with the player.
        // World up gives the height (the hold point's own axes are rotated), so the stack stays vertical.
        Vector3 slotLocalPosition = holdPaperPoint.InverseTransformVector(Vector3.up * (paperHeight * slot));
        // Every paper takes the player's facing, so the stack stays aligned even when papers join while turning.
        Quaternion slotLocalRotation = Quaternion.Inverse(holdPaperPoint.rotation) * transform.rotation;

        paperStack.Push(paper);
        SyncPaperCount();

        paperTransform.DOKill(); // a reused paper must never carry a tween from its previous use

        if (animated)
            FlyPaperToSlot(paper, slot, slotLocalPosition, slotLocalRotation);
        else
            PlacePaperInSlot(paperTransform, slotLocalPosition, slotLocalRotation);

        return true;
    }

    /// <summary>
    /// Flies the paper to its slot. The slot is re-read from the moving hold point every frame, so the paper
    /// homes in on the player instead of a stale point, and it lands exactly in place (no snap at the end).
    /// </summary>
    private void FlyPaperToSlot(GameObject paper, int slot, Vector3 slotLocalPosition, Quaternion slotLocalRotation)
    {
        Transform paperTransform = paper.transform;
        Vector3 startPosition = paperTransform.position;
        Quaternion startRotation = paperTransform.rotation;

        DOVirtual.Float(0f, 1f, Mathf.Max(0.01f, pickupDuration), progress =>
            {
                if (paper == null) return;

                Vector3 target = holdPaperPoint.TransformPoint(slotLocalPosition);
                Vector3 position = Vector3.LerpUnclamped(startPosition, target, progress);
                position.y += pickupArcHeight * Mathf.Sin(progress * Mathf.PI);
                paperTransform.position = position;
                paperTransform.rotation = Quaternion.Slerp(startRotation, holdPaperPoint.rotation * slotLocalRotation, progress);
            })
            .SetEase(Ease.InOutSine)
            .SetUpdate(UpdateType.Late)                             // after the player has moved this frame, so the paper never trails by a frame
            .SetTarget(paperTransform)                              // paperTransform.DOKill() stops it
            .SetRecyclable(true)
            .SetLink(paper, LinkBehaviour.KillOnDisable)            // and so does returning the paper to the pool
            .OnComplete(() =>
            {
                if (paper == null) return;

                PlacePaperInSlot(paperTransform, slotLocalPosition, slotLocalRotation);
                PopPaper(paperTransform);
                // follows the landed paper (which now rides on the hold point) so it never trails behind a moving player
                Vector3 pickupVfxOffset = Vector3.up * pickupVFXHeightOffset;
                VFXPool.Instance.Play(paperPickupVFX, paperTransform.position + pickupVfxOffset, paperTransform, pickupVfxOffset);

                float fill = MaxPaperStack > 1 ? slot / (float)(MaxPaperStack - 1) : 0f;
                AudioManager.Instance.PlaySFX(paperSound, 1f, 1f + pickupPitchRange * fill);
            });
    }

    private void PlacePaperInSlot(Transform paperTransform, Vector3 localPosition, Quaternion localRotation)
    {
        paperTransform.SetParent(holdPaperPoint, true);
        paperTransform.localPosition = localPosition;
        paperTransform.localRotation = localRotation;
    }

    /// <summary>
    /// Small scale pop when a paper lands on the stack. The scale is always put back to exactly where it was,
    /// even if the pop is interrupted (delivered or returned to the pool mid-pop).
    /// </summary>
    private void PopPaper(Transform paperTransform)
    {
        Vector3 baseScale = paperTransform.localScale;

        DOTween.Sequence()
            .Append(paperTransform.DOScale(baseScale * popScale, popUpTime).SetEase(Ease.OutQuad))
            .Append(paperTransform.DOScale(baseScale, popDownTime).SetEase(Ease.OutBack))
            .SetTarget(paperTransform)
            .SetRecyclable(true)
            .SetLink(paperTransform.gameObject, LinkBehaviour.KillOnDisable)
            .OnKill(() =>
            {
                if (paperTransform != null) paperTransform.localScale = baseScale;
            });
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
