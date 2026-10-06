using System;
using UnityEngine;

/// <summary>
/// Two tiny upgrades, nothing more: Carry Capacity (extra papers the player can carry) and Worker Speed (shorter work tick).
/// Levels are stored in PlayerPrefs next to the coins, so the office save format is untouched.
/// The upgrades never write into the PlayerData / OfficeSettings assets: they only add to or scale the base values.
/// </summary>
public class UpgradeManager : Singlton<UpgradeManager>
{
    public enum UpgradeType { Capacity, WorkerSpeed }
    public enum PurchaseResult { Success, NotEnoughMoney, MaxLevel }

    [Header("Carry Capacity (level 0 = the base capacity in PlayerData)")]
    [SerializeField] private int[] capacityBonus = { 0, 10, 20 };
    [SerializeField] private int[] capacityCosts = { 100, 250 };       // cost to reach level 1, level 2

    [Header("Worker Speed (multiplier of the work tick time)")]
    [SerializeField] private float[] speedMultiplier = { 1f, 0.8f, 0.62f };
    [SerializeField] private int[] speedCosts = { 150, 350 };
    [Tooltip("The work tick never goes below this many seconds, so animation and logic stay readable.")]
    [SerializeField] private float minWorkInterval = 0.22f;

    [Header("Feedback")]
    [SerializeField] private AudioClip purchaseSound;
    [SerializeField] private AudioClip deniedSound;

    private const string CapacityKey = "Upgrade_Capacity";
    private const string SpeedKey = "Upgrade_WorkerSpeed";

    private int capacityLevel;
    private int speedLevel;

    /// <summary>type, new level</summary>
    public static event Action<UpgradeType, int> onLevelChanged;

    protected override void Awake()
    {
        base.Awake();
        capacityLevel = Mathf.Clamp(PlayerPrefs.GetInt(CapacityKey, 0), 0, capacityCosts.Length);
        speedLevel = Mathf.Clamp(PlayerPrefs.GetInt(SpeedKey, 0), 0, speedCosts.Length);
    }

    // ---- queries --------------------------------------------------------------------------------------------------

    public int GetLevel(UpgradeType type) => type == UpgradeType.Capacity ? capacityLevel : speedLevel;
    public int GetMaxLevel(UpgradeType type) => (type == UpgradeType.Capacity ? capacityCosts : speedCosts).Length;
    public bool IsMaxed(UpgradeType type) => GetLevel(type) >= GetMaxLevel(type);

    /// <summary>Cost of the next level, or 0 when maxed.</summary>
    public int GetNextCost(UpgradeType type)
    {
        if (IsMaxed(type)) return 0;
        var costs = type == UpgradeType.Capacity ? capacityCosts : speedCosts;
        return costs[GetLevel(type)];
    }

    public int CapacityBonus => capacityBonus[Mathf.Clamp(capacityLevel, 0, capacityBonus.Length - 1)];
    public int NextCapacityBonus => IsMaxed(UpgradeType.Capacity) ? CapacityBonus : capacityBonus[capacityLevel + 1];

    /// <summary>Work tick time for a worker whose authored (base) tick is baseInterval.</summary>
    public float GetWorkInterval(float baseInterval)
    {
        float multiplier = speedMultiplier[Mathf.Clamp(speedLevel, 0, speedMultiplier.Length - 1)];
        return Mathf.Max(minWorkInterval, baseInterval * multiplier);
    }

    /// <summary>1 = base speed, larger = faster. Used to speed up the typing animation along with the logic.</summary>
    public float WorkSpeedFactor => 1f / speedMultiplier[Mathf.Clamp(speedLevel, 0, speedMultiplier.Length - 1)];

    public float NextWorkSpeedFactor => 1f / speedMultiplier[Mathf.Clamp(speedLevel + 1, 0, speedMultiplier.Length - 1)];

    // ---- purchase -------------------------------------------------------------------------------------------------

    public bool CanAfford(UpgradeType type) => !IsMaxed(type) && CurrencyManager.Instance.GetCoins() >= GetNextCost(type);

    /// <summary>Spends the cost exactly once and raises the level, or changes nothing.</summary>
    public PurchaseResult TryPurchase(UpgradeType type)
    {
        if (IsMaxed(type)) return PurchaseResult.MaxLevel;

        int cost = GetNextCost(type);
        if (CurrencyManager.Instance.GetCoins() < cost)
        {
            AudioManager.Instance.PlaySFX(deniedSound, 0.6f, 0.7f);
            return PurchaseResult.NotEnoughMoney;
        }

        // level first, then money: a re-entrant call from an event handler sees the new level and the new price
        int newLevel = GetLevel(type) + 1;
        if (type == UpgradeType.Capacity) capacityLevel = newLevel; else speedLevel = newLevel;
        CurrencyManager.Instance.UpdateCoins(-cost);

        PlayerPrefs.SetInt(type == UpgradeType.Capacity ? CapacityKey : SpeedKey, newLevel);
        PlayerPrefs.Save();

        AudioManager.Instance.PlaySFX(purchaseSound, 0.9f, 1f + 0.12f * newLevel);
        onLevelChanged?.Invoke(type, newLevel);
        return PurchaseResult.Success;
    }
}
