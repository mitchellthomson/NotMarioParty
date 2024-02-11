using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class MGAnimationMovementController : MonoBehaviour
{
    MGPlayerInputs _playerInput;
    CharacterController _characterController;
    Animator _animator;

    int _isWalkingHash;
    int _isRunningHash;
    int _isJumpingHash;
    int _jumpCountHash;
    Vector2 _currentMovementInput;
    Vector3 _currentMovement;
    Vector3 _currentRunMovement;
    Vector3 _appliedMovement;
    float _rotFactorPerFrame = 1.0f;
    bool _isMovementPressed;
    bool _isRunPressed;
    bool _isJumpPressed;
    bool _isJumping;
    bool _isJumpAnimating = false;
    int _jumpCount = 0;
    Dictionary<int, float> _initalJumpVelocities = new Dictionary<int, float>();
    Dictionary<int, float> _initalJumpGravities= new Dictionary<int, float>();
    Coroutine _currentJumpResetRoutine = null;

    [Header("Walking")]
    [SerializeField] float _runMultiplier = 3.0f;

    [Header("Gravity")]
    [SerializeField] float _groundedGravity = -.05f;

    [SerializeField] float _gravity = -9.8f;

    [Header("Jumps")]
    [SerializeField] float _initalJumpVelocity;
    [SerializeField] float _maxJumpHeight= 2.0f;
    [SerializeField] float _maxJumpTime =0.75f;
    

    

    void Awake()
    {
        _playerInput = new MGPlayerInputs();
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();

        _isWalkingHash = Animator.StringToHash("isWalking");
        _isRunningHash = Animator.StringToHash("isRunning");
        _isJumpingHash = Animator.StringToHash("isJumping");
        _jumpCountHash = Animator.StringToHash("jumpCount");

        _playerInput.CharacterControls.Move.started += OnMovementInput;
        _playerInput.CharacterControls.Move.canceled += OnMovementInput;
        _playerInput.CharacterControls.Move.performed += OnMovementInput;
        _playerInput.CharacterControls.Run.started += onRun;
        _playerInput.CharacterControls.Run.canceled += onRun;

        _playerInput.CharacterControls.Jump.started += onJump;
        _playerInput.CharacterControls.Jump.canceled += onJump;

        SetupJumpVariables();
    }

    void SetupJumpVariables()
    {
        float timeToApex = _maxJumpTime / 2;
        _gravity = -2 * _maxJumpHeight / Mathf.Pow(timeToApex,2);
        _initalJumpVelocity = 2 * _maxJumpHeight / timeToApex;

        //dict stuff below
        float secondJumpGravity = -2 * (_maxJumpHeight + 2) / Mathf.Pow(timeToApex * 1.25f,2);
        float secondJumpInitialVelocity = 2 *(_maxJumpHeight + 2) / (timeToApex * 1.25f);
        float thirdJumpGravity = -2 * (_maxJumpHeight + 4) / Mathf.Pow(timeToApex * 1.25f,2);
        float thirdJumpInitialVelocity = 2 *(_maxJumpHeight + 4) / (timeToApex * 1.25f);

        _initalJumpVelocities.Add(1, _initalJumpVelocity);
        _initalJumpVelocities.Add(2, secondJumpInitialVelocity);
        _initalJumpVelocities.Add(3, thirdJumpInitialVelocity);

        _initalJumpGravities.Add(0, _gravity);
        _initalJumpGravities.Add(1, _gravity);
        _initalJumpGravities.Add(2, secondJumpGravity);
        _initalJumpGravities.Add(3, thirdJumpGravity);
    }

    void HandleJump()
    {
        if (!_isJumping && _characterController.isGrounded && _isJumpPressed)
        {
            if(_jumpCount <3 && _currentJumpResetRoutine !=null)
            {
                StopCoroutine(_currentJumpResetRoutine);
            }
            _animator.SetBool(_isJumpingHash,true);
            _isJumpAnimating = true;
            _isJumping = true;
            _jumpCount += 1;
            _animator.SetInteger(_jumpCountHash,_jumpCount);
            
            //Jump movement without 3 jump combo
            //_currentMovement.y = initalJumpVelocity * 0.5f;
            //_currentRunMovement.y = initalJumpVelocity * 0.5f;

            _currentMovement.y = _initalJumpVelocities[_jumpCount];
            _appliedMovement.y = _initalJumpVelocities[_jumpCount];
        }
        else if(!_isJumpPressed && _isJumping && _characterController.isGrounded)
        {
            _isJumping = false;
        }
    }

    IEnumerator JumpResetRoutine()
    {
        yield return new WaitForSeconds(.5f);
        _jumpCount = 0;
    }
    void onJump(InputAction.CallbackContext context)
    {
        _isJumpPressed = context.ReadValueAsButton();
    }

    void onRun(InputAction.CallbackContext context)
    {
        _isRunPressed = context.ReadValueAsButton();
    }

    void RotationHandler()
    {
        Vector3 posToLookAt;
        posToLookAt.x = _currentMovement.x;
        posToLookAt.y = 0.0f;
        posToLookAt.z = _currentMovement.z;

        Quaternion curRot = transform.rotation;

        if(_isMovementPressed)
        {
            Quaternion targetRot = Quaternion.LookRotation(posToLookAt);
            transform.rotation = Quaternion.Slerp(curRot, targetRot, _rotFactorPerFrame);
        }

    }

    void OnMovementInput(InputAction.CallbackContext context)
    {
            _currentMovementInput = context.ReadValue<Vector2>();
            _currentMovement.x = _currentMovementInput.x;
            _currentMovement.z = _currentMovementInput.y;
            _currentRunMovement.x = _currentMovementInput.x * _runMultiplier;
            _currentRunMovement.z = _currentMovementInput.y * _runMultiplier;
            _isMovementPressed = _currentMovementInput.x !=0 || _currentMovementInput.y != 0;
    }

    void AnimationHandler()
    {
        bool isWalking = _animator.GetBool(_isWalkingHash);
        bool isRunning = _animator.GetBool(_isRunningHash);

        if(_isMovementPressed && !isWalking)
        {
            _animator.SetBool(_isWalkingHash, true);
        }
        else if(!_isMovementPressed && isWalking)
        {
            _animator.SetBool(_isWalkingHash,false);
        }

        if((_isMovementPressed && _isRunPressed) && !isRunning)
        {
            _animator.SetBool(_isRunningHash, true);
        }
        else if((!_isMovementPressed || !_isRunPressed) && isRunning)
        {
            _animator.SetBool(_isRunningHash, false);
        }
    }

    void HandleGravity()
    {
        bool isFalling = _currentMovement.y <= 0.0f || !_isJumpPressed;
        float fallMultiplier = 1.5f;
        float prevYVelocity;
        

        if(_characterController.isGrounded)
        {
            if(_isJumpAnimating)
            {
                _animator.SetBool(_isJumpingHash,false);
                _isJumpAnimating = false;
                _currentJumpResetRoutine = StartCoroutine(JumpResetRoutine());
                if(_jumpCount == 3)
                {
                    _jumpCount = 0;
                    _animator.SetInteger(_jumpCountHash, _jumpCount);
                }
            }
            _currentMovement.y = _groundedGravity;
            _appliedMovement.y = _groundedGravity;
        }
        else if(isFalling)
        {
            prevYVelocity = _currentMovement.y;
            _currentMovement.y = _currentMovement.y + (_initalJumpGravities[_jumpCount]* fallMultiplier * Time.deltaTime);
            _appliedMovement.y = Mathf.Max((prevYVelocity + _currentMovement.y) *.5f, -20.0f);
        }
        else
        {
            prevYVelocity = _currentMovement.y;
            _currentMovement.y = _currentMovement.y + (_initalJumpGravities[_jumpCount]* fallMultiplier * Time.deltaTime);
            _appliedMovement.y = (prevYVelocity + _currentMovement.y) * .5f;
        }
    }
    void Update()
    {   
        AnimationHandler();
        RotationHandler();
        if(_isRunPressed)
        {
            _appliedMovement.x = _currentRunMovement.x;
            _appliedMovement.z = _currentRunMovement.z;
        }
        else
        {
            _appliedMovement.x = _currentMovement.x;
            _appliedMovement.z = _currentMovement.z;
        }

        _characterController.Move(_appliedMovement * Time.deltaTime);
        HandleGravity();
        HandleJump();
        
    }

    void OnEnable()
    {
        _playerInput.CharacterControls.Enable();
    }

    void OnDisable()
    {
        _playerInput.CharacterControls.Disable();
    }
}
