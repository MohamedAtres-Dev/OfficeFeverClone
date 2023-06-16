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
    Vector3 currentVelocity;

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
    }

    private void OnDisable()
    {
        PlayerInput.onPlayerMoveInput -= OnGetPlayerMoveInput;
    }

    private void Update()
    {
        RecalculateMovement();
    }
    #endregion

    #region Methods

    private void RecalculateMovement()
    {
        Vector3 direction = new Vector3(_inputVector.x, 0f, _inputVector.y).normalized;

        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, movementSettings.RotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            currentVelocity += (moveDir.normalized * movementSettings.MoveSpeed - currentVelocity) * movementSettings.airResistance;
            controller.Move(currentVelocity * Time.deltaTime);

            //this is for preventing the player to move outside the map
            if (mapBoundsTransform != null)
            {
                Vector3 position = transform.position;
                position.x = Mathf.Clamp(position.x, bounds.min.x, bounds.max.x);
                position.z = Mathf.Clamp(position.z, bounds.min.z, bounds.max.z);
                transform.position = position;
            }
            
            //this is for animation State
            if (!isPlayerMoving)
            {
                onPlayerMove.Invoke(true);
                isPlayerMoving = true;
            }
            
        }
        else
        {
            currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, movementSettings.airResistance);
            controller.Move(currentVelocity * Time.deltaTime);
            
            if (mapBoundsTransform != null)
            {
                Vector3 position = transform.position;
                position.x = Mathf.Clamp(position.x, bounds.min.x, bounds.max.x);
                position.z = Mathf.Clamp(position.z, bounds.min.z, bounds.max.z);
                transform.position = position;
            }

            //this is for animation State
            if (isPlayerMoving)
            {
                onPlayerMove.Invoke(false);
                isPlayerMoving = false;
            }
        }

    }
    #endregion

    private void OnGetPlayerMoveInput(Vector2 move)
    {
        Debug.Log("Move " + move);
        _inputVector = move;
    }
}
