using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class OfficeGeneratorZone : Zone
{
    private int officePrice = 100;
    private int moneyPaid = 0;
    private float fillingTime = 0.2f;
    private Coroutine animatePurchase;

    [Header("UI Elements")]
    public TextMeshProUGUI priceText;
    public Image progressImage;

    public Office myOffice;

    public int OfficePrice { get => officePrice; set => officePrice = value; }
    public int MoneyPaid { get => moneyPaid; set => moneyPaid = value; }

    private void Start()
    {
        priceText.text = MathHelper.FormatNumber(officePrice - moneyPaid);
        progressImage.fillAmount = (float)moneyPaid / (float)officePrice;
    }


    public override void PerformAction(PlayerManager playerManager)
    {
        base.PerformAction(playerManager);

        //Here I need the office price to check first 
        if (CurrencyManager.Instance.GetCoins() >= (officePrice - moneyPaid))
        {
            int paidAmount = officePrice - moneyPaid;
            CurrencyManager.Instance.UpdateCoins(-paidAmount);

            //Withdraw the money with animation for UI 
            moneyPaid = officePrice;
            if (animatePurchase == null)
                animatePurchase = StartCoroutine(AnimatePurchase(paidAmount, playerManager));
           
            return;
        }
        else
        {
            moneyPaid += CurrencyManager.Instance.GetCoins();
            if (animatePurchase == null)
                animatePurchase = StartCoroutine(AnimatePurchase(CurrencyManager.Instance.GetCoins(), playerManager));
            CurrencyManager.Instance.ResetCoins();
            Debug.Log("Perform Action on Office Zone - Money Paid " + moneyPaid + "rest of the money " + (officePrice - moneyPaid));
        }
    }

    private IEnumerator AnimatePurchase(int paidAmount, PlayerManager playerManager)
    {
        if (paidAmount <= 0)
            yield break;

        // Calculate the difference between the full price and the paid amount
        int remainingAmount = Mathf.Max(officePrice - moneyPaid, 0);

        Debug.Log("The Remaing amount " + remainingAmount);
        // Animate the price text from the full price to the remaining amount
        priceText.text = MathHelper.FormatNumber(officePrice - moneyPaid);
        priceText.transform.DOScale(1.2f, 0.2f).SetLoops(2, LoopType.Yoyo);

        DOTween.To(() => MathHelper.ParseFormattedNumber(priceText.text), x => priceText.text = MathHelper.FormatNumber(x), remainingAmount, fillingTime).SetEase(Ease.Linear);


        // Calculate the fill amount based on the paid amount
        float fillAmount = (float)moneyPaid / (float)officePrice;

        // Animate the progress image if the paid amount is greater than zero
        if (paidAmount > 0)
        {
            progressImage.DOFillAmount(fillAmount, fillingTime).SetEase(Ease.Linear);
        }

        yield return new WaitForSeconds(fillingTime);

        // Set the final values
        priceText.text = remainingAmount.ToString();

        if (moneyPaid == officePrice)
        {
            progressImage.fillAmount = 1.0f;
            myOffice.CreateOffice();
            yield break;
        }
        else if (paidAmount > 0)
        {
            progressImage.fillAmount = fillAmount;
        }
    }

    public int GetMoneyPaid()
    {
        return moneyPaid;
    }

    public override void StopAction()
    {
        base.StopAction();
        if (animatePurchase != null)
        {
            StopCoroutine(animatePurchase);
            animatePurchase = null;
        }
    }
}
