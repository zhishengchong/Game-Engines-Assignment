using UnityEngine;
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
		[Header("Player")]
		public float MoveSpeed = 4.0f;
		public float SprintSpeed = 6.0f;
		public float RotationSpeed = 1.0f;
		public float SpeedChangeRate = 10.0f;

		[Space(10)]
		public float JumpHeight = 1.2f;
		public float Gravity = -15.0f;

		[Space(10)]
		public float JumpTimeout = 0.1f;
		public float FallTimeout = 0.15f;

		[Header("Player Grounded")]
		public bool Grounded = true;
		public float GroundedOffset = -0.14f;
		public float GroundedRadius = 0.5f;
		public LayerMask GroundLayers;

		[Header("Cinemachine")]
		public GameObject CinemachineCameraTarget;
		public float TopClamp = 90.0f;
		public float BottomClamp = -90.0f;

		private float _cinemachineTargetPitch;
		private float _speed;
		private float _rotationVelocity;
		private float _verticalVelocity;
		private float _terminalVelocity = 53.0f;

		private float _jumpTimeoutDelta;
		private float _fallTimeoutDelta;

#if ENABLE_INPUT_SYSTEM
		private PlayerInput _playerInput;
#endif
		private CharacterController _controller;
		private StarterAssetsInputs _input;
		private GameObject _mainCamera;

		private const float _threshold = 0.01f;

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
			if (_mainCamera == null)
			{
				_mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
			}
		}

		private void Start()
		{
			_controller = GetComponent<CharacterController>();
			_input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
			_playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

			_jumpTimeoutDelta = JumpTimeout;
			_fallTimeoutDelta = FallTimeout;
		}

		private void Update()
		{
			// ==========================================
			// 【Dash 防报错补丁】：在冲刺时，碰撞体会被关闭
			// 如果发现碰撞体未激活，直接跳过本帧的所有移动与重力计算！
			// ==========================================
			if (_controller != null && !_controller.enabled) return;

			JumpAndGravity();
			GroundedCheck();
			Move();
		}

		private void LateUpdate()
		{
			CameraRotation();
		}

		private void GroundedCheck()
		{
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
			Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
		}

		private void CameraRotation()
		{
			if (_input.look.sqrMagnitude >= _threshold)
			{
				// SUPERHOT 修复：转动视角使用真实时间
				float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.unscaledDeltaTime;
				
				_cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
				_rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

				_cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);
				CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);
				transform.Rotate(Vector3.up * _rotationVelocity);
			}
		}

		private void Move()
		{
			float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;
			if (_input.move == Vector2.zero) targetSpeed = 0.0f;

			// 【防瞬移核心修复】：不再信任物理引擎乱报的速度，直接使用我们自己计算的 _speed
			float currentHorizontalSpeed = _speed; 
			
			float speedOffset = 0.1f;
			float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

			if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
			{
				// SUPERHOT 修复：平滑加速使用真实时间
				_speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.unscaledDeltaTime * SpeedChangeRate);
				_speed = Mathf.Round(_speed * 1000f) / 1000f;
			}
			else
			{
				_speed = targetSpeed;
			}

			Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
			if (_input.move != Vector2.zero)
			{
				inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
			}

			// SUPERHOT 修复：最终位移计算使用真实时间
			Vector3 moveVector = inputDirection.normalized * (_speed * Time.unscaledDeltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.unscaledDeltaTime;

			// 防瞬移安全锁：如果一帧算出来的位移比 5 米还大，说明炸了，强制归零
			if (moveVector.magnitude > 5f) 
			{
				moveVector = Vector3.zero;
			}

			_controller.Move(moveVector);
		}

		private void JumpAndGravity()
		{
			if (Grounded)
			{
				_fallTimeoutDelta = FallTimeout;

				// 安全锁：在地面时锁定垂直速度
				_verticalVelocity = -2f;

				if (_input.jump && _jumpTimeoutDelta <= 0.0f)
				{
					_verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
				}

				if (_jumpTimeoutDelta >= 0.0f)
				{
					_jumpTimeoutDelta -= Time.unscaledDeltaTime;
				}
			}
			else
			{
				_jumpTimeoutDelta = JumpTimeout;

				if (_fallTimeoutDelta >= 0.0f)
				{
					_fallTimeoutDelta -= Time.unscaledDeltaTime;
				}
				_input.jump = false;

				// 安全锁：限制下落最大速度
				if (_verticalVelocity < -_terminalVelocity)
				{
					_verticalVelocity = -_terminalVelocity;
				}
			}

			// SUPERHOT 修复：重力随真实时间累加
			if (!Grounded && _verticalVelocity < _terminalVelocity)
			{
				_verticalVelocity += Gravity * Time.unscaledDeltaTime;
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
			Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
			Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

			if (Grounded) Gizmos.color = transparentGreen;
			else Gizmos.color = transparentRed;

			Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
		}
	}
}