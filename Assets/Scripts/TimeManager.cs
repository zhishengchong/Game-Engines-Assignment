using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets; 

public class TimeManager : MonoBehaviour
{
    [Header("时间设置")]
    public float slowTimeScale = 0.05f;  
    public float normalTimeScale = 1.0f; 
    public float lerpSpeed = 10f;        
    
    [Header("手感优化")]
    [Tooltip("每次开火或丢枪时，强制保持时间正常流动的秒数（后摇缓冲）")]
    public float actionTimeBuffer = 0.1f; 

    private StarterAssetsInputs _playerInputs;
    private FirstPersonController _fpc;
    private WeaponInteraction _weaponInt;
    
    // 用来记录动作发生后的缓冲倒计时
    private float _actionTimer = 0f;

    void Start()
    {
        // 自动去玩家身上获取需要的三大组件
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerInputs = player.GetComponent<StarterAssetsInputs>();
            _fpc = player.GetComponent<FirstPersonController>();
            _weaponInt = player.GetComponent<WeaponInteraction>();
        }
    }

    void Update()
    {
        if (_playerInputs == null) return;

        // 1. 移动状态：按了 WASD
        bool isMoving = _playerInputs.move.sqrMagnitude > 0.01f;

        // 2. 空中状态：只要玩家的脚不在地上（跳跃、下坠），时间就一直流动
        bool isInAir = _fpc != null && !_fpc.Grounded;

        // 3. 战斗动作状态
        bool isCombatActing = false;

        // 【核心修复】：必须手里有枪，按键才作数！
        if (_weaponInt != null && _weaponInt.HasWeapon())
        {
            // 如果按下了开火或丢枪，给缓冲器充能！
            if (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)
            {
                _actionTimer = actionTimeBuffer; // 比如充能 0.2 秒
            }
        }

        // 处理缓冲倒计时（即使你松开鼠标，这 0.2 秒内时间依然正常流逝）
        if (_actionTimer > 0f)
        {
            _actionTimer -= Time.unscaledDeltaTime;
            isCombatActing = true;
        }

        // 【终极判定】：移动中、在空中、或者处于战斗后摇中，时间流动！
        bool isActing = isMoving || isInAir || isCombatActing;

        float targetScale = isActing ? normalTimeScale : slowTimeScale;
        Time.timeScale = Mathf.Lerp(Time.timeScale, targetScale, Time.unscaledDeltaTime * lerpSpeed);

        Time.fixedDeltaTime = Mathf.Clamp(0.02f * Time.timeScale, 0.01f, 0.02f);
    }
}