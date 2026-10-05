using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SpawnManager : Singlton<SpawnManager>
{
    [Header("Paper")]
    [SerializeField] private GameObject paperPrefab;
    [SerializeField] private int paperPoolSize;
    
    [Header("Money")]
    [Space]
    [SerializeField] private GameObject moneyPrefab;
    [SerializeField] private int moneyPoolSize;

    public static UnityAction onInstantiatingPools = delegate { };

    /// <summary>
    /// True once the pools exist and onInstantiatingPools has been raised. Listeners that are enabled
    /// after the event (late-enabled objects, scene reloads) check this instead of waiting for an event that already fired.
    /// </summary>
    public static bool PoolsReady { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        PoolsReady = false;
    }

    // The pools are built in Awake so they exist for every Start (e.g. OfficeFactory), but the event is raised
    // in Start: every OnEnable has run by then, so no listener can miss it.
    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return; // duplicate singleton, the original already owns the pools

        PoolManager.Instance.InstantiatePool(ObjectPoolTypes.PAPER, paperPrefab, paperPoolSize);
        PoolManager.Instance.InstantiatePool(ObjectPoolTypes.MONEY, moneyPrefab, moneyPoolSize);
    }

    private void Start()
    {
        if (Instance != this || PoolsReady) return;

        PoolsReady = true;
        onInstantiatingPools.Invoke();
    }

}
