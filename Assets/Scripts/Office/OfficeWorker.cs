using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public class OfficeWorker : MonoBehaviour
{
    private Coroutine workCoroutine;
    private float workInterval = 0.4f; //I can make settings in scriptable object so i can control this variable from other places like upgrade the work to speed him up
    private int currentPaperAmount;
    public Office currentOffice;


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

    public void OnGetpaper()
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
                currentOffice.OnProceedWork();
                //Proceed Money 
                //onProceedWork.Invoke(this);
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
