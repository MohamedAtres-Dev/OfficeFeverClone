using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerManager))]
public class PlayerInteract : MonoBehaviour
{
    PlayerManager playerManager;

    List<Zone> interactZones = new List<Zone>();

    private void Start()
    {
        playerManager = GetComponent<PlayerManager>();
    }


    private void OnTriggerEnter(Collider other)
    {
        Zone zone = other.GetComponent<Zone>();
        if (zone != null)
        {
            interactZones.Add(zone);
            zone.PerformAction(playerManager);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Zone zone = other.GetComponent<Zone>();
        if (zone != null && interactZones.Contains(zone))
        {
            interactZones.Remove(zone);
            zone.StopAction();
            Debug.Log("Clear Interact ");
        }
    }
}
