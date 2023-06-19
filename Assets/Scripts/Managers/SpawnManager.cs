using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [Header("Paper")]
    [SerializeField] private GameObject paperPrefab;
    [SerializeField] private int paperPoolSize;
    
    [Header("Money")]
    [Space]
    [SerializeField] private GameObject moneyPrefab;
    [SerializeField] private int moneyPoolSize;

    // Start is called before the first frame update
    void Start()
    {
        PoolManager.Instance.InstantiatePool(ObjectPoolTypes.PAPER, paperPrefab, paperPoolSize);
        PoolManager.Instance.InstantiatePool(ObjectPoolTypes.MONEY, moneyPrefab, moneyPoolSize);
    }

}
