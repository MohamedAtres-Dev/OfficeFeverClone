using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaperGeneratorZone : Zone
{
    public override void PerformAction(PlayerManager playerManager)
    {
        base.PerformAction(playerManager);
        Debug.Log("Perform Action on Paper Zone ");
        //Here anything related to UI 
        playerManager.CollectPaper();
    }
}
