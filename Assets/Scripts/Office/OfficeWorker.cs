using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public class OfficeWorker : MonoBehaviour
{
    private Coroutine workCoroutine;
    private float workInterval = 0.4f; //I can make settings in scriptable object so i can control this variable from other places like upgrade the work to speed him up
    private int currentPaperAmount;

    public static UnityAction onProceedWork = delegate { };
    private void OnEnable()
    {
        PaperSenderZone.onGetPaper += OnGetpaper;
    }

    private void OnDisable()
    {
        PaperSenderZone.onGetPaper -= OnGetpaper;
    }

    private void Start()
    {
        workCoroutine = StartCoroutine(Working());
    }

    public int GetpaperCount()
    {
        return currentPaperAmount;
    }

    private void OnGetpaper()
    {
        currentPaperAmount++;


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

                StopWorking();
            }
            else
            {
                //Proceed Money 
                onProceedWork.Invoke();
                currentPaperAmount--;
            }
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
