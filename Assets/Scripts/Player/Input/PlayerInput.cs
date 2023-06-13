using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
/// <summary>
/// This script will be the ref for Player Input even from PC or Mobile
/// </summary>
public class PlayerInput : MonoBehaviour, InputActions.IGamePlayActions
{
    #region Events
    public static UnityAction<Vector2> onPlayerMove = delegate { };
    #endregion

    #region Fields
    private InputActions _inputHandler;
    #endregion

    #region Monobehaviour 
    private void OnEnable()
    {
        if (_inputHandler == null)
        {
            _inputHandler = new InputActions();
            _inputHandler.GamePlay.SetCallbacks(this);
        }
        _inputHandler.GamePlay.Enable();
    }

    private void OnDisable()
    {
        _inputHandler.GamePlay.Disable();
    }

    #endregion

    #region Interface Implementation
    public void OnMove(InputAction.CallbackContext context)
    {
        onPlayerMove.Invoke(context.ReadValue<Vector2>());
    }
    #endregion
}
