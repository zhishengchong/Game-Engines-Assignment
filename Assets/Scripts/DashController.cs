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
    public float fovRecoverySpeed = 8f; 

    [Header("音效设置")]
    [Tooltip("把你找好的冲刺音效拖到这里")]
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

        // 自动给玩家挂载一个喇叭用来播放冲刺声音
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
            _currentFOV = Mathf.Lerp(_currentFOV, _targetFOV, Time.unscaledDeltaTime * fovRecoverySpeed);
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
        
        // ==========================================
        // 【播放音效】：瞬间播放空间撕裂的冲刺声！
        // ==========================================
        if (dashSound != null && _audioSource != null)
        {
            _audioSource.pitch = 1f; // 保证冲刺声音不受之前操作的干扰
            _audioSource.PlayOneShot(dashSound);
        }

        _targetFOV = dashFOV;
        _currentFOV = dashFOV; 

        Vector3 dashTarget = targetPos - (targetPos - transform.position).normalized * 1.5f;
        
        while (Vector3.Distance(transform.position, dashTarget) > 0.5f)
        {
            transform.position = Vector3.Lerp(transform.position, dashTarget, Time.unscaledDeltaTime * dashSpeed);
            yield return null; 
        }

        _controller.enabled = true;
        isDashing = false;

        _targetFOV = _normalFOV;
    }
}