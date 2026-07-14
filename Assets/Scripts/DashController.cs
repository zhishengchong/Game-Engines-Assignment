using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(100)]
public class DashController : MonoBehaviour
{
    public static DashController Instance;

    [Header("冲刺设置")]
    public float dashSpeed = 20f;
    public float dashWindowTime = 2f;
    public bool isDashing = false;

    [Header("电影级视角拉伸 (FOV Warp)")]
    public float dashFOV = 100f;
    public float dashFOVExpandSpeed = 20f;
    public float fovRecoverySpeed = 8f;

    [Header("音效设置")]
    public AudioClip dashSound;

    private AudioSource _audioSource;
    private CharacterController _controller;
    private StarterAssets.FirstPersonController _firstPersonController;

    private bool _dashAvailable = false;
    private Vector3 _dashTargetPos;
    private float _dashTimer;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        _controller = GetComponent<CharacterController>();
        _firstPersonController = GetComponent<StarterAssets.FirstPersonController>();

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
        }
    }

    void Update()
    {
        if (_dashAvailable)
        {
            _dashTimer -= Time.unscaledDeltaTime;

            if (_dashTimer <= 0f)
            {
                _dashAvailable = false;
            }

            if (_firstPersonController != null && _firstPersonController.IsCannonActionLocked())
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.shiftKey.wasPressedThisFrame && !isDashing)
            {
                StartCoroutine(DashRoutine(_dashTargetPos));
            }
        }
    }

    public void OnEnemyKilled(Vector3 enemyPosition)
    {
        _dashAvailable = true;
        _dashTargetPos = enemyPosition;
        _dashTimer = dashWindowTime;
    }

    private IEnumerator DashRoutine(Vector3 targetPos)
    {
        if (_firstPersonController != null && _firstPersonController.IsCannonActionLocked())
        {
            yield break;
        }

        isDashing = true;
        _dashAvailable = false;

        if (_controller != null)
        {
            _controller.enabled = false;
        }

        if (dashSound != null && _audioSource != null)
        {
            _audioSource.pitch = 1f;
            _audioSource.PlayOneShot(dashSound);
        }

        if (_firstPersonController != null)
        {
            _firstPersonController.RequestExternalFOV(dashFOV, dashFOVExpandSpeed, 0f);
        }

        Vector3 directionToTarget = targetPos - transform.position;

        if (directionToTarget.sqrMagnitude < 0.01f)
        {
            directionToTarget = transform.forward;
        }

        Vector3 dashTarget = targetPos - directionToTarget.normalized * 1.5f;

        while (Vector3.Distance(transform.position, dashTarget) > 0.5f)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                dashTarget,
                Time.unscaledDeltaTime * dashSpeed
            );

            yield return null;
        }

        if (_controller != null)
        {
            _controller.enabled = true;
        }

        isDashing = false;

        if (_firstPersonController != null)
        {
            _firstPersonController.ReleaseExternalFOV(fovRecoverySpeed);
        }
    }

    public void HoldFOV(float punchFOV, float expandSpeed, float autoReleaseTime = 0f)
    {
        if (_firstPersonController != null)
        {
            _firstPersonController.RequestExternalFOV(punchFOV, expandSpeed, autoReleaseTime);
        }
    }

    public void ReleaseFOV()
    {
        if (_firstPersonController != null)
        {
            _firstPersonController.ReleaseExternalFOV(fovRecoverySpeed);
        }
    }

    public void ConfirmTunnelEntry()
    {
        if (_firstPersonController != null)
        {
            _firstPersonController.KeepExternalFOVUntilManualRelease();
        }
    }
}