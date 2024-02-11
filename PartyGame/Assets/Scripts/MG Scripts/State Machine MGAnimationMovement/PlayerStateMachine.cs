using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStateMachine : MonoBehaviour
{
    CharacterController _characterController;
    Animator _animator;
    MGPlayerInputs _playerInput;
    int _isWalkingHash;
    int _isRunningHash;

    Vector2 _currentMovementInput;
    Vector3 _currentMovement;
    Vector3 _appliedMovement;
    bool _isMovementPressed;
    bool _isRunPressed;

    float _rotFactorPerFrame = 15.0f;
    [SerializeField] float _runMultiplier = 3.0f;
    int _zero = 0;

    [SerializeField] float _gravity = -9.8f;
    [SerializeField] float _groundedGravity = -.05f;

    
    bool _isJumpPressed = false;
    [SerializeField] float _initalJumpVelocity;
    [SerializeField] float _maxJumpHeight= 2.0f;
    [SerializeField] float _maxJumpTime =0.75f;
    bool _isJumping = false;
    int _isJumpingHash;
    int _jumpCountHash;
    bool _requireNewJumpPress = false;
    int _jumpCount = 0;
    Dictionary<int, float> _initalJumpVelocities = new Dictionary<int, float>();
    Dictionary<int, float> _jumpGravities= new Dictionary<int, float>();
    Coroutine _currentJumpResetRoutine = null;

    PlayerBaseState _currentState;
    PlayerStateFactory _states;

    public PlayerBaseState CurrentState { get { return _currentState; } set { _currentState = value; }}
    public Animator Animator { get { return _animator; }}
    public Coroutine CurrentJumpResetRoutine { get { return _currentJumpResetRoutine; } set { _currentJumpResetRoutine = value; }}
    public CharacterController CharacterController{ get { return _characterController; }}
    public Dictionary<int, float> InitalJumpVelocities { get { return _initalJumpVelocities; }}
    public Dictionary<int, float> JumpGravities { get { return _jumpGravities; }}
    public int JumpCount { get { return _jumpCount; } set { _jumpCount = value; }}
    public int IsWalkingHash { get { return _isWalkingHash; }}
    public int IsRunningHash { get { return _isRunningHash; }}
    public int IsJumpingHash { get {return _isJumpingHash; }}
    public int JumpCountHash { get {return _jumpCountHash; }}
    public bool IsMovementPressed { get { return _isMovementPressed; }}
    public bool IsRunPressed { get { return _isRunPressed; }}
    public bool RequireNewJumpPress { get { return _requireNewJumpPress; } set { _requireNewJumpPress= value; }}
    public bool IsJumping { set { _isJumping = value; }}
    public bool IsJumpPressed { get { return _isJumpPressed; }}
    public float GroundedGravity { get { return _groundedGravity; } set { _groundedGravity= value; }}
    public float CurrentMovementY { get { return _currentMovement.y; } set {_currentMovement.y = value;}}
    public float AppliedMovementX { get { return _appliedMovement.x; } set {_appliedMovement.x = value;}}
    public float AppliedMovementY { get { return _appliedMovement.y; } set {_appliedMovement.y = value;}}
    public float AppliedMovementZ { get { return _appliedMovement.z; } set {_appliedMovement.z = value;}}
    public float RunMultiplier { get { return _runMultiplier;}}
    public Vector2 CurrentMovementInput { get { return _currentMovementInput;}}


    void Awake()
    {
        _playerInput = new MGPlayerInputs();
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();

        _states = new PlayerStateFactory(this);
        _currentState = _states.Grounded();
        _currentState.EnterState();

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

        _jumpGravities.Add(0, _gravity);
        _jumpGravities.Add(1, _gravity);
        _jumpGravities.Add(2, secondJumpGravity);
        _jumpGravities.Add(3, thirdJumpGravity);
    }

    void Update()
    {
        RotationHandler();
        _characterController.Move(_appliedMovement * Time.deltaTime);
        _currentState.UpdateStates();
        
        
    }
    void RotationHandler()
    {
        Vector3 posToLookAt;
        posToLookAt.x = _currentMovementInput.x;
        posToLookAt.y = _zero;
        posToLookAt.z = _currentMovementInput.y;

        Quaternion curRot = transform.rotation;

        if(_isMovementPressed)
        {
            Quaternion targetRot = Quaternion.LookRotation(posToLookAt);
            transform.rotation = Quaternion.Slerp(curRot, targetRot, _rotFactorPerFrame * Time.deltaTime);
        }

    }
    void OnMovementInput(InputAction.CallbackContext context)
    {
            _currentMovementInput = context.ReadValue<Vector2>();
            // _currentMovement.x = _currentMovementInput.x;
            // _currentMovement.z = _currentMovementInput.y;
            // _currentRunMovement.x = _currentMovementInput.x * _runMultiplier;
            // _currentRunMovement.z = _currentMovementInput.y * _runMultiplier;
            _isMovementPressed = _currentMovementInput.x !=0 || _currentMovementInput.y != 0;
    }

    void onJump(InputAction.CallbackContext context)
    {
        _isJumpPressed = context.ReadValueAsButton();
        _requireNewJumpPress = false;
    }

    void onRun(InputAction.CallbackContext context)
    {
        _isRunPressed = context.ReadValueAsButton();
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
