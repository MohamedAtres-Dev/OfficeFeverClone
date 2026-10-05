using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform playerTransform; // The transform of the player to follow
    public float followSpeed = 10f; // How quickly the camera closes the gap to the player, per second (higher = tighter). Same result at any frame rate.
    public Vector3 offset = new Vector3(0f, 2f, -10f); // The offset from the player's position 

    private void LateUpdate()
    {
        // If the player transform is null, exit early
        if (playerTransform == null)
            return;

        // Frame-rate independent damping: the gap shrinks by the same fraction per second at any frame rate.
        // (Lerp with followSpeed * deltaTime gives a different curve per frame rate, and snaps to the target at or below followSpeed fps.)
        float smoothing = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
        Vector3 newPosition = Vector3.Lerp(transform.position, playerTransform.position + offset, smoothing);

        // Set the position of the camera to the new position
        transform.position = newPosition;
    }
}
