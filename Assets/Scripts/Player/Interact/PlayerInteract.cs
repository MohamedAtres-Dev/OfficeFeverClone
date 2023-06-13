using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerManager))]
public class PlayerInteract : MonoBehaviour
{
    PlayerManager playerManager;
    Zone interactZone;

    private void Start()
    {
        playerManager = GetComponent<PlayerManager>();
    }


    private void OnTriggerEnter(Collider other)
    {
        interactZone = other.GetComponent<Zone>();
        if (interactZone != null)
        {
            interactZone.PerformAction(playerManager);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (interactZone != null)
        {
            interactZone.StopAction();
            Debug.Log("Clear Interact ");
            interactZone = null;
        }
    }
}
