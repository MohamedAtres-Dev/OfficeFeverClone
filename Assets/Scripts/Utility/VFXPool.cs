using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Minimal pool for one-shot particle effects. Keyed by prefab, so any VFX prefab can be played without
/// scene setup. Effects are returned to the pool by themselves when they finish (see PooledVFX).
/// </summary>
public class VFXPool : Singlton<VFXPool>
{
    private readonly Dictionary<PooledVFX, Stack<PooledVFX>> idle = new Dictionary<PooledVFX, Stack<PooledVFX>>();

    /// <summary>
    /// Plays the effect once at the given world position, or, when follow is given, keeps it at
    /// follow.position + followOffset for its whole duration. Safe to call with a null prefab.
    /// </summary>
    public void Play(PooledVFX prefab, Vector3 position, Transform follow = null, Vector3 followOffset = default)
    {
        if (prefab == null) return;

        if (!idle.TryGetValue(prefab, out Stack<PooledVFX> stack))
        {
            stack = new Stack<PooledVFX>();
            idle[prefab] = stack;
        }

        PooledVFX instance = null;
        while (instance == null && stack.Count > 0)
            instance = stack.Pop(); // skip instances destroyed externally (e.g. by a scene unload)

        if (instance == null)
            instance = Instantiate(prefab, transform);

        instance.transform.SetPositionAndRotation(position, Quaternion.identity);
        instance.Begin(() => stack.Push(instance), follow, followOffset);
    }
}
