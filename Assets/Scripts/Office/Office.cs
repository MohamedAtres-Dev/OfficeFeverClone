using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class Office : MonoBehaviour
{
    public GameObject UIObject;

    public OfficeWorker officeWorker;
    [SerializeField] private MoneyCollectorZone moneyCollector;
    [SerializeField] private PaperSenderZone paperSender;
    [SerializeField] private OfficeGeneratorZone officeGenerator;


    [Header("VFX")]
    [Tooltip("Played once when this workstation is purchased and becomes active (UnlockUpgradeVFX).")]
    [SerializeField] private PooledVFX unlockVFX;
    [Tooltip("Where the unlock effect is centred, relative to the office root. Defaults to the worker's position.")]
    [SerializeField] private Vector3 unlockVFXOffset = new Vector3(0f, 0.5f, 0f);

    private OfficeState currentState;
    private bool isRestoringPapers;
    private bool unlockPlayed;

    public void CreateOffice()
    {
        OfficeFactory.Instance.CreateOffice(this);

        // Only a real purchase reaches here (the factory's load path never calls CreateOffice).
        if (unlockPlayed) return;
        unlockPlayed = true;
        Vector3 centre = officeWorker != null ? officeWorker.transform.position : transform.position;
        VFXPool.Instance.Play(unlockVFX, centre + unlockVFXOffset);
    }

    public int GetMoneyGeneratedCount()
    {
        return moneyCollector.GetMoneyGeneratedCount();
    }
    
    public int GetPaperCount()
    {
        return officeWorker.GetpaperCount();
    }

    public int GetMoneyPaid()
    {
        return officeGenerator.GetMoneyPaid();
    }

    public void SetOfficePrice(int price, int moneyPaid)
    {
        officeGenerator.OfficePrice = price;
        officeGenerator.MoneyPaid = moneyPaid;
    }



    public void SetOfficeState(OfficeState state)
    {
        currentState = state;
    }

    public int GetOfficeId()
    {
        return currentState.officeId;
    }
    public void GenerateMoney(int moneyAmount)
    {
        moneyCollector.GenerateInitialMoney(moneyAmount);
    }

    public void GeneratePaper(int paperAmount)
    {
        // papers restored from the save must not trigger the worker's "papers received" response
        isRestoringPapers = true;
        paperSender.GenerateInitialPapers(paperAmount);
        isRestoringPapers = false;
    }





    public void OnProceedWork()
    {
        moneyCollector.OnWorkerProceedWork(officeWorker);
        paperSender.OnWorkerProceedWork(officeWorker);
    }

    public void OnGetPaper()
    {
        officeWorker.OnGetpaper(isRestoringPapers);
    }
}
