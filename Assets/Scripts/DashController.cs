using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class DashController : MonoBehaviour
{
    [Header("冲刺设置")]
    public float dashSpeed = 40f;           // 极快的冲刺速度
    public float dashWindowTime = 2f;       // 击杀后，你有多少秒的时间可以按下 Shift 冲刺

    private CharacterController _controller;
    private bool _isDashing = false;
    private bool _dashAvailable = false;    // 当前是否有冲刺权限
    private Vector3 _dashTargetPos;         // 冲刺的目标位置
    private float _dashTimer;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // 如果拥有冲刺权限，开始倒计时
        if (_dashAvailable)
        {
            // 使用真实时间倒计时，不受慢动作影响
            _dashTimer -= Time.unscaledDeltaTime;
            
            if (_dashTimer <= 0f)
            {
                _dashAvailable = false;
                Debug.Log("冲刺权限已过期。");
            }

            // 在权限时间内按下 Shift 键 (由于用了新输入系统，直接读键盘最稳)
            if (Keyboard.current.shiftKey.wasPressedThisFrame && !_isDashing)
            {
                StartCoroutine(DashRoutine(_dashTargetPos));
            }
        }
    }

    // 【核心接口】：这个方法将由子弹在击杀敌人时调用！
    public void OnEnemyKilled(Vector3 enemyPosition)
    {
        _dashAvailable = true;
        _dashTargetPos = enemyPosition;
        _dashTimer = dashWindowTime;
        
        Debug.Log("【系统提示】击杀确认！你有 " + dashWindowTime + " 秒时间按下 SHIFT 瞬间冲刺！");
    }

    private IEnumerator DashRoutine(Vector3 targetPos)
    {
        _isDashing = true;
        _dashAvailable = false; // 消耗掉这次权限
        
        // 【安全锁】：关闭物理碰撞，防止飞天 Bug 或卡进墙里
        _controller.enabled = false;
        
        // 目标点定在敌人死前位置的身前 1.5 米，防止卡进怪物的出生点墙壁
        Vector3 dashTarget = targetPos - (targetPos - transform.position).normalized * 1.5f;
        
        // 使用 unscaledDeltaTime 保证在极慢时间下也能光速冲刺
        while (Vector3.Distance(transform.position, dashTarget) > 0.5f)
        {
            transform.position = Vector3.Lerp(transform.position, dashTarget, Time.unscaledDeltaTime * dashSpeed);
            yield return null; 
        }

        // 冲刺结束，恢复物理运算
        _controller.enabled = true;
        _isDashing = false;
    }
}