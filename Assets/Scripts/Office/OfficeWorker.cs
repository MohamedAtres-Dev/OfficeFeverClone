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

    [Header("Work Response")]
    [Tooltip("Small effect played when the worker starts on newly received papers (reuses PaperPickupVFX).")]
    [SerializeField] private PooledVFX workStartVFX;
    [SerializeField] private float workStartVFXHeight = 1.2f;
    [Tooltip("Scale punch added to the worker when they start working. 0 disables it.")]
    [SerializeField] private float workStartPunch = 0.08f;

    private bool responsePending;   // papers were received at runtime (not restored from the save)
    private bool isProcessing;      // the response already played for the current work session


    //make the office be the coordinator between these classes 
    private void OnEnable()
    {
        
    }

    private void OnDisable()
    {
       
    }

    private void Start()
    {
        workCoroutine = StartCoroutine(Working());
    }

    public int GetpaperCount()
    {
        return currentPaperAmount;
    }

    /// <param name="restored">True for papers rebuilt from the save: they never trigger the work response.</param>
    public void OnGetpaper(bool restored = false)
    {
        currentPaperAmount++;
        if (!restored) responsePending = true;


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
            yield return new WaitForSeconds(workInterval);

            if(currentPaperAmount <= 0)
            {
                isProcessing = false;
                responsePending = false;
                StopWorking();
            }
            else
            {
                if (responsePending && !isProcessing)
                    PlayWorkStartResponse();
                responsePending = false;

                currentOffice.OnProceedWork();
                //Proceed Money 
                //onProceedWork.Invoke(this);
                currentPaperAmount--;
            }
        }
    }

    /// <summary>Once per work session (idle to working): a small effect over the worker plus a tiny scale punch.</summary>
    private void PlayWorkStartResponse()
    {
        isProcessing = true;
        VFXPool.Instance.Play(workStartVFX, transform.position + Vector3.up * workStartVFXHeight);

        if (workStartPunch > 0f)
        {
            transform.DOComplete(); // finish any running punch first so the base scale is never captured mid-punch
            transform.DOPunchScale(Vector3.one * workStartPunch, 0.25f, 6, 0.5f).SetTarget(transform);
        }
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
