using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

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
                ResetObject(obj); // defensive: a reused object always starts from its prefab state
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
        ResetObject(obj);
        obj.SetActive(false);
    }

    /// <summary>
    /// Stops any tween still running on the object and restores the scale and rotation a tween (for example a
    /// pickup pop) may have left behind, so the next user of the pooled object never inherits them.
    /// </summary>
    private void ResetObject(GameObject obj)
    {
        Transform objTransform = obj.transform;
        objTransform.DOKill();

        if (prefab != null)
        {
            objTransform.localScale = prefab.transform.localScale;
            objTransform.localRotation = prefab.transform.localRotation;
        }
    }
}


public enum ObjectPoolTypes
{
    NONE,
    PAPER,
    MONEY
}