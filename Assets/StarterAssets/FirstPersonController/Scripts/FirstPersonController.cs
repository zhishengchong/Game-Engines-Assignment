using UnityEngine;
using Unity.Cinemachine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class FirstPersonController : MonoBehaviour
    {
        public static FirstPersonController Instance;

        [Header("🚀 Apex 动量狂奔 (Momentum Sprint)")]
        public float MaxSprintSpeed = 12.0f;
        public float AccelerationRate = 8.0f;
        public float DecelerationRate = 12.0f;

        [Header("🛹 极限滑铲 (Apex Slide)")]
        public float MaxSlideBoost = 4.5f;
        public float MaxSlideSpeed = 16.5f;
        public float SlideDecayRate = 6.0f;
        public float CrouchHeight = 0.8f;
        public float SlideJumpBoost = 12.0f;
        public float SlideCooldown = 0.5f;

        [Header("🦘 喷气背包：二段跳 (Titanfall Double Jump)")]
        public int MaxJumps = 2;
        public float DoubleJumpBoostMultiplier = 1.25f;
        public float DoubleJumpForwardPunch = 8.0f;
        public float MaxDoubleJumpSpeed = 24.0f;
        public AudioClip doubleJumpSound;

        private int _currentJumps;
        private float _doubleJumpJuiceTimer = 0f;
        private AudioSource _audioSource;

        [Header("🎥 Apex 镜头魔法 (Camera Juice)")]
        public CinemachineCamera virtualCamera;
        public float normalFOV = 90f;
        public float sprintFOV = 105f;
        public float slideFOV = 115f;
        public float maxRollAngle = 3.5f;

        [Header("🎯 武器瞄准系统 (ADS)")]
        public float adsFOV = 40f;

        [Header("💥 外部 FOV 覆盖：Dash / JumpPad / Cannon / Lift")]
        public float externalFOVRecoverSpeed = 8f;

        private bool _externalFOVActive = false;
        private float _externalFOVTarget = 90f;
        private float _externalFOVLerpSpeed = 20f;
        private float _externalFOVTimer = 0f;
        private bool _externalFOVHasAutoRelease = false;

        [Header("💀 空间回溯拉扯 (Rewind Shake)")]
        public float rewindFOV = 150f;
        public float rewindMaxRoll = 12f;
        [HideInInspector] public bool isRewinding = false;

        [Header("🧲 重力大炮支持 (Gravity Cannon Support)")]
        [Tooltip("大炮发射后，短时间降低空中外力阻尼，让轨迹更稳定。")]
        public float launchNoDragAirFriction = 0.15f;

        private bool _isCannonControlled = false;
        private float _launchNoDragTimer = 0f;
        private float _cannonActionLockTimer = 0f;

        [Header("🌀 重力上升场支持 (Gravity Lift Support)")]
        [Tooltip("玩家在 Gravity Lift 区域内时是否屏蔽跳跃。现在建议关闭。")]
        public bool blockJumpWhileGravityLifted = false;

        [Tooltip("进入 Gravity Lift 时是否刷新跳跃次数。建议开启，这样玩家可以在上升场内二段跳。")]
        public bool rechargeJumpsOnGravityLiftEnter = true;

        private float _gravityLiftTimer = 0f;
        private float _gravityLiftTargetUpSpeed = 9f;
        private float _gravityLiftAcceleration = 24f;
        private float _gravityLiftGravityMultiplier = 0.15f;

        [Space(10)]
        public float JumpHeight = 1.2f;
        public float Gravity = -15.0f;
        public float RotationSpeed = 1.0f;
        public float JumpTimeout = 0.1f;
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        public bool Grounded = true;
        public float GroundedOffset = -0.14f;
        public float GroundedRadius = 0.5f;
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        public GameObject CinemachineCameraTarget;
        public float TopClamp = 89.0f;
        public float BottomClamp = -89.0f;

        private float _cinemachineTargetPitch;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        private float _currentSpeed;
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        private Vector3 _externalVelocity = Vector3.zero;
        private float _jumpPadGraceTimer = 0f;

        private bool _isSliding = false;
        private float _normalHeight;
        private float _normalCameraY;
        private Vector3 _slideDirection;
        private float _slideCooldownTimer = 0f;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif

        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private AlwaysEquippedWeaponController _alwaysEquippedWeaponController;

        private const float _threshold = 0.01f;

        private readonly Collider[] _groundCheckHits = new Collider[16];

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput != null && _playerInput.currentControlScheme == "KeyboardMouse";
#else
                return false;
#endif
            }
        }

        private void Awake()
        {
            Instance = this;

            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
            _alwaysEquippedWeaponController = GetComponent<AlwaysEquippedWeaponController>();

#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#endif

            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            if (_controller != null)
            {
                _normalHeight = _controller.height;
            }

            if (CinemachineCameraTarget != null)
            {
                _normalCameraY = CinemachineCameraTarget.transform.localPosition.y;
            }

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }

            _currentJumps = MaxJumps;

            if (virtualCamera != null)
            {
                virtualCamera.Lens.FieldOfView = normalFOV;
            }
        }

        private void Update()
        {
            if (_controller != null && !_controller.enabled) return;
            if (_input == null || _controller == null) return;

            if (_slideCooldownTimer > 0f)
            {
                _slideCooldownTimer -= Time.deltaTime;
            }

            if (_doubleJumpJuiceTimer > 0f)
            {
                _doubleJumpJuiceTimer -= Time.unscaledDeltaTime;
            }

            UpdateExternalFOVTimer();
            UpdateCannonActionLockTimer();
            UpdateGravityLiftTimer();

            if (_isCannonControlled)
            {
                ClearMovementInput();
                ApplyCameraJuice();
                return;
            }

            GroundedCheck();
            JumpAndGravity();
            Move();
            ApplyCameraJuice();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void UpdateExternalFOVTimer()
        {
            if (!_externalFOVActive) return;
            if (!_externalFOVHasAutoRelease) return;

            _externalFOVTimer -= Time.unscaledDeltaTime;

            if (_externalFOVTimer <= 0f)
            {
                ReleaseExternalFOV(externalFOVRecoverSpeed);
            }
        }

        private void UpdateCannonActionLockTimer()
        {
            if (_cannonActionLockTimer > 0f)
            {
                _cannonActionLockTimer -= Time.deltaTime;

                if (_cannonActionLockTimer < 0f)
                {
                    _cannonActionLockTimer = 0f;
                }
            }
        }

        private void UpdateGravityLiftTimer()
        {
            if (_gravityLiftTimer > 0f)
            {
                _gravityLiftTimer -= Time.deltaTime;

                if (_gravityLiftTimer < 0f)
                {
                    _gravityLiftTimer = 0f;
                }
            }
        }

        private void GroundedCheck()
        {
            if (_jumpPadGraceTimer > 0f)
            {
                _jumpPadGraceTimer -= Time.deltaTime;
                Grounded = false;
                return;
            }

            Vector3 spherePosition = new Vector3(
                transform.position.x,
                transform.position.y + GroundedOffset,
                transform.position.z
            );

            int hitCount = Physics.OverlapSphereNonAlloc(
                spherePosition,
                GroundedRadius,
                _groundCheckHits,
                GroundLayers,
                QueryTriggerInteraction.Ignore
            );

            bool foundRealGround = false;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _groundCheckHits[i];
                if (hit == null) continue;

                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                CharacterController hitController = hit.GetComponentInParent<CharacterController>();
                if (hitController != null && hitController == _controller)
                {
                    continue;
                }

                if (hit.isTrigger)
                {
                    continue;
                }

                foundRealGround = true;
                break;
            }

            Grounded = foundRealGround;
        }

        private void CameraRotation()
        {
            if (_input == null) return;
            if (CinemachineCameraTarget == null) return;

            if (_input.look.sqrMagnitude >= _threshold)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.unscaledDeltaTime;

                _cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
                _rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

                _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

                CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(
                    _cinemachineTargetPitch,
                    0.0f,
                    0.0f
                );

                transform.Rotate(Vector3.up * _rotationVelocity);
            }
        }

        private void Move()
        {
            Vector3 inputDirection = Vector3.zero;

            if (_input.move != Vector2.zero)
            {
                inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
                inputDirection.Normalize();
            }

            bool wantsToSlide = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                wantsToSlide = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.cKey.isPressed;
            }
#endif

            if (wantsToSlide && Grounded && _input.move.y > 0 && !_isSliding && _slideCooldownTimer <= 0f)
            {
                _isSliding = true;
                _slideDirection = inputDirection;

                float momentumRatio = Mathf.Clamp01(_currentSpeed / MaxSprintSpeed);
                float dynamicBoost = Mathf.Lerp(0.5f, MaxSlideBoost, momentumRatio);

                _currentSpeed += dynamicBoost;
                _currentSpeed = Mathf.Min(_currentSpeed, MaxSlideSpeed);

                _controller.height = CrouchHeight;
                _controller.center = new Vector3(0, CrouchHeight / 2f, 0);
            }

            if (_isSliding)
            {
                if (!wantsToSlide || _currentSpeed < 3.0f || !Grounded)
                {
                    Vector3 rayStart = transform.position + new Vector3(0, CrouchHeight, 0);

                    if (!Physics.Raycast(rayStart, Vector3.up, _normalHeight - CrouchHeight + 0.1f, GroundLayers))
                    {
                        StopSlide();
                    }
                }
            }

            if (CinemachineCameraTarget != null)
            {
                float targetCamY = _isSliding ? _normalCameraY - (_normalHeight - CrouchHeight) : _normalCameraY;
                Vector3 currentCamPos = CinemachineCameraTarget.transform.localPosition;

                CinemachineCameraTarget.transform.localPosition = Vector3.Lerp(
                    currentCamPos,
                    new Vector3(currentCamPos.x, targetCamY, currentCamPos.z),
                    Time.unscaledDeltaTime * 15f
                );
            }

            if (_isSliding)
            {
                _currentSpeed -= SlideDecayRate * Time.deltaTime;
                _currentSpeed = Mathf.Max(_currentSpeed, 0f);
                inputDirection = _slideDirection;
            }
            else
            {
                float targetSpeed = _input.move == Vector2.zero ? 0.0f : MaxSprintSpeed;
                float accelRate = _input.move == Vector2.zero ? DecelerationRate : AccelerationRate;

                _currentSpeed = Mathf.MoveTowards(
                    _currentSpeed,
                    targetSpeed,
                    Time.deltaTime * accelRate
                );
            }

            if (_launchNoDragTimer > 0f)
            {
                _launchNoDragTimer -= Time.deltaTime;

                if (_launchNoDragTimer < 0f)
                {
                    _launchNoDragTimer = 0f;
                }
            }

            _externalVelocity = Vector3.ClampMagnitude(_externalVelocity, 80f);

            Vector3 horizontalMove = inputDirection * (_currentSpeed * Time.deltaTime);
            Vector3 verticalMove = new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime;
            Vector3 externalMove = _externalVelocity * Time.deltaTime;

            Vector3 moveVector = horizontalMove + verticalMove + externalMove;

            _controller.Move(moveVector);

            float friction;

            if (_launchNoDragTimer > 0f)
            {
                friction = launchNoDragAirFriction;
            }
            else
            {
                friction = Grounded ? 8f : 1.5f;
            }

            _externalVelocity = Vector3.Lerp(_externalVelocity, Vector3.zero, Time.deltaTime * friction);
        }

        private void JumpAndGravity()
        {
            if (IsGravityLiftActive())
            {
                Grounded = false;
                _fallTimeoutDelta = FallTimeout;

                if (_jumpPadGraceTimer < 0.05f)
                {
                    _jumpPadGraceTimer = 0.05f;
                }

                float acceleration = Mathf.Max(1f, _gravityLiftAcceleration);

                _verticalVelocity = Mathf.MoveTowards(
                    _verticalVelocity,
                    _gravityLiftTargetUpSpeed,
                    acceleration * Time.deltaTime
                );

                _verticalVelocity += Gravity * Mathf.Clamp01(_gravityLiftGravityMultiplier) * Time.deltaTime;

                if (_verticalVelocity < 0f)
                {
                    _verticalVelocity = Mathf.MoveTowards(
                        _verticalVelocity,
                        0f,
                        acceleration * Time.deltaTime
                    );
                }

                bool jumpBlocked = blockJumpWhileGravityLifted || IsCannonActionLocked();

                if (jumpBlocked && _input.jump)
                {
                    _input.jump = false;
                }

                if (!jumpBlocked && _input.jump && _jumpTimeoutDelta <= 0.0f && _currentJumps > 0)
                {
                    HandleJumpInput();
                }

                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }

                _input.jump = false;
                return;
            }

            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;

                if (_verticalVelocity <= 0.0f)
                {
                    _currentJumps = MaxJumps;
                    _verticalVelocity = -2f;
                }
            }
            else
            {
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
            }

            bool jumpBlockedByCannon = IsCannonActionLocked();

            if (jumpBlockedByCannon && _input.jump)
            {
                _input.jump = false;
            }

            if (!jumpBlockedByCannon && _input.jump && _jumpTimeoutDelta <= 0.0f && _currentJumps > 0)
            {
                HandleJumpInput();
            }

            if (_jumpTimeoutDelta >= 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }

            _input.jump = false;

            if (!Grounded)
            {
                if (_verticalVelocity > -_terminalVelocity)
                {
                    _verticalVelocity += Gravity * Time.deltaTime;
                }

                if (_verticalVelocity < -_terminalVelocity)
                {
                    _verticalVelocity = -_terminalVelocity;
                }
            }
        }

        private void HandleJumpInput()
        {
            bool isDoubleJump = !Grounded && _currentJumps < MaxJumps;

            float jumpVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

            if (IsGravityLiftActive())
            {
                _verticalVelocity = Mathf.Max(_verticalVelocity, jumpVelocity);
            }
            else
            {
                _verticalVelocity = jumpVelocity;
            }

            if (isDoubleJump)
            {
                _currentSpeed *= DoubleJumpBoostMultiplier;

                Vector3 inputDir = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

                if (inputDir == Vector3.zero)
                {
                    inputDir = Vector3.forward;
                }

                Vector3 punchDirection = transform.right * inputDir.x + transform.forward * inputDir.z;
                _externalVelocity += punchDirection * DoubleJumpForwardPunch;

                _currentSpeed = Mathf.Min(_currentSpeed, MaxDoubleJumpSpeed);

                if (doubleJumpSound != null && _audioSource != null)
                {
                    _audioSource.pitch = Random.Range(1.1f, 1.4f);
                    _audioSource.PlayOneShot(doubleJumpSound, 0.8f);
                }

                _doubleJumpJuiceTimer = 0.2f;
            }
            else
            {
                if (_isSliding)
                {
                    Vector3 forwardBoost = transform.forward * SlideJumpBoost;
                    _externalVelocity += forwardBoost;

                    StopSlide();
                }
            }

            _currentJumps--;
            _jumpTimeoutDelta = JumpTimeout;
        }

        private void ApplyCameraJuice()
        {
            if (virtualCamera == null) return;

            float targetFOV = normalFOV;
            float targetRoll = 0f;
            float lerpSpeed = 18f;

            bool isAiming = false;

#if ENABLE_INPUT_SYSTEM
            if (_alwaysEquippedWeaponController != null &&
                _alwaysEquippedWeaponController.HasWeapon() &&
                Mouse.current != null &&
                Mouse.current.rightButton.isPressed)
            {
                isAiming = true;
            }
#endif

            if (isRewinding)
            {
                targetFOV = rewindFOV;
                targetRoll = Random.Range(-rewindMaxRoll, rewindMaxRoll);
                lerpSpeed = 40f;
            }
            else if (isAiming)
            {
                targetFOV = adsFOV;
                targetRoll = 0f;
                lerpSpeed = 22f;
            }
            else if (_isSliding)
            {
                targetFOV = slideFOV;

                float slideRightAmount = Vector3.Dot(_slideDirection, transform.right);
                targetRoll = -slideRightAmount * maxRollAngle * 1.5f;
            }
            else if (Grounded)
            {
                float speedRatio = Mathf.Clamp01(_currentSpeed / MaxSprintSpeed);
                targetFOV = Mathf.Lerp(normalFOV, sprintFOV, speedRatio);
                targetRoll = -_input.move.x * maxRollAngle;
            }

            // 关键修复：
            // 外部 FOV 不能覆盖 ADS。
            // 之前 Lift FOV = 100 会压过 adsFOV = 40，所以你能打独头弹，但镜头不缩放。
            if (_externalFOVActive && !isAiming && !isRewinding)
            {
                targetFOV = Mathf.Max(targetFOV, _externalFOVTarget);
                lerpSpeed = Mathf.Max(lerpSpeed, _externalFOVLerpSpeed);
            }

            if (_doubleJumpJuiceTimer > 0f && !isAiming)
            {
                targetFOV += 12f;
            }

            virtualCamera.Lens.FieldOfView = Mathf.Lerp(
                virtualCamera.Lens.FieldOfView,
                targetFOV,
                Time.unscaledDeltaTime * lerpSpeed
            );

            virtualCamera.Lens.Dutch = Mathf.Lerp(
                virtualCamera.Lens.Dutch,
                targetRoll,
                Time.unscaledDeltaTime * (isRewinding ? 50f : lerpSpeed)
            );
        }

        private void StopSlide()
        {
            _isSliding = false;
            _slideCooldownTimer = SlideCooldown;

            if (_controller != null)
            {
                _controller.height = _normalHeight;
                _controller.center = new Vector3(0, _normalHeight / 2f, 0);
            }
        }

        private void ClearMovementInput()
        {
            if (_input == null) return;

            _input.move = Vector2.zero;
            _input.jump = false;
            _input.sprint = false;
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;

            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        public void ApplyJumpPadForce(Vector3 launchVelocity)
        {
            _verticalVelocity = launchVelocity.y;
            _externalVelocity = new Vector3(launchVelocity.x, 0f, launchVelocity.z);

            Grounded = false;
            _jumpPadGraceTimer = 0.2f;

            if (_isSliding)
            {
                StopSlide();
            }
        }

        public void AddExternalVelocity(Vector3 velocity, float maxExternalSpeed = 60f, bool forceAirborne = false)
        {
            _externalVelocity += velocity;
            _externalVelocity = Vector3.ClampMagnitude(_externalVelocity, maxExternalSpeed);

            if (forceAirborne)
            {
                Grounded = false;
                _jumpPadGraceTimer = 0.08f;
            }

            if (_isSliding)
            {
                StopSlide();
            }
        }

        public void ApplyGravityLift(
            float targetUpSpeed,
            float acceleration,
            float gravityMultiplier,
            float keepAliveTime = 0.12f
        )
        {
            bool wasLiftInactive = _gravityLiftTimer <= 0f;

            _gravityLiftTimer = Mathf.Max(_gravityLiftTimer, keepAliveTime);
            _gravityLiftTargetUpSpeed = Mathf.Max(0f, targetUpSpeed);
            _gravityLiftAcceleration = Mathf.Max(1f, acceleration);
            _gravityLiftGravityMultiplier = Mathf.Clamp01(gravityMultiplier);

            if (wasLiftInactive && rechargeJumpsOnGravityLiftEnter)
            {
                _currentJumps = MaxJumps;
                _jumpTimeoutDelta = 0f;
            }

            Grounded = false;

            if (_jumpPadGraceTimer < 0.05f)
            {
                _jumpPadGraceTimer = 0.05f;
            }

            if (_isSliding)
            {
                StopSlide();
            }
        }

        public void ApplyGravityLiftExitBoost(
            Vector3 boostVelocity,
            float launchGraceTime = 0.16f,
            float noDragDuration = 0.3f
        )
        {
            _gravityLiftTimer = 0f;

            Vector3 horizontalBoost = new Vector3(
                boostVelocity.x,
                0f,
                boostVelocity.z
            );

            _externalVelocity += horizontalBoost;
            _externalVelocity = Vector3.ClampMagnitude(_externalVelocity, 80f);

            if (boostVelocity.y > _verticalVelocity)
            {
                _verticalVelocity = boostVelocity.y;
            }

            Grounded = false;
            _jumpPadGraceTimer = Mathf.Max(_jumpPadGraceTimer, launchGraceTime);
            _launchNoDragTimer = Mathf.Max(_launchNoDragTimer, noDragDuration);

            if (_isSliding)
            {
                StopSlide();
            }
        }

        public bool IsGravityLiftActive()
        {
            return _gravityLiftTimer > 0f;
        }

        public void BeginCannonControl(bool resetMomentum = true)
        {
            _isCannonControlled = true;

            if (resetMomentum)
            {
                ResetMomentum();
            }

            ClearMovementInput();

            if (_isSliding)
            {
                StopSlide();
            }
        }

        public void CancelCannonControl()
        {
            _isCannonControlled = false;
            ClearMovementInput();
        }

        public void EndCannonControlAndLaunch(
            Vector3 launchVelocity,
            float launchGraceTime = 0.25f,
            float maxHorizontalExternalSpeed = 80f,
            float noDragDuration = 0.75f,
            float actionLockDuration = 0.45f
        )
        {
            _isCannonControlled = false;

            ResetMomentum();

            Vector3 horizontalVelocity = new Vector3(
                launchVelocity.x,
                0f,
                launchVelocity.z
            );

            if (maxHorizontalExternalSpeed > 0f)
            {
                horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, maxHorizontalExternalSpeed);
            }

            _verticalVelocity = launchVelocity.y;
            _externalVelocity = horizontalVelocity;

            Grounded = false;
            _jumpPadGraceTimer = launchGraceTime;
            _launchNoDragTimer = noDragDuration;
            _cannonActionLockTimer = actionLockDuration;

            _currentJumps = MaxJumps;

            ClearMovementInput();

            if (_isSliding)
            {
                StopSlide();
            }
        }

        public bool IsCannonControlled()
        {
            return _isCannonControlled;
        }

        public bool IsCannonActionLocked()
        {
            return _isCannonControlled || _cannonActionLockTimer > 0f;
        }

        public void ResetMomentum()
        {
            _currentSpeed = 0f;
            _verticalVelocity = 0f;
            _externalVelocity = Vector3.zero;
            _launchNoDragTimer = 0f;
            _gravityLiftTimer = 0f;

            if (_isSliding)
            {
                StopSlide();
            }
        }

        public void RequestExternalFOV(float targetFOV, float lerpSpeed, float autoReleaseTime = 0f)
        {
            _externalFOVActive = true;
            _externalFOVTarget = targetFOV;
            _externalFOVLerpSpeed = Mathf.Max(1f, lerpSpeed);

            if (autoReleaseTime > 0f)
            {
                _externalFOVTimer = autoReleaseTime;
                _externalFOVHasAutoRelease = true;
            }
            else
            {
                _externalFOVTimer = 0f;
                _externalFOVHasAutoRelease = false;
            }
        }

        public void ReleaseExternalFOV(float recoverSpeed = 8f)
        {
            _externalFOVActive = false;
            _externalFOVTimer = 0f;
            _externalFOVHasAutoRelease = false;
            externalFOVRecoverSpeed = recoverSpeed;
        }

        public void KeepExternalFOVUntilManualRelease()
        {
            _externalFOVHasAutoRelease = false;
            _externalFOVTimer = 0f;
        }

        public int GetCurrentJumps()
        {
            return _currentJumps;
        }

        public int GetMaxJumps()
        {
            return MaxJumps;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Grounded ? Color.green : Color.red;

            Vector3 spherePosition = new Vector3(
                transform.position.x,
                transform.position.y + GroundedOffset,
                transform.position.z
            );

            Gizmos.DrawWireSphere(spherePosition, GroundedRadius);
        }
    }
}