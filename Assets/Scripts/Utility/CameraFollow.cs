using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CameraFollow : MonoBehaviour
{
    public Transform playerTransform; // The transform of the player to follow
    public float followSpeed = 10f; // How quickly the camera closes the gap to the player, per second (higher = tighter). Same result at any frame rate.
    public Vector3 offset = new Vector3(0f, 2f, -10f); // The offset from the player's position

    [Tooltip("Field-of-view change of the celebration punch (degrees). Kept tiny: it is a zoom, never a shake.")]
    [SerializeField] private float punchFov = -3f;

    private Camera cam;
    private float baseFov;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam != null) baseFov = cam.fieldOfView;
    }

    private void OnDisable()
    {
        if (cam == null) return;
        cam.DOKill();
        cam.fieldOfView = baseFov;
    }

    /// <summary>A short zoom-in-and-back on big moments (workstation unlock). Only the FOV moves, never the position.</summary>
    public void Punch()
    {
        if (cam == null) return;
        cam.DOKill();
        cam.fieldOfView = baseFov;
        DOTween.Sequence()
            .Append(cam.DOFieldOfView(baseFov + punchFov, 0.12f).SetEase(Ease.OutQuad))
            .Append(cam.DOFieldOfView(baseFov, 0.35f).SetEase(Ease.OutSine))
            .SetTarget(cam)
            .OnKill(() => { if (cam != null) cam.fieldOfView = baseFov; });
    }

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
