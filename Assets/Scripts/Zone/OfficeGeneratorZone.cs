using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// The purchase zone of a locked workstation. Stepping on it buys the workstation in one go when the player can afford
/// it (the price is deducted exactly once), otherwise the price just shakes and nothing is taken.
/// moneyPaid is still saved by the factory; an old save with a partial payment is honoured as credit.
/// </summary>
public class OfficeGeneratorZone : Zone
{
    private int officePrice = 100;
    private int moneyPaid = 0;
    private float fillingTime = 0.2f;
    private Coroutine animatePurchase;
    private bool purchased;           // set the moment the money is taken: blocks any second purchase
    private float nextDeniedTime;

    [Header("UI Elements")]
    public TextMeshProUGUI priceText;
    public Image progressImage;
    [Tooltip("Gently pulses while the player can afford the workstation.")]
    [SerializeField] private Transform pulseTarget;
    [SerializeField] private Color affordableColor = new Color(0.18f, 0.66f, 0.40f, 1f);
    [SerializeField] private Color insufficientColor = new Color(0.90f, 0.35f, 0.31f, 1f);
    [SerializeField] private AudioClip deniedSound;

    public Office myOffice;

    public int OfficePrice { get => officePrice; set => officePrice = value; }
    public int MoneyPaid { get => moneyPaid; set => moneyPaid = value; }

    private int Remaining => Mathf.Max(0, officePrice - moneyPaid);

    private void OnEnable()
    {
        CurrencyManager.onUpdateCoins += OnCoinsChanged;
    }

    private void OnDisable()
    {
        CurrencyManager.onUpdateCoins -= OnCoinsChanged;
        KillTweens();
        animatePurchase = null;
    }

    private void Start()
    {
        priceText.text = MathHelper.FormatNumber(Remaining);
        progressImage.fillAmount = officePrice > 0 ? (float)moneyPaid / officePrice : 0f;
        RefreshAffordability();
    }

    private void KillTweens()
    {
        if (priceText != null) priceText.transform.DOKill(true);
        if (pulseTarget != null) { pulseTarget.DOKill(); pulseTarget.localScale = Vector3.one; }
        if (progressImage != null) progressImage.DOKill();
    }

    private void OnCoinsChanged(int coins)
    {
        if (!purchased) RefreshAffordability();
    }

    /// <summary>Price colour and pulse follow whether the player can afford the workstation right now.</summary>
    private void RefreshAffordability()
    {
        if (priceText == null || purchased) return;

        bool canAfford = CurrencyManager.Instance.GetCoins() >= Remaining;
        priceText.color = canAfford ? affordableColor : insufficientColor;

        if (pulseTarget == null) return;
        bool pulsing = DOTween.IsTweening(pulseTarget);
        if (canAfford && !pulsing)
            pulseTarget.DOScale(1.07f, 0.55f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetTarget(pulseTarget);
        else if (!canAfford && pulsing)
        {
            pulseTarget.DOKill();
            pulseTarget.localScale = Vector3.one;
        }
    }

    public override void PerformAction(PlayerManager playerManager)
    {
        base.PerformAction(playerManager);
        if (purchased) return;

        int remaining = Remaining;
        if (CurrencyManager.Instance.GetCoins() < remaining)
        {
            ShowDenied();
            return;
        }

        // Purchase: flag first, then take the money once. Nothing below can run twice.
        purchased = true;
        if (remaining > 0) CurrencyManager.Instance.UpdateCoins(-remaining);
        moneyPaid = officePrice;

        if (pulseTarget != null) { pulseTarget.DOKill(); pulseTarget.localScale = Vector3.one; }
        animatePurchase = StartCoroutine(AnimatePurchase(remaining));
    }

    private void ShowDenied()
    {
        if (Time.time < nextDeniedTime) return;   // walking in and out of the zone must not spam the feedback
        nextDeniedTime = Time.time + 0.6f;

        AudioManager.Instance.PlaySFX(deniedSound, 0.6f, 0.7f);
        priceText.transform.DOKill(true);
        priceText.transform.DOPunchRotation(new Vector3(0f, 0f, 12f), 0.35f, 14, 0.6f).SetTarget(priceText.transform);
        priceText.transform.DOPunchScale(Vector3.one * 0.18f, 0.3f, 6, 0.5f).SetTarget(priceText.transform);
    }

    private IEnumerator AnimatePurchase(int paidAmount)
    {
        // the bar fills and the price counts down to 0, then the workstation builds itself (Office.CreateOffice)
        priceText.color = affordableColor;
        priceText.transform.DOScale(1.2f, 0.1f).SetLoops(2, LoopType.Yoyo).SetTarget(priceText.transform);
        DOTween.To(() => MathHelper.ParseFormattedNumber(priceText.text), x => priceText.text = MathHelper.FormatNumber(x), 0, fillingTime)
            .SetEase(Ease.Linear).SetTarget(priceText.transform);
        progressImage.DOFillAmount(1f, fillingTime).SetEase(Ease.Linear);

        yield return new WaitForSeconds(fillingTime);

        priceText.text = "0";
        progressImage.fillAmount = 1f;
        animatePurchase = null;
        myOffice.CreateOffice();   // hides this UI, builds the desk and plays the unlock VFX once
    }

    public int GetMoneyPaid()
    {
        return moneyPaid;
    }

    public override void StopAction()
    {
        base.StopAction();
        // Leaving the zone never cancels a purchase that already took the money: the build still finishes.
    }
}
