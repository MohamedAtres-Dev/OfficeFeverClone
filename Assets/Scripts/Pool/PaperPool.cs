using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaperPool 
{
    private List<GameObject> pool;
    private GameObject prefab;

    public PaperPool(GameObject prefab, int size , Transform parent)
    {
        this.prefab = prefab;
        this.pool = new List<GameObject>();

        for (int i = 0; i < size; i++)
        {
            GameObject obj = GameObject.Instantiate(prefab);
            obj.transform.SetParent(parent);
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

}
