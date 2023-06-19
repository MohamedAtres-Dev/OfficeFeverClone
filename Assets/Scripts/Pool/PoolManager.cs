using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : Singlton<PoolManager>
{
    public GenericDictionary<ObjectPoolTypes, ObjectPool> allObjectPools = new GenericDictionary<ObjectPoolTypes, ObjectPool>();

    public void InstantiatePool(ObjectPoolTypes poolType, GameObject prefab, int size)
    {
        if (allObjectPools.ContainsKey(poolType))
            allObjectPools[poolType].InstantiatePool(prefab, size);
    }

    public GameObject GetObjectFromPool(ObjectPoolTypes poolType)
    {
        if (!allObjectPools.ContainsKey(poolType)) return null;
        return allObjectPools[poolType].GetObject();
    }

    public void ReturnObjectToPool(ObjectPoolTypes poolType, GameObject obj)
    {
        if (allObjectPools.ContainsKey(poolType))
            allObjectPools[poolType].ReturnObjectToPool(obj);
    }
}
