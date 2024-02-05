using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class MGAnimationMovementController : MonoBehaviour
{
    MGPlayerInputs playerInput;
    CharacterController characterController;
    Animator animator;

    int isWalkingHash;
    int isRunningHash;
    int isJumpingHash;
    int jumpCountHash;
    Vector2 currentMovementInput;
    Vector3 currentMovement;
    Vector3 currentRunMovement;
    float rotFactorPerFrame = 1.0f;
    bool isMovementPressed;
    bool isRunPressed;
    bool isJumpPressed;
    bool isJumping;
    bool isJumpAnimating = false;
    Dictionary<int, float> initalJumpVelocities = new Dictionary<int, float>();
    Dictionary<int, float> initalJumpGravities= new Dictionary<int, float>();
    Coroutine currentJumpResetRoutine = null;

    [Header("Walking")]
    [SerializeField] float runMultiplier = 3.0f;

    [Header("Gravity")]
    [SerializeField] float groundedGravity = -.05f;

    [SerializeField] float gravity = -9.8f;

    [Header("Jumps")]
    [SerializeField] float initalJumpVelocity;
    [SerializeField] float maxJumpHeight= 4.0f;
    [SerializeField] float maxJumpTime =0.75f;
    [SerializeField] int jumpCount = 0;

    

    void Awake()
    {
        playerInput = new MGPlayerInputs();
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        isWalkingHash = Animator.StringToHash("isWalking");
        isRunningHash = Animator.StringToHash("isRunning");
        isJumpingHash = Animator.StringToHash("isJumping");
        jumpCountHash = Animator.StringToHash("jumpCount");

        playerInput.CharacterControls.Move.started += onMovementInput;
        playerInput.CharacterControls.Move.canceled += onMovementInput;
        playerInput.CharacterControls.Move.performed += onMovementInput;
        playerInput.CharacterControls.Run.started += onRun;
        playerInput.CharacterControls.Run.canceled += onRun;

        playerInput.CharacterControls.Jump.started += onJump;
        playerInput.CharacterControls.Jump.canceled += onJump;

        setupJumpVariables();
    }

    void setupJumpVariables()
    {
        float timeToApex = maxJumpTime / 2;
        gravity = -2 * maxJumpHeight / Mathf.Pow(timeToApex,2);
        initalJumpVelocity = 2 * maxJumpHeight / timeToApex;

        //dict stuff below
        float secondJumpGravity = -2 * (maxJumpHeight + 2) / Mathf.Pow(timeToApex * 1.25f,2);
        float secondJumpInitialVelocity = 2 *(maxJumpHeight + 2) / (timeToApex * 1.25f);
        float thirdJumpGravity = -2 * (maxJumpHeight + 4) / Mathf.Pow(timeToApex * 1.25f,2);
        float thirdJumpInitialVelocity = 2 *(maxJumpHeight + 4) / (timeToApex * 1.25f);

        initalJumpVelocities.Add(1, initalJumpVelocity);
        initalJumpVelocities.Add(2, secondJumpInitialVelocity);
        initalJumpVelocities.Add(3, thirdJumpInitialVelocity);

        initalJumpGravities.Add(0, gravity);
        initalJumpGravities.Add(1, gravity);
        initalJumpGravities.Add(2, secondJumpGravity);
        initalJumpGravities.Add(3, thirdJumpGravity);
    }

    void handleJump()
    {
        if (!isJumping && characterController.isGrounded && isJumpPressed)
        {
            if(jumpCount <3 && currentJumpResetRoutine !=null)
            {
                StopCoroutine(currentJumpResetRoutine);
            }
            animator.SetBool(isJumpingHash,true);
            isJumpAnimating = true;
            isJumping = true;
            jumpCount += 1;
            animator.SetInteger(jumpCountHash,jumpCount);
            
            //Jump movement without 3 jump combo
            //currentMovement.y = initalJumpVelocity * 0.5f;
            //currentRunMovement.y = initalJumpVelocity * 0.5f;

            currentMovement.y = initalJumpVelocities[jumpCount] * 0.5f;
            currentRunMovement.y = initalJumpVelocities[jumpCount]* 0.5f;
        }
        else if(!isJumpPressed && isJumping && characterController.isGrounded)
        {
            isJumping = false;
        }
    }

    IEnumerator jumpResetRoutine()
    {
        yield return new WaitForSeconds(.5f);
        jumpCount = 0;
    }
    void onJump(InputAction.CallbackContext context)
    {
        isJumpPressed = context.ReadValueAsButton();
    }

    void onRun(InputAction.CallbackContext context)
    {
        isRunPressed = context.ReadValueAsButton();
    }

    void rotationHandler()
    {
        Vector3 posToLookAt;
        posToLookAt.x = currentMovement.x;
        posToLookAt.y = 0.0f;
        posToLookAt.z = currentMovement.z;

        Quaternion curRot = transform.rotation;

        if(isMovementPressed)
        {
            Quaternion targetRot = Quaternion.LookRotation(posToLookAt);
            transform.rotation = Quaternion.Slerp(curRot, targetRot, rotFactorPerFrame);
        }

    }

    void onMovementInput(InputAction.CallbackContext context)
    {
            currentMovementInput = context.ReadValue<Vector2>();
            currentMovement.x = currentMovementInput.x;
            currentMovement.z = currentMovementInput.y;
            currentRunMovement.x = currentMovementInput.x * runMultiplier;
            currentRunMovement.z = currentMovementInput.y * runMultiplier;
            isMovementPressed = currentMovementInput.x !=0 || currentMovementInput.y != 0;
    }

    void animationHandler()
    {
        bool isWalking = animator.GetBool(isWalkingHash);
        bool isRunning = animator.GetBool(isRunningHash);

        if(isMovementPressed && !isWalking)
        {
            animator.SetBool(isWalkingHash, true);
        }
        else if(!isMovementPressed && isWalking)
        {
            animator.SetBool(isWalkingHash,false);
        }

        if((isMovementPressed && isRunPressed) && !isRunning)
        {
            animator.SetBool(isRunningHash, true);
        }
        else if((!isMovementPressed || !isRunPressed) && isRunning)
        {
            animator.SetBool(isRunningHash, false);
        }
    }

    void handleGravity()
    {
        bool isFalling = currentMovement.y <= 0.0f || !isJumpPressed;
        float fallMultiplier = 1.5f;
        float prevYVelocity;
        float newYVelocity;
        float nextYVelocity;

        if(characterController.isGrounded)
        {
            if(isJumpAnimating)
            {
                animator.SetBool(isJumpingHash,false);
                isJumpAnimating = false;
                currentJumpResetRoutine = StartCoroutine(jumpResetRoutine());
                if(jumpCount == 3)
                {
                    jumpCount = 0;
                    animator.SetInteger(jumpCountHash, jumpCount);
                }
            }
            currentMovement.y = groundedGravity;
            currentRunMovement.y = groundedGravity;
        }
        else if(isFalling)
        {
            prevYVelocity = currentMovement.y;
            newYVelocity = currentMovement.y + (initalJumpGravities[jumpCount]* fallMultiplier * Time.deltaTime);
            nextYVelocity = (prevYVelocity + newYVelocity) *0.5f;
            currentMovement.y = nextYVelocity;
            currentRunMovement.y = nextYVelocity;
        }
        else
        {
            prevYVelocity = currentMovement.y;
            newYVelocity = currentMovement.y + (initalJumpGravities[jumpCount] * Time.deltaTime);
            nextYVelocity = (prevYVelocity + newYVelocity) *0.5f;
            currentMovement.y = nextYVelocity;
            currentRunMovement.y = nextYVelocity;
        }
    }
    void Update()
    {   
        animationHandler();
        rotationHandler();
        if(isRunPressed)
        {
            characterController.Move(currentRunMovement * Time.deltaTime);
        }
        else
        {
            characterController.Move(currentMovement * Time.deltaTime);
        }
        handleGravity();
        handleJump();
        
    }

    void OnEnable()
    {
        playerInput.CharacterControls.Enable();
    }

    void OnDisable()
    {
        playerInput.CharacterControls.Disable();
    }
}
