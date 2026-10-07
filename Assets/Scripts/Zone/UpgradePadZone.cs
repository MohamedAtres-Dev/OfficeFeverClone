using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// A physical upgrade station on the office floor: a round pad with the upgrade's icon and a small sign stand behind
/// it showing the next benefit, price and level. Standing on the pad briefly charges the ring, then buys one level
/// through the existing UpgradeManager.TryPurchase. One purchase per visit: the player has to step off before the
/// pad can sell the next level, so a player who keeps standing never buys twice by accident.
/// </summary>
public class UpgradePadZone : Zone
{
    [SerializeField] private UpgradeManager.UpgradeType type;

    [Header("Pad")]
    [Tooltip("Physical pad top: punched on purchase and tinted grey when maxed.")]
    [SerializeField] private Transform padTop;
    [SerializeField] private Renderer padTopRenderer;
    [SerializeField] private Image chargeRing;
    [SerializeField] private Transform icon;
    [SerializeField] private Graphic iconGraphic;

    [Header("Sign")]
    [SerializeField] private Transform sign;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private GameObject coinIcon;
    [SerializeField] private Image[] pips;

    [Header("Look")]
    [SerializeField] private Color accentColor = new Color(0.30f, 0.64f, 1f, 1f);
    [SerializeField] private Color maxedColor = new Color(0.72f, 0.76f, 0.82f, 1f);
    [SerializeField] private Color affordableColor = new Color(0.18f, 0.66f, 0.40f, 1f);
    [SerializeField] private Color insufficientColor = new Color(0.90f, 0.35f, 0.31f, 1f);
    [SerializeField] private Color maxTextColor = new Color(0.95f, 0.66f, 0.10f, 1f);
    [SerializeField] private Color pipOn = new Color(1f, 0.72f, 0.12f, 1f);
    [SerializeField] private Color pipOff = new Color(0.80f, 0.83f, 0.88f, 1f);

    [Header("Feel")]
    [Tooltip("Seconds the player stands on the pad before it buys (stops a player walking past from buying by accident).")]
    [SerializeField] private float chargeTime = 0.45f;
    [SerializeField] private int paymentBundles = 5;
    [SerializeField] private AudioClip deniedSound;

    private enum State { None, Affordable, Insufficient, Maxed }
    private State state = State.None;
    private Coroutine chargeCoroutine;
    private bool armed = true;          // false after a purchase until the player steps off
    private float nextDeniedTime;
    private MaterialPropertyBlock block;
    private Vector3 iconBaseScale = Vector3.one;
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        if (icon != null) iconBaseScale = icon.localScale;
    }

    private bool started;

    private void OnEnable()
    {
        UpgradeManager.onLevelChanged += OnLevelChanged;
        CurrencyManager.onUpdateCoins += OnCoinsChanged;
        state = State.None;
        armed = true;
        SetCharge(0f);
        if (started) Refresh(); // the first refresh waits for Start: the managers must have run Awake before anyone asks for them
    }

    private void Start()
    {
        started = true;
        Refresh();
    }

    private void OnDisable()
    {
        UpgradeManager.onLevelChanged -= OnLevelChanged;
        CurrencyManager.onUpdateCoins -= OnCoinsChanged;
        chargeCoroutine = null; // coroutines stop with the object
        KillTweens();
    }

    private void KillTweens()
    {
        if (padTop != null) padTop.DOKill(true);
        if (sign != null) sign.DOKill(true);
        if (costText != null) costText.transform.DOKill(true);
        if (icon != null) { icon.DOKill(); icon.localScale = iconBaseScale; }
        DOTween.Kill(this);
    }

    private void OnLevelChanged(UpgradeManager.UpgradeType changed, int level)
    {
        if (changed != type) return;
        state = State.None;
        Refresh();
    }

    private void OnCoinsChanged(int coins)
    {
        Refresh();
    }

    // ---- trigger ----------------------------------------------------------------------------------------------------

    public override void PerformAction(PlayerManager playerManager)
    {
        base.PerformAction(playerManager);
        if (!armed || chargeCoroutine != null) return;

        var up = UpgradeManager.Instance;
        if (up.IsMaxed(type)) return;
        if (!up.CanAfford(type))
        {
            ShowDenied();
            return;
        }
        chargeCoroutine = StartCoroutine(Charge(playerManager));
    }

    public override void StopAction()
    {
        base.StopAction();
        armed = true;
        if (chargeCoroutine != null)
        {
            StopCoroutine(chargeCoroutine);
            chargeCoroutine = null;
        }
        if (chargeRing != null) chargeRing.DOFillAmount(0f, 0.15f).SetTarget(this);
    }

    private IEnumerator Charge(PlayerManager playerManager)
    {
        DOTween.Kill(this); // a ring that was still emptying must not fight the charge
        float t = 0f;
        while (t < chargeTime)
        {
            t += Time.deltaTime;
            SetCharge(t / chargeTime);
            yield return null;
        }

        chargeCoroutine = null;
        armed = false; // one purchase per visit, whatever the result

        int paid = UpgradeManager.Instance.GetNextCost(type);
        if (UpgradeManager.Instance.TryPurchase(type) == UpgradeManager.PurchaseResult.Success)
            PlayPurchaseFeedback(playerManager.transform, paid);
        else
            ShowDenied();

        if (chargeRing != null) chargeRing.DOFillAmount(0f, 0.25f).SetDelay(0.15f).SetTarget(this);
    }

    private void SetCharge(float value)
    {
        if (chargeRing != null) chargeRing.fillAmount = Mathf.Clamp01(value);
    }

    // ---- feedback ---------------------------------------------------------------------------------------------------

    private void PlayPurchaseFeedback(Transform player, int paid)
    {
        FlyPayment(player);

        if (padTop != null)
        {
            padTop.DOKill(true);
            padTop.DOPunchScale(new Vector3(0.12f, 0.35f, 0.12f), 0.35f, 6, 0.5f).SetTarget(padTop);
        }
        if (sign != null)
        {
            sign.DOKill(true);
            sign.DOPunchScale(Vector3.one * 0.1f, 0.35f, 6, 0.5f).SetDelay(0.12f).SetTarget(sign);
        }

        // the paid price counts down to 0, then the next price (or MAX) pops in
        if (costText != null)
        {
            Transform ct = costText.transform;
            ct.DOKill(true);
            costText.color = affordableColor;
            DOTween.Sequence()
                .Append(DOTween.To(x => costText.text = MathHelper.FormatNumber(Mathf.RoundToInt(x)), paid, 0f, 0.22f).SetEase(Ease.OutQuad))
                .AppendCallback(() => { state = State.None; Refresh(); })
                .Append(ct.DOPunchScale(Vector3.one * 0.3f, 0.3f, 6, 0.5f))
                .SetTarget(ct);
            costText.text = MathHelper.FormatNumber(paid); // Refresh already wrote the new price this frame
        }

        PopPip(UpgradeManager.Instance.GetLevel(type) - 1);
    }

    /// <summary>A few cash bundles from the money pool hop from the player into the pad (paying is visible).</summary>
    private void FlyPayment(Transform player)
    {
        Vector3 target = (padTop != null ? padTop.position : transform.position) + Vector3.up * 0.15f;
        for (int i = 0; i < paymentBundles; i++)
        {
            GameObject money = PoolManager.Instance.GetObjectFromPool(ObjectPoolTypes.MONEY);
            if (money == null) return;

            Transform t = money.transform;
            t.SetParent(null, true);
            Vector3 start = player.position + Vector3.up * 1.0f;
            Vector3 baseScale = t.localScale;
            t.position = start;
            t.localScale = Vector3.zero;

            DOVirtual.Float(0f, 1f, 0.32f, p =>
                {
                    Vector3 pos = Vector3.LerpUnclamped(start, target, p);
                    pos.y += 0.7f * Mathf.Sin(p * Mathf.PI);
                    t.position = pos;
                    t.localScale = baseScale * (p < 0.2f ? p / 0.2f : Mathf.Lerp(1f, 0.4f, (p - 0.2f) / 0.8f));
                })
                .SetDelay(i * 0.05f)
                .SetEase(Ease.InSine)
                .SetTarget(t)
                .SetLink(money, LinkBehaviour.KillOnDisable)
                .OnKill(() => { if (money != null && money.activeSelf) PoolManager.Instance.ReturnObjectToPool(ObjectPoolTypes.MONEY, money); });
        }
    }

    private void ShowDenied()
    {
        if (Time.time < nextDeniedTime) return;   // stepping on and off must not spam the feedback
        nextDeniedTime = Time.time + 0.6f;

        AudioManager.Instance.PlaySFX(deniedSound, 0.6f, 0.7f);
        if (costText == null) return;
        Transform ct = costText.transform;
        ct.DOKill(true);
        ct.DOPunchRotation(new Vector3(0f, 0f, 12f), 0.35f, 14, 0.6f).SetTarget(ct);
        ct.DOPunchScale(Vector3.one * 0.18f, 0.3f, 6, 0.5f).SetTarget(ct);
    }

    private void PopPip(int index)
    {
        if (pips == null || index < 0 || index >= pips.Length) return;
        Transform p = pips[index].transform;
        p.DOKill(true);
        p.localScale = Vector3.one;
        p.DOPunchScale(Vector3.one * 0.8f, 0.35f, 6, 0.6f).SetDelay(0.2f).SetTarget(p);
    }

    // ---- state ------------------------------------------------------------------------------------------------------

    private void Refresh()
    {
        var up = UpgradeManager.Instance;
        State next = up.IsMaxed(type) ? State.Maxed : (up.CanAfford(type) ? State.Affordable : State.Insufficient);
        if (next == state) return;
        state = next;

        int level = up.GetLevel(type);
        for (int i = 0; pips != null && i < pips.Length; i++)
            pips[i].color = i < level ? pipOn : pipOff;

        if (titleText != null) titleText.text = BuildTitle(up, state == State.Maxed);

        bool maxed = state == State.Maxed;
        if (coinIcon != null) coinIcon.SetActive(!maxed);
        if (costText != null)
        {
            costText.text = maxed ? "MAX" : MathHelper.FormatNumber(up.GetNextCost(type));
            costText.color = maxed ? maxTextColor : (state == State.Affordable ? affordableColor : insufficientColor);
        }
        if (iconGraphic != null) iconGraphic.color = new Color(1f, 1f, 1f, maxed ? 0.55f : 1f);
        if (chargeRing != null) chargeRing.gameObject.SetActive(!maxed);
        TintPad(maxed ? maxedColor : accentColor);

        // affordable: the icon breathes so the pad reads as "you can buy this now"
        if (icon != null)
        {
            icon.DOKill();
            icon.localScale = iconBaseScale;
            if (state == State.Affordable)
                icon.DOScale(iconBaseScale * 1.1f, 0.55f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetTarget(icon);
        }
    }

    private void TintPad(Color color)
    {
        if (padTopRenderer == null) return;
        block ??= new MaterialPropertyBlock();
        padTopRenderer.GetPropertyBlock(block);
        block.SetColor(ColorId, color);
        padTopRenderer.SetPropertyBlock(block);
    }

    /// <summary>"CARRY +10" / "SPEED +25%": the next level's benefit, so the sign reads without any explanation.</summary>
    private string BuildTitle(UpgradeManager up, bool maxed)
    {
        if (type == UpgradeManager.UpgradeType.Capacity)
            return maxed ? "CARRY" : "CARRY +" + (up.NextCapacityBonus - up.CapacityBonus);
        return maxed ? "SPEED" : "SPEED +" + Mathf.RoundToInt((up.NextWorkSpeedFactor / up.WorkSpeedFactor - 1f) * 100f) + "%";
    }
}
