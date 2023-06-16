using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimations : MonoBehaviour
{
    private int isHanging;
    private int isRunning;

    private Animator animator;



    private void Awake()
    {
        isHanging = Animator.StringToHash("IsHanging");
        isRunning = Animator.StringToHash("IsRuning");

        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        PlayerManager.onHangingPaper += PlayerHangingState;
        PlayerMovement.onPlayerMove += PlayerMoveState;
    }

    private void OnDisable()
    {
        PlayerManager.onHangingPaper -= PlayerHangingState;
        PlayerMovement.onPlayerMove -= PlayerMoveState;
    }


    private void PlayerHangingState(bool state)
    {
        animator.SetBool(isHanging, state);
    }


    private void PlayerMoveState(bool moveState)
    {
        animator.SetBool(isRunning, moveState);
    }
}
