using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : Singlton<GameManager>
{
    public GameObject officeWorkerPrefab;
    void Start()
    {
#if !UNITY_EDITOR
        Debug.unityLogger.logEnabled = false; //Disable any debug log for better performance
#endif
    }

    
    public void CreateOffice(Vector3 officePosition)
    {
        Vector3 newPosition = new Vector3(officePosition.x, 1f, officePosition.z);
        Instantiate(officeWorkerPrefab, newPosition, Quaternion.identity);
    }
}
