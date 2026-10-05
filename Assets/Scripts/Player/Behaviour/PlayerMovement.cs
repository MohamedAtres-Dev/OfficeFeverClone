using Lean.Gui;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerMovement : MonoBehaviour
{
    #region Fields
    [SerializeField] PlayerMovementSettings movementSettings;
    [SerializeField] Transform mapBoundsTransform; //this can be a dynamic variable depends on map generating and also we can have like predefined map prefabs with its borders
    //and it's better to have like map manager and the player ask it to get the current map transform
    private Bounds bounds;

    private CharacterController controller;

    
    private Vector2 _inputVector;
    float turnSmoothVelocity;
    float currentSpeed; // speed along the facing direction, so the player never slides sideways
    float verticalVelocity;

    // The controller needs gravity: without it, rubbing along an edge lifts it a few millimetres at a time
    // and it never comes back down. The flat zone triggers then stop detecting the player.
    private const float Gravity = -25f;
    private const float GroundedPush = -2f;

    // Joystick values below this are treated as "no input"; above it the magnitude is rescaled back to 0..1.
    private const float InputDeadZone = 0.1f;
    // Run animation hysteresis on the real (post-collision) speed, in m/s.
    private const float RunStartSpeed = 0.5f;
    private const float RunStopSpeed = 0.25f;

    public static UnityAction<bool> onPlayerMove = delegate { };
    private bool isPlayerMoving;
    #endregion

    #region Monobehaviour

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }
    private void Start()
    {
        if (mapBoundsTransform != null)
        {
            bounds = new Bounds(mapBoundsTransform.position, mapBoundsTransform.GetComponent<MeshRenderer>().bounds.size);
        }
    }


    private void OnEnable()
    {
        PlayerInput.onPlayerMoveInput += OnGetPlayerMoveInput;
        LeanJoystick.onJoystickMove += OnGetPlayerMoveInput;
    }

    private void OnDisable()
    {
        PlayerInput.onPlayerMoveInput -= OnGetPlayerMoveInput;
        LeanJoystick.onJoystickMove -= OnGetPlayerMoveInput;
    }

    private void Update()
    {
        RecalculateMovement();
    }
    #endregion

    #region Methods

    private void RecalculateMovement()
    {
        float dt = Time.deltaTime;

        // Analog input: direction from the stick, strength from how far it is pushed (0..1).
        Vector2 input = Vector2.ClampMagnitude(_inputVector, 1f);
        float inputMagnitude = input.magnitude;
        float strength = inputMagnitude <= InputDeadZone ? 0f : Mathf.Clamp01((inputMagnitude - InputDeadZone) / (1f - InputDeadZone));

        float targetSpeed = 0f;
        if (strength > 0f)
        {
            Vector3 direction = new Vector3(input.x, 0f, input.y).normalized;
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, movementSettings.RotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            // The player only moves where it is facing, so a sharp turn first slows down and rotates
            // instead of sliding sideways. Facing the stick direction gives full speed, 90 degrees or more gives none.
            float alignment = Mathf.Clamp01(Vector3.Dot(transform.forward, direction));
            targetSpeed = movementSettings.MoveSpeed * strength * alignment;
        }

        // Frame-rate independent acceleration and deceleration (m/s per second).
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, movementSettings.SpeedChangeRate * dt);

        Vector3 move = transform.forward * (currentSpeed * dt);

        // Keep the player inside the map by trimming the move itself, not by writing transform.position
        // (writing the position of a CharacterController desyncs it from the physics state).
        if (mapBoundsTransform != null)
        {
            Vector3 position = transform.position;
            move.x = Mathf.Clamp(position.x + move.x, bounds.min.x, bounds.max.x) - position.x;
            move.z = Mathf.Clamp(position.z + move.z, bounds.min.z, bounds.max.z) - position.z;
        }

        // Keep the controller pressed onto the floor so its height always returns to ground level.
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = GroundedPush;
        else
            verticalVelocity += Gravity * dt;
        move.y = verticalVelocity * dt;

        controller.Move(move);

        UpdateMoveState();
    }

    /// <summary>
    /// Run/idle follows the speed the controller really achieved (so pushing into a wall plays idle),
    /// with hysteresis so it cannot flicker around the threshold.
    /// </summary>
    private void UpdateMoveState()
    {
        Vector3 velocity = controller.velocity;
        velocity.y = 0f;
        float actualSpeed = velocity.magnitude;

        if (!isPlayerMoving && actualSpeed > RunStartSpeed)
        {
            isPlayerMoving = true;
            onPlayerMove.Invoke(true);
        }
        else if (isPlayerMoving && actualSpeed < RunStopSpeed)
        {
            isPlayerMoving = false;
            onPlayerMove.Invoke(false);
        }
    }
    #endregion

    public void OnGetPlayerMoveInput(Vector2 move)
    {
        _inputVector = move;
    }
}
