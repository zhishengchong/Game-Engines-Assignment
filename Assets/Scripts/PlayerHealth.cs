using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance;

    [Header("🛡️ 系统完整度 (Health)")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float regenDelay = 3f;
    public float regenRate = 20f;

    [Header("⏳ 猎空级高精度回溯 (Tracer Recall Engine)")]
    public float recordInterval = 0.1f;
    public int maxRecords = 30;

    private struct Breadcrumb
    {
        public Vector3 position;
        public bool isGrounded;
    }

    private Queue<Breadcrumb> _trail = new Queue<Breadcrumb>();
    private float _recordTimer = 0f;

    [Header("💀 重载惩罚 (Reboot Penalty)")]
    [Tooltip("倒带物理演出耗时")]
    public float rewindDuration = 2.5f;
    public float postRewindInvincible = 0.5f;
    public float scorePenalty = 10000f;
    public float timePenalty = 5f;

    [Header("🎧 听觉物理 (Rewind Audio)")]
    public AudioClip rewindSoundEffect;

    private AudioSource _audioSource;

    private bool _isInvincible = false;
    private float _lastDamageTime = 0f;

    private StarterAssets.FirstPersonController _fpc;
    private CharacterController _controller;
    private StarterAssets.StarterAssetsInputs _starterInputs;
    private AlwaysEquippedWeaponController _weaponController;
    private DashController _dashController;

#if ENABLE_INPUT_SYSTEM
    private PlayerInput _playerInput;
#endif

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        currentHealth = maxHealth;

        _fpc = GetComponent<StarterAssets.FirstPersonController>();
        _controller = GetComponent<CharacterController>();
        _starterInputs = GetComponent<StarterAssets.StarterAssetsInputs>();
        _weaponController = GetComponent<AlwaysEquippedWeaponController>();
        _dashController = GetComponent<DashController>();

#if ENABLE_INPUT_SYSTEM
        _playerInput = GetComponent<PlayerInput>();
#endif

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Update()
    {
        if (!_isInvincible && _fpc != null)
        {
            _recordTimer += Time.deltaTime;

            if (_recordTimer >= recordInterval)
            {
                if (_trail.Count >= maxRecords)
                {
                    _trail.Dequeue();
                }

                _trail.Enqueue(new Breadcrumb
                {
                    position = transform.position,
                    isGrounded = _fpc.Grounded
                });

                _recordTimer = 0f;
            }
        }

        if (!_isInvincible && currentHealth < maxHealth)
        {
            if (Time.time - _lastDamageTime >= regenDelay)
            {
                currentHealth += regenRate * Time.deltaTime;
                currentHealth = Mathf.Min(currentHealth, maxHealth);
            }
        }

#if ENABLE_INPUT_SYSTEM
        // 测试用：按 K 触发一次致命伤害。最终提交前可以删掉这段。
        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            TakeDamage(100f);
        }
#endif
    }

    public void TakeDamage(float amount)
    {
        if (_isInvincible) return;

        currentHealth -= amount;
        _lastDamageTime = Time.time;

        Debug.Log($"[警告] 系统受损！当前完整度: {currentHealth}");

        if (currentHealth <= 0f)
        {
            TriggerSystemReboot();
        }
    }

    private void TriggerSystemReboot()
    {
        if (_isInvincible) return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddBonusScore(-scorePenalty, -timePenalty, "REBOOT_FAIL");
            GameManager.Instance.BreakCombo();
        }

        StartCoroutine(PhysicalRewindRoutine());
    }

    private IEnumerator PhysicalRewindRoutine()
    {
        _isInvincible = true;

        bool controllerWasEnabled = _controller != null && _controller.enabled;
        bool fpcWasEnabled = _fpc != null && _fpc.enabled;
        bool weaponWasEnabled = _weaponController != null && _weaponController.enabled;
        bool dashWasEnabled = _dashController != null && _dashController.enabled;

#if ENABLE_INPUT_SYSTEM
        bool playerInputWasEnabled = _playerInput != null && _playerInput.enabled;
#endif

        // 1. 冻结玩家控制
        if (_controller != null)
        {
            _controller.enabled = false;
        }

        if (_fpc != null)
        {
            _fpc.isRewinding = true;
            _fpc.ResetMomentum();
            _fpc.enabled = false;
        }

        if (_weaponController != null)
        {
            _weaponController.enabled = false;
        }

        if (_dashController != null)
        {
            _dashController.enabled = false;
        }

#if ENABLE_INPUT_SYSTEM
        if (_playerInput != null)
        {
            _playerInput.enabled = false;
        }
#endif

        if (_starterInputs != null)
        {
            _starterInputs.move = Vector2.zero;
            _starterInputs.look = Vector2.zero;
            _starterInputs.jump = false;
            _starterInputs.sprint = false;
        }

        // 2. 触发 UI / 后处理 / 音效
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.TriggerRewindGlitch(rewindDuration);
        }

        if (VisualFeedbackController.Instance != null)
        {
            VisualFeedbackController.Instance.TriggerRebootVisuals(rewindDuration);
        }

        if (rewindSoundEffect != null && _audioSource != null)
        {
            _audioSource.pitch = 1f;
            _audioSource.volume = 1f;
            _audioSource.PlayOneShot(rewindSoundEffect);
        }

        // 3. 生成回溯路径
        List<Breadcrumb> path = new List<Breadcrumb>(_trail);
        path.Reverse();

        _trail.Clear();
        _recordTimer = 0f;

        Vector3 targetSafePos = transform.position;
        bool foundSafe = false;

        for (int i = path.Count - 1; i >= 0; i--)
        {
            if (path[i].isGrounded)
            {
                targetSafePos = path[i].position;
                foundSafe = true;
                break;
            }
        }

        if (!foundSafe && path.Count > 0)
        {
            targetSafePos = path[path.Count - 1].position;
        }

        List<Vector3> visualPath = new List<Vector3>();
        visualPath.Add(transform.position);

        foreach (Breadcrumb p in path)
        {
            visualPath.Add(p.position);
        }

        visualPath.Add(targetSafePos);

        float segmentTime = rewindDuration / Mathf.Max(1, visualPath.Count);

        // 4. 物理回溯移动
        for (int i = 0; i < visualPath.Count - 1; i++)
        {
            Vector3 pA = visualPath[i];
            Vector3 pB = visualPath[i + 1];
            float t = 0f;

            while (t < segmentTime)
            {
                t += Time.unscaledDeltaTime;
                transform.position = Vector3.Lerp(pA, pB, t / segmentTime);

                if (_fpc != null && _fpc.virtualCamera != null)
                {
                    _fpc.virtualCamera.Lens.FieldOfView = Mathf.Lerp(
                        _fpc.virtualCamera.Lens.FieldOfView,
                        _fpc.rewindFOV,
                        Time.unscaledDeltaTime * 40f
                    );

                    _fpc.virtualCamera.Lens.Dutch = Mathf.Lerp(
                        _fpc.virtualCamera.Lens.Dutch,
                        Random.Range(-_fpc.rewindMaxRoll, _fpc.rewindMaxRoll),
                        Time.unscaledDeltaTime * 50f
                    );
                }

                yield return null;
            }

            transform.position = pB;
        }

        // 5. 停止回溯音效
        if (_audioSource != null && _audioSource.isPlaying)
        {
            _audioSource.Stop();
        }

        // 6. 恢复状态
        currentHealth = maxHealth;

        if (_controller != null)
        {
            _controller.enabled = controllerWasEnabled;
        }

        if (_fpc != null)
        {
            _fpc.enabled = fpcWasEnabled;
            _fpc.isRewinding = false;
            _fpc.ResetMomentum();
        }

        if (_weaponController != null)
        {
            _weaponController.enabled = weaponWasEnabled;
        }

        if (_dashController != null)
        {
            _dashController.enabled = dashWasEnabled;
        }

#if ENABLE_INPUT_SYSTEM
        if (_playerInput != null)
        {
            _playerInput.enabled = playerInputWasEnabled;
        }
#endif

        if (_starterInputs != null)
        {
            _starterInputs.move = Vector2.zero;
            _starterInputs.look = Vector2.zero;
            _starterInputs.jump = false;
            _starterInputs.sprint = false;
        }

        yield return new WaitForSecondsRealtime(postRewindInvincible);

        _isInvincible = false;

        Debug.Log("✅ 回溯完毕，系统重新接入。");
    }
}