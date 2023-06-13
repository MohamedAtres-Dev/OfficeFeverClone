using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoneyCollectorZone : Zone
{
    public override void PerformAction(PlayerManager playerManager)
    {
        base.PerformAction(playerManager);
        Debug.Log("Collect Money ");
    }
}
