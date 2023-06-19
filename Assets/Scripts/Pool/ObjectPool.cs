using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ObjectPool : MonoBehaviour
{
    private List<GameObject> pool;
    private GameObject prefab;

    public void InstantiatePool(GameObject prefab, int size)
    {
        this.prefab = prefab;
        this.pool = new List<GameObject>();

        for (int i = 0; i < size; i++)
        {
            GameObject obj = GameObject.Instantiate(prefab);
            obj.transform.SetParent(transform);
            obj.SetActive(false);
            this.pool.Add(obj);
        }
    }

    public GameObject GetObject()
    {
        foreach (GameObject obj in this.pool)
        {
            if (!obj.activeInHierarchy)
            {
                obj.SetActive(true);
                return obj;
            }
        }

        GameObject newObj = GameObject.Instantiate(prefab);
        newObj.SetActive(true);
        this.pool.Add(newObj);
        return newObj;
    }

    public void ReturnObjectToPool(GameObject obj)
    {
        obj.transform.SetParent(transform);
        obj.SetActive(false);
    }
}


public enum ObjectPoolTypes
{
    NONE,
    PAPER,
    MONEY
}