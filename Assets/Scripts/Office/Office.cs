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


    private OfficeState currentState;

    public void CreateOffice()
    {
        OfficeFactory.Instance.CreateOffice(this);
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
        paperSender.GenerateInitialPapers(paperAmount);
    }


}
