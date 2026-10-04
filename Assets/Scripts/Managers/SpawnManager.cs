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

    // Start is called before the first frame update
    protected override void Awake()
    {
        base.Awake();
        PoolManager.Instance.InstantiatePool(ObjectPoolTypes.PAPER, paperPrefab, paperPoolSize);
        PoolManager.Instance.InstantiatePool(ObjectPoolTypes.MONEY, moneyPrefab, moneyPoolSize);
        onInstantiatingPools.Invoke();
    }

}
