using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform playerTransform; // The transform of the player to follow
    public float followSpeed = 10f; // The speed at which the camera should follow the player
    public Vector3 offset = new Vector3(0f, 2f, -10f); // The offset from the player's position 
    private void LateUpdate()
    {
        // If the player transform is null, exit early
        if (playerTransform == null)
            return;

        // Calculate the new position of the camera based on the player's position and the offset, and the follow speed
        Vector3 newPosition = Vector3.Lerp(transform.position, playerTransform.position + offset, followSpeed * Time.deltaTime);

        // Set the position of the camera to the new position
        transform.position = newPosition;
    }
}
