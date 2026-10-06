using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;


public class OfficeWorker : MonoBehaviour
{
    private Coroutine workCoroutine;
    private float workInterval = 0.4f; //I can make settings in scriptable object so i can control this variable from other places like upgrade the work to speed him up
    private int currentPaperAmount;
    public Office currentOffice;

    [Header("Work Feedback")]
    [Tooltip("The visible workstation (the desk model). All work/build animation is applied to this transform, " +
             "never to the worker root, because the root also carries the interaction zones.")]
    [SerializeField] private Transform workVisual;
    [Tooltip("Scale punch when the worker first receives papers (idle to working).")]
    [SerializeField] private float receivePunch = 0.08f;
    [Tooltip("Small scale punch on every processed paper, so the desk 'types' in rhythm with the work.")]
    [SerializeField] private float typingPunch = 0.025f;

    [Header("Build Animation")]
    [SerializeField] private float buildFootprintTime = 0.2f;
    [SerializeField] private float buildRiseTime = 0.4f;

    // Presentation hooks (read by OfficeAvatar). They carry no gameplay logic.
    public event Action PaperReceived;          // idle -> receives papers
    public event Action<bool> WorkingChanged;   // true when the worker starts processing, false when out of papers
    public event Action PaperProcessed;         // one paper turned into value
    public event Action<float> BuildStarted;    // purchase build-in began; arg = seconds until the desk impact

    private Vector3 visualBaseScale = Vector3.one;
    private bool visualBaseCached;
    private bool isProcessing;      // true from the first processed paper until the worker runs out of papers
    private float nextReceiveTime;  // keeps a stream of delivered papers from re-punching the desk every frame


    private void Awake()
    {
        CacheVisualScale();
    }

    private void OnDisable()
    {
        ResetVisual();
    }

    private void Start()
    {
        workCoroutine = StartCoroutine(Working());
    }

    private void CacheVisualScale()
    {
        if (workVisual == null || visualBaseCached) return;
        visualBaseScale = workVisual.localScale;
        visualBaseCached = true;
    }

    /// <summary>Stops any running desk tween and puts the desk back at its authored scale.</summary>
    private void ResetVisual()
    {
        if (workVisual == null) return;
        workVisual.DOKill();
        if (visualBaseCached) workVisual.localScale = visualBaseScale;
    }

    public int GetpaperCount()
    {
        return currentPaperAmount;
    }

    /// <param name="restored">True for papers rebuilt from the save: they never trigger the receive response.</param>
    public void OnGetpaper(bool restored = false)
    {
        currentPaperAmount++;

        // idle -> receives paper: the desk reacts the moment the paper lands, not when the next work tick comes
        if (!restored && !isProcessing && Time.time >= nextReceiveTime)
        {
            nextReceiveTime = Time.time + 0.5f;
            Punch(receivePunch, 0.28f);
            PaperReceived?.Invoke();
        }


        if (workCoroutine == null)
        {
            Debug.Log("Start Working Again ");
            workCoroutine = StartCoroutine(Working());
        }
    }


    private IEnumerator Working()
    {
        while (true)
        {
            yield return new WaitForSeconds(UpgradeManager.Instance.GetWorkInterval(workInterval));

            if(currentPaperAmount <= 0)
            {
                if (isProcessing) WorkingChanged?.Invoke(false);
                isProcessing = false;
                StopWorking();
            }
            else
            {
                // works: a light typing beat per processed paper
                Punch(typingPunch, 0.18f);
                if (!isProcessing) WorkingChanged?.Invoke(true);
                isProcessing = true;
                PaperProcessed?.Invoke();

                currentOffice.OnProceedWork();
                //Proceed Money 
                //onProceedWork.Invoke(this);
                currentPaperAmount--;
            }
        }
    }

    private void Punch(float amount, float duration)
    {
        if (workVisual == null || amount <= 0f) return;
        CacheVisualScale();

        workVisual.DOKill();
        workVisual.localScale = visualBaseScale; // never start a punch from a half-finished one
        workVisual.DOPunchScale(visualBaseScale * amount, duration, 4, 0.5f)
            .SetTarget(workVisual)
            .OnKill(() => { if (workVisual != null) workVisual.localScale = visualBaseScale; });
    }

    /// <summary>
    /// Build-in sequence for a freshly purchased workstation: the desk appears as a flat footprint, then rises
    /// with an overshoot. onImpact fires at the strongest moment (the overshoot) so the caller can sync VFX/SFX.
    /// </summary>
    public void PlayBuildAnimation(Action onImpact)
    {
        BuildStarted?.Invoke(buildFootprintTime + buildRiseTime * 0.55f);
        if (workVisual == null)
        {
            onImpact?.Invoke();
            return;
        }

        CacheVisualScale();
        Vector3 b = visualBaseScale;
        workVisual.DOKill();
        workVisual.localScale = new Vector3(b.x * 0.3f, 0f, b.z * 0.3f);

        float impactTime = buildFootprintTime + buildRiseTime * 0.55f;
        DOTween.Sequence()
            .Append(workVisual.DOScale(new Vector3(b.x, b.y * 0.05f, b.z), buildFootprintTime).SetEase(Ease.OutQuad))
            .Append(workVisual.DOScale(b, buildRiseTime).SetEase(Ease.OutBack, 2f))
            .InsertCallback(impactTime, () => onImpact?.Invoke())
            .SetTarget(workVisual)
            .OnKill(() => { if (workVisual != null) workVisual.localScale = b; });
    }

    private void StopWorking()
    {
        if (workCoroutine != null)
        {
            StopCoroutine(workCoroutine);
            workCoroutine = null;
        }
    }
}
