using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SpawnManager : MonoBehaviour
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
    void Awake()
    {
        PoolManager.Instance.InstantiatePool(ObjectPoolTypes.PAPER, paperPrefab, paperPoolSize);
        PoolManager.Instance.InstantiatePool(ObjectPoolTypes.MONEY, moneyPrefab, moneyPoolSize);
        onInstantiatingPools.Invoke();
    }

}
