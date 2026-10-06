using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Zone : MonoBehaviour
{
    // Zones are entered/exited constantly, so the base actions do not log (an editor Debug.Log allocates ~10 KB per call).
    public virtual void PerformAction(PlayerManager playerManager)
    {
        //Let the action continue until the player move from this zone
    }

    public virtual void StopAction()
    {
    }
}
