using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "PlayerMovementSettings", menuName = "Data/PlayerMovementSettings")]
public class PlayerMovementSettings : ScriptableObject
{
	[Tooltip("Move speed of the character in m/s")]
	public float MoveSpeed = 2.0f;
	[Tooltip("Sprint speed of the character in m/s")]
	public float SprintSpeed = 5.335f;
	[Tooltip("How fast the character turns to face movement direction")]
	[Range(0.0f, 0.3f)]
	public float RotationSmoothTime = 0.12f;
	[Tooltip("Acceleration and deceleration in m/s per second. Higher = snappier start and stop.")]
	public float SpeedChangeRate = 50.0f;

    [Tooltip("No longer used. Kept so existing assets keep their serialized data; use SpeedChangeRate.")]
    public float airResistance = 0.1f;
}
