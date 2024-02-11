using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerJumpState : PlayerBaseState
{
    IEnumerator IJumpResetRoutine()
    {
        yield return new WaitForSeconds(.5f);
        Ctx.JumpCount = 0;
    }

    public PlayerJumpState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory): base (currentContext, playerStateFactory)
    {
        IsRootState = true;
        InitializeSubState();
    }
    public override void EnterState()
    {
        HandleJump();
        Debug.Log("Jump State");
    }
    public override void UpdateState()
    {
        CheckSwitchStates();
        HandleGravity();
        Debug.Log("Update Jump");
    }
    public override void ExitState()
    {
        Debug.Log("Exit Jump");
        Ctx.Animator.SetBool(Ctx.IsJumpingHash, false);

        if(Ctx.IsJumpPressed)
        {
            Ctx.RequireNewJumpPress = true;
        }
        
        Ctx.CurrentJumpResetRoutine = Ctx.StartCoroutine(IJumpResetRoutine());

        if(Ctx.JumpCount == 3)
        {
            Ctx.JumpCount = 0;
            Ctx.Animator.SetInteger(Ctx.JumpCountHash, Ctx.JumpCount);
        }
    }
    public override void CheckSwitchStates()
    {
        if(Ctx.CharacterController.isGrounded)
        {
            SwitchState(Factory.Grounded());
        }
    }
    public override void InitializeSubState()
    {
        if(!Ctx.IsMovementPressed && !Ctx.IsRunPressed)
        {
            SetSubState(Factory.Idle());
        }
        else if (Ctx.IsMovementPressed && !Ctx.IsRunPressed)
        {
            SetSubState(Factory.Walk());
        }
        else
        {
            SetSubState(Factory.Run());
        }
    }

    void HandleJump()
    {
        if(Ctx.JumpCount <3 && Ctx.CurrentJumpResetRoutine !=null)
            {
                Ctx.StopCoroutine(Ctx.CurrentJumpResetRoutine);
            }
        Ctx.Animator.SetBool(Ctx.IsJumpingHash, true);
        Ctx.IsJumping = true;
        Ctx.JumpCount += 1;
        Ctx.Animator.SetInteger(Ctx.JumpCountHash,Ctx.JumpCount);
        
        //Jump movement without 3 jump combo
        //Ctx.currentMovement.y = initalJumpVelocity * 0.5f;
        //Ctx.currentRunMovement.y = initalJumpVelocity * 0.5f;

        Ctx.CurrentMovementY = Ctx.InitalJumpVelocities[Ctx.JumpCount];
        Ctx.AppliedMovementY = Ctx.InitalJumpVelocities[Ctx.JumpCount];
    }

    void HandleGravity()
    {
        bool isFalling = Ctx.CurrentMovementY <= 0.0f || !Ctx.IsJumpPressed;
        float fallMultiplier = 2.0f;
        
        if(isFalling)
        {
            float prevYVelocity = Ctx.CurrentMovementY;
            Ctx.CurrentMovementY = Ctx.CurrentMovementY + (Ctx.JumpGravities[Ctx.JumpCount]* fallMultiplier * Time.deltaTime);
            Ctx.AppliedMovementY = Mathf.Max((prevYVelocity + Ctx.CurrentMovementY) *.5f, -20.0f);
        }
        else
        {
            float prevYVelocity = Ctx.CurrentMovementY;
            Ctx.CurrentMovementY = Ctx.CurrentMovementY + (Ctx.JumpGravities[Ctx.JumpCount]* fallMultiplier * Time.deltaTime);
            Ctx.AppliedMovementY = (prevYVelocity + Ctx.CurrentMovementY) * .5f;
        }
    }
}
