using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    #region Fields
    [SerializeField] PlayerMovementSettings movementSettings;
    private CharacterController controller;

    
    private Vector2 _inputVector;
    float turnSmoothVelocity;
    Vector3 currentVelocity;
    
    
    #endregion

    #region Monobehaviour

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }


    private void OnEnable()
    {
        PlayerInput.onPlayerMove += OnPlayerMove;
    }

    private void OnDisable()
    {
        PlayerInput.onPlayerMove -= OnPlayerMove;
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
        }
        else
        {
            currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, movementSettings.airResistance);
            controller.Move(currentVelocity * Time.deltaTime);
        }

    }
    #endregion

    private void OnPlayerMove(Vector2 move)
    {
        _inputVector = move;
    }
}
