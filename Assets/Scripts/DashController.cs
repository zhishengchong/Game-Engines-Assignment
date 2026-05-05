using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(100)] 
public class DashController : MonoBehaviour
{
    public static DashController Instance;

    [Header("冲刺设置")]
    public float dashSpeed = 40f;           
    public float dashWindowTime = 2f;       
    public bool isDashing = false; 

    [Header("电影级视角拉伸 (FOV Warp)")]
    public float dashFOV = 100f; 
    [Tooltip("普通的、缓慢恢复的镜头速度")]
    public float fovRecoverySpeed = 8f; 

    [Header("音效设置")]
    public AudioClip dashSound;
    private AudioSource _audioSource;

    private CharacterController _controller;
    private bool _dashAvailable = false;    
    private Vector3 _dashTargetPos;         
    private float _dashTimer;

    private Camera _mainCamera;
    private float _normalFOV = 40f;
    private float _targetFOV;
    private float _currentFOV;
    
    // 【核心新增】：用来动态控制当前镜头缩放的速度
    private float _activeFovSpeed;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        _controller = GetComponent<CharacterController>();
        _mainCamera = Camera.main; 
        
        if (_mainCamera != null)
        {
            _normalFOV = _mainCamera.fieldOfView;
            _targetFOV = _normalFOV;
            _currentFOV = _normalFOV;
        }

        _activeFovSpeed = fovRecoverySpeed;

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
            if (_dashTimer <= 0f) _dashAvailable = false;

            if (Keyboard.current.shiftKey.wasPressedThisFrame && !isDashing)
            {
                StartCoroutine(DashRoutine(_dashTargetPos));
            }
        }
    }

    void LateUpdate()
    {
        if (_mainCamera != null)
        {
            // 无论拉伸还是收缩，都受动态速度控制
            _currentFOV = Mathf.Lerp(_currentFOV, _targetFOV, Time.unscaledDeltaTime * _activeFovSpeed);
            _mainCamera.fieldOfView = _currentFOV;
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
        isDashing = true;
        _dashAvailable = false; 
        _controller.enabled = false;
        
        if (dashSound != null && _audioSource != null)
        {
            _audioSource.pitch = 1f; 
            _audioSource.PlayOneShot(dashSound);
        }

        _targetFOV = dashFOV;
        _activeFovSpeed = 20f; // 冲刺时快速拉伸

        Vector3 dashTarget = targetPos - (targetPos - transform.position).normalized * 1.5f;
        while (Vector3.Distance(transform.position, dashTarget) > 0.5f)
        {
            transform.position = Vector3.Lerp(transform.position, dashTarget, Time.unscaledDeltaTime * dashSpeed);
            yield return null; 
        }

        _controller.enabled = true;
        isDashing = false;

        _targetFOV = _normalFOV;
        _activeFovSpeed = fovRecoverySpeed; // 冲刺结束缓慢恢复
    }

    // ==========================================
    // 【终极重构】：支持自定义速度的 FOV 门阀
    // ==========================================
    
    // 1. 瞬间撑爆并锁定 (通过传入极高的 speed)
    public void HoldFOV(float punchFOV, float expandSpeed)
    {
        _targetFOV = punchFOV; 
        _activeFovSpeed = expandSpeed; // 比如传入 30，镜头就会在极短时间内瞬间撑爆
    }

    // 2. 解除锁定，平缓回落
    public void ReleaseFOV()
    {
        _targetFOV = _normalFOV; 
        _activeFovSpeed = fovRecoverySpeed; // 恢复为缓慢的 8f，平滑落地
    }
}