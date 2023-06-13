using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Zone : MonoBehaviour
{
    public virtual void PerformAction(PlayerManager playerManager)
    {
        Debug.Log("General Actions like Sound");
        //Let the action continue until the player move from this zone 
    }

    public virtual void StopAction()
    {
        Debug.Log("Stop Action ");
    }
}
