using Assets.Scripts.Areas.Character.Mono;
using Assets.Scripts.Areas.Shared.Mono;
using Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Areas.Shared.UI;



#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : NetworkBehaviour
    {
        [Header("Player")]
        [Tooltip("Lock speed of the character in m/s")]
        public float LockSpeed = 3f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 80.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        private float _cameraAngleOverride;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;
        private CinemachineVirtualCamera _cinemachineVirtualCamera;
        private TargetSelector _targetSelector;

        [Header("Camera Zoom")]
        [SerializeField] private float _minZoom = 0f;
        [SerializeField] private float _maxZoom = 8f;
        [SerializeField] private float _zoomSpeed = 0.1f;

        private Cinemachine3rdPersonFollow _thirdPersonFollow;
        private float _currentZoom;
        private GameObject _geometry;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

        private Vector3? _lastMousePos = null;

        private Transform _lockTarget = null;
        [SerializeField] private float _lockLerpSpeed = 6f;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
                return false;
#endif
            }
        }

        private void Awake()
        {
            // get a reference to our main camera
            _mainCamera = transform.parent.Find("MainCamera").gameObject;
            _cinemachineVirtualCamera = transform.parent.Find("PlayerFollowCamera").GetComponent<CinemachineVirtualCamera>();
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

            _animator = GetComponent<Animator>();
            _hasAnimator = _animator != null;
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            UnityEngine.Cursor.visible = true;
            UnityEngine.Cursor.lockState = CursorLockMode.None;

            if (IsOwner)
            {
                var audioSource = GetComponent<AudioSource>();
                audioSource.enabled = true;
                AudioManager.Instance.Init(audioSource);

                _thirdPersonFollow = _cinemachineVirtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
                _currentZoom = _thirdPersonFollow.CameraDistance;
                _geometry = transform.Find("Geometry").gameObject;
                _targetSelector = GetComponent<TargetSelector>();
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

#if ENABLE_INPUT_SYSTEM
            if (IsClient && IsOwner)
            {
                _playerInput = GetComponent<PlayerInput>();
                _playerInput.enabled = true;
#endif
                _mainCamera.SetActive(true);
                transform.parent.Find("PlayerFollowCamera").gameObject.SetActive(true);
                transform.parent.Find("UI_EventSystem").gameObject.SetActive(true);
                _cinemachineVirtualCamera.Follow = transform.Find("PlayerCameraRoot");
#if ENABLE_INPUT_SYSTEM
            }
#endif
        }

        private void Update()
        {
            if (IsOwner && !ChatUI.Instance.InputField.isFocused)
            {
                JumpAndGravity();
                GroundedCheck();
                Move();
                HandleCusor();
                HandleZoom();
            }
        }

        private void HandleCusor()
        {
            if (_input.Rotate && Cursor.lockState == CursorLockMode.None)
            {
                if (_lastMousePos == null)
                {
                    _lastMousePos = Mouse.current.position.ReadValue();
                }

                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }

            if (!_input.Rotate && Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;

                if (_lastMousePos.HasValue)
                {
                    Mouse.current.WarpCursorPosition(_lastMousePos.Value);
                    _lastMousePos = null;
                }
            }
        }

        private void HandleZoom()
        {
            if (IsNotScrollReact())
            {
                return;
            }

            float scroll = Mouse.current.scroll.ReadValue().y;

            if (Mathf.Abs(scroll) > 0.1f)
            {
                _currentZoom -= scroll * _zoomSpeed * Time.deltaTime * 100f;
                _currentZoom = Mathf.Clamp(_currentZoom, _minZoom, _maxZoom);

                var distance = Mathf.Lerp(_thirdPersonFollow.CameraDistance, _currentZoom, Time.deltaTime * 10f);

                if (distance > 0)
                {
                    _thirdPersonFollow.CameraDistance = distance;

                    _geometry.SetActive(distance > 0.5f);
                }
            }
        }

        private bool IsNotScrollReact()
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            var eventData = new PointerEventData(EventSystem.current)
            {
                position = Mouse.current.position.ReadValue()
            };

            var results = new List<RaycastResult>();

            EventSystem.current.RaycastAll(eventData, results);

            return results
                .Where(x => x.gameObject != null)
                .Where(x => x.gameObject.GetComponentInParent<ScrollRect>() != null)
                .Any();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
        {
            if (_lockTarget != null)
            {
                if (_input.Rotate && _input.Look.sqrMagnitude >= _threshold)
                {
                    if (_lockTarget.tag == "Target")
                    {
                        _targetSelector.HandleUnselect();
                        _targetSelector.UnselectServerRpc();
                    }

                    UnlockCamera();

                    return;
                }

                Vector3 origin = CinemachineCameraTarget.transform.position;
                Vector3 dir = (_lockTarget.position - origin).normalized;

                if (dir.sqrMagnitude > 0.0001f)
                {
                    Quaternion desired = Quaternion.LookRotation(dir);

                    // clamp pitch
                    Vector3 e = desired.eulerAngles;
                    float pitch = e.x;

                    if (pitch > 180f)
                    {
                        pitch -= 360f;
                    }

                    pitch = Mathf.Clamp(pitch, BottomClamp, TopClamp);
                    desired = Quaternion.Euler(pitch + _cameraAngleOverride, e.y, 0f);

                    CinemachineCameraTarget.transform.rotation = Quaternion.Slerp(CinemachineCameraTarget.transform.rotation, desired, Time.deltaTime * _lockLerpSpeed);

                    // keep yaw/pitch consistent with current rotation so other code can read them
                    Vector3 cur = CinemachineCameraTarget.transform.rotation.eulerAngles;
                    _cinemachineTargetYaw = cur.y;
                    float curPitch = cur.x;

                    if (curPitch > 180f)
                    {
                        curPitch -= 360f;
                    }

                    _cinemachineTargetPitch = curPitch - _cameraAngleOverride;
                }

                return;
            }

            if (_input.Rotate && _input.Look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.Look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.Look.y * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + _cameraAngleOverride, _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.Sprint ? SprintSpeed : LockSpeed;

            // if there is no input, set the target speed to 0
            if (_input.Move == Vector2.zero)
            {
                targetSpeed = 0.0f;
            }

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.AnalogMovement ? _input.Move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.Move.x, 0.0f, _input.Move.y).normalized;

            // When locked on to a target, move relative to the target axis and rotate to face the target.
            if (_lockTarget != null)
            {
                // compute yaw of the vector from player -> target
                Vector3 toTarget = (_lockTarget.position - transform.position);
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude < 0.0001f) toTarget = transform.forward;

                float targetYaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;

                // smoothly rotate player to face target (so player is oriented toward enemy)
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetYaw, ref _rotationVelocity, RotationSmoothTime);
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);

                // movement axes relative to target
                Quaternion targetYawRot = Quaternion.Euler(0f, targetYaw, 0f);
                Vector3 forward = targetYawRot * Vector3.forward;
                Vector3 right = targetYawRot * Vector3.right;

                Vector3 moveDir = (right * _input.Move.x + forward * _input.Move.y);
                if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

                _controller.Move(moveDir * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

                // update animator
                if (_hasAnimator)
                {
                    _animator.SetFloat(_animIDSpeed, _animationBlend);
                    _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
                }

                return;
            }

            // if there is a move input rotate player when the player is moving (free camera mode)
            if (_input.Move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // move the player
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                HandleGrounded();
            }
            else
            {
                HandleAirborne();
            }

            ApplyGravity();
        }

        private void HandleGrounded()
        {
            // reset the fall timeout timer
            _fallTimeoutDelta = FallTimeout;

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDFreeFall, false);
            }

            // stop our velocity dropping infinitely when grounded
            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }

            // Jump
            if (_input.Jump && _jumpTimeoutDelta <= 0.0f)
            {
                PerformJump();
            }

            // jump timeout
            if (_jumpTimeoutDelta >= 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
        }

        private void PerformJump()
        {
            // the square root of H * -2 * G = how much velocity needed to reach desired height
            _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDJump, true);
            }
        }

        private void HandleAirborne()
        {
            // reset the jump timeout timer
            _jumpTimeoutDelta = JumpTimeout;

            // fall timeout
            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
            else if (_hasAnimator)
            {
                // update animator if using character
                _animator.SetBool(_animIDFreeFall, true);
            }

            // if we are not grounded, do not jump
            _input.Jump = false;
        }

        private void ApplyGravity()
        {
            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.35f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            Gizmos.color = Grounded ? transparentGreen : transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f && FootstepAudioClips.Length > 0)
            {
                var index = Random.Range(0, FootstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }

        public void LockCameraToTarget(Transform target, float angleOverride = 10f)
        {
            _cameraAngleOverride = angleOverride;
            _lockTarget = target;
            LockCameraPosition = true;

            var origin = CinemachineCameraTarget.transform.position;
            var dir = (target.position - origin).normalized;

            if (dir.sqrMagnitude < 0.0001f)
            {
                return;
            }

            var lookRot = Quaternion.LookRotation(dir);
            _cinemachineTargetYaw = lookRot.eulerAngles.y;
            float pitch = lookRot.eulerAngles.x;

            if (pitch > 180f)
            {
                pitch -= 360f;
            }

            _cinemachineTargetPitch = Mathf.Clamp(pitch, BottomClamp, TopClamp);
            _input.SprintInput(false);
        }

        public void UnlockCamera()
        {
            _lockTarget = null;
            LockCameraPosition = false;
            _input.SprintInput(true);
        }
    }
}
