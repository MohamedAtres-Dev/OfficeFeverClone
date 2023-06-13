using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    
    void Start()
    {
#if !UNITY_EDITOR
        Debug.unityLogger.logEnabled = false; //Disable any debug log for better performance
#endif
    }

    
    void Update()
    {
        
    }
}
