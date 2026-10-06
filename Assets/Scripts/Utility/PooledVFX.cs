using System;
using UnityEngine;

/// <summary>
/// Root component of a one-shot VFX prefab. Plays every child particle system, then deactivates itself
/// once the longest one has finished so VFXPool can reuse it. Optionally follows a transform while it plays
/// (the prefab's particle systems should then use Local simulation space).
/// </summary>
public class PooledVFX : MonoBehaviour
{
    private ParticleSystem[] systems;
    private float totalTime;
    private float elapsed;
    private Action onFinished;
    private Transform followTarget;
    private Vector3 followOffset;

    private void Awake()
    {
        systems = GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem system in systems)
        {
            ParticleSystem.MainModule main = system.main;
            totalTime = Mathf.Max(totalTime, main.startDelay.constantMax + Mathf.Max(main.duration, main.startLifetime.constantMax));
        }
        enabled = false;
    }

    public void Begin(Action finished, Transform follow = null, Vector3 offset = default)
    {
        onFinished = finished;
        followTarget = follow;
        followOffset = offset;
        elapsed = 0f;
        gameObject.SetActive(true);

        foreach (ParticleSystem system in systems)
        {
            system.Clear(true);
            system.Play(false);
        }
        enabled = true;
    }

    // Runs after the player has moved this frame, so a followed effect never trails behind.
    private void LateUpdate()
    {
        if (followTarget == null) return;
        if (!followTarget.gameObject.activeInHierarchy)
        {
            followTarget = null;
            return;
        }
        transform.position = followTarget.position + followOffset;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed < totalTime) return;

        enabled = false;
        followTarget = null;
        foreach (ParticleSystem system in systems)
            system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

        gameObject.SetActive(false);
        onFinished?.Invoke();
        onFinished = null;
    }
}
