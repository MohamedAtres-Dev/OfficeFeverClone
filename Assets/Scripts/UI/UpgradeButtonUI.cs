using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// One compact HUD card for one upgrade: title, level pips and the next price.
/// States: affordable (green price, gentle pulse), unaffordable (red price, muted), maxed (MAX badge, not clickable).
/// </summary>
public class UpgradeButtonUI : MonoBehaviour
{
    [SerializeField] private UpgradeManager.UpgradeType type;
    [SerializeField] private Button button;
    [SerializeField] private RectTransform root;
    [SerializeField] private CanvasGroup group;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Image coinIcon;
    [SerializeField] private Image[] pips;
    [SerializeField] private Image iconBackground;

    [Header("Colours")]
    [SerializeField] private Color affordableColor = new Color(0.18f, 0.66f, 0.40f, 1f);
    [SerializeField] private Color insufficientColor = new Color(0.90f, 0.35f, 0.31f, 1f);
    [SerializeField] private Color pipOn = new Color(1f, 0.72f, 0.12f, 1f);
    [SerializeField] private Color pipOff = new Color(0.80f, 0.83f, 0.88f, 1f);

    private enum State { None, Affordable, Insufficient, Maxed }
    private State state = State.None;
    private int shownCoins = -1;

    private void OnEnable()
    {
        UpgradeManager.onLevelChanged += OnLevelChanged;
        CurrencyManager.onUpdateCoins += OnCoinsChanged;
        button.onClick.AddListener(OnClick);
        state = State.None;
        Refresh();
    }

    private void OnDisable()
    {
        UpgradeManager.onLevelChanged -= OnLevelChanged;
        CurrencyManager.onUpdateCoins -= OnCoinsChanged;
        button.onClick.RemoveListener(OnClick);
        root.DOKill();
        root.localScale = Vector3.one;
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

    private void OnClick()
    {
        switch (UpgradeManager.Instance.TryPurchase(type))
        {
            case UpgradeManager.PurchaseResult.Success:
                root.DOKill(true);
                root.DOPunchScale(Vector3.one * 0.14f, 0.3f, 6, 0.6f).SetTarget(root);
                PopPip(UpgradeManager.Instance.GetLevel(type) - 1);
                break;
            case UpgradeManager.PurchaseResult.NotEnoughMoney:
                root.DOKill(true);
                root.DOShakeAnchorPos(0.3f, new Vector2(14f, 0f), 18, 0f, false, true).SetTarget(root);
                costText.transform.DOKill(true);
                costText.transform.DOPunchScale(Vector3.one * 0.25f, 0.3f, 6, 0.5f).SetTarget(costText.transform);
                break;
        }
    }

    private void PopPip(int index)
    {
        if (pips == null || index < 0 || index >= pips.Length) return;
        pips[index].transform.DOKill(true);
        pips[index].transform.localScale = Vector3.one;
        pips[index].transform.DOPunchScale(Vector3.one * 0.7f, 0.35f, 6, 0.6f).SetTarget(pips[index].transform);
    }

    private void Refresh()
    {
        var up = UpgradeManager.Instance;
        State next = up.IsMaxed(type) ? State.Maxed : (up.CanAfford(type) ? State.Affordable : State.Insufficient);

        if (next == state) return;
        state = next;

        int level = up.GetLevel(type);
        for (int i = 0; pips != null && i < pips.Length; i++)
            pips[i].color = i < level ? pipOn : pipOff;

        titleText.text = BuildTitle(up, state == State.Maxed);

        switch (state)
        {
            case State.Maxed:
                costText.text = "MAX";
                costText.color = affordableColor;
                coinIcon.gameObject.SetActive(false);
                button.interactable = false;
                group.alpha = 1f;
                StopPulse();
                break;
            case State.Affordable:
                costText.text = MathHelper.FormatNumber(up.GetNextCost(type));
                costText.color = affordableColor;
                coinIcon.gameObject.SetActive(true);
                button.interactable = true;
                group.alpha = 1f;
                StartPulse();
                break;
            default:
                costText.text = MathHelper.FormatNumber(up.GetNextCost(type));
                costText.color = insufficientColor;
                coinIcon.gameObject.SetActive(true);
                button.interactable = true;
                group.alpha = 0.82f;
                StopPulse();
                break;
        }
    }

    /// <summary>"CARRY +10" / "SPEED +25%": the next level's benefit, so the card reads without any explanation.</summary>
    private string BuildTitle(UpgradeManager up, bool maxed)
    {
        if (type == UpgradeManager.UpgradeType.Capacity)
            return maxed ? "CARRY" : "CARRY +" + (up.NextCapacityBonus - up.CapacityBonus);
        return maxed ? "SPEED" : "SPEED +" + Mathf.RoundToInt((up.NextWorkSpeedFactor / up.WorkSpeedFactor - 1f) * 100f) + "%";
    }

    private void StartPulse()
    {
        if (DOTween.IsTweening(root)) return;
        root.DOScale(1.04f, 0.6f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetTarget(root);
    }

    private void StopPulse()
    {
        root.DOKill();
        root.localScale = Vector3.one;
    }
}
