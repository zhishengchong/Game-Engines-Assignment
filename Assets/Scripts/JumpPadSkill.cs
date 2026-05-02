using UnityEngine;
using UnityEngine.InputSystem;

public class JumpPadSkill : MonoBehaviour
{
    public static JumpPadSkill Instance; 

    [Header("技能设置")]
    public GameObject jumpPadPrefab; 
    [Tooltip("向前丢出的力度（已削弱，原为15）")]
    public float throwForce = 8f;
    [Tooltip("向上抛出的弧度补偿（微微加大，让抛物线更好看）")]
    public float throwUpwardForce = 6f;

    [Header("冷却系统")]
    public float maxCooldown = 15f;    
    public float currentCooldown = 0f;

    [Header("SUPERHOT 动态惩罚")]
    [Range(0.1f, 1f)] public float slowMotionCooldownMultiplier = 0.3f;

    private Camera _mainCamera;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        _mainCamera = Camera.main;
    }

    void Update()
    {
        if (currentCooldown > 0f)
        {
            float currentRate = Mathf.Lerp(slowMotionCooldownMultiplier, 1f, Time.timeScale);
            currentCooldown -= Time.unscaledDeltaTime * currentRate;
        }

        if (Keyboard.current.zKey.wasPressedThisFrame && currentCooldown <= 0f)
        {
            ThrowJumpPad();
        }
    }

    private void ThrowJumpPad()
    {
        if (jumpPadPrefab == null || _mainCamera == null) return;

        currentCooldown = maxCooldown;

        // ==========================================
        // 【核心修复】：防穿模安全生成机制
        // ==========================================
        Vector3 startPos = _mainCamera.transform.position;
        // 计算原始的理想生成点
        Vector3 idealSpawnPos = startPos + _mainCamera.transform.forward * 1.5f - new Vector3(0, 0.5f, 0);
        
        Vector3 spawnPos = idealSpawnPos;
        Vector3 dir = idealSpawnPos - startPos;
        
        // 发射射线，扫描从眼睛到生成点之间是否有地板/墙壁挡着
        RaycastHit[] hits = Physics.RaycastAll(startPos, dir.normalized, dir.magnitude);
        float closestDistance = dir.magnitude;

        foreach (var hit in hits)
        {
            // 忽略玩家身上的所有零件，只检测环境
            if (!hit.collider.transform.root.CompareTag("Player") && !hit.collider.CompareTag("Player"))
            {
                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    // 如果撞到地/墙，就贴着表面往法线方向（外面）挪出 0.2 米，绝对不卡进地里！
                    spawnPos = hit.point + hit.normal * 0.2f;
                }
            }
        }

        // 保持跳板水平
        Quaternion flatRotation = Quaternion.Euler(0, _mainCamera.transform.eulerAngles.y, 0);
        GameObject pad = Instantiate(jumpPadPrefab, spawnPos, flatRotation);

        // 满门抄斩式物理屏蔽：防重叠爆炸
        Collider[] allPlayerColliders = GetComponentsInChildren<Collider>();
        Collider[] padColliders = pad.GetComponentsInChildren<Collider>();
        foreach (Collider padCol in padColliders)
        {
            if (!padCol.isTrigger) 
            {
                foreach (Collider pCol in allPlayerColliders)
                {
                    Physics.IgnoreCollision(padCol, pCol);
                }
            }
        }

        // 施加推力
        Rigidbody rb = pad.GetComponent<Rigidbody>();
        if (rb != null)
        {
            CharacterController cc = GetComponent<CharacterController>();
            Vector3 playerVelocity = cc != null ? cc.velocity : Vector3.zero;
            
            playerVelocity.y = 0; 
            
            Vector3 aimDirection = _mainCamera.transform.forward; 
            Vector3 force = playerVelocity + (aimDirection * throwForce) + (Vector3.up * throwUpwardForce);
            
            rb.AddForce(force, ForceMode.VelocityChange);
        }
        
        // 触发 UI 高光和倒计时跳减
        if (SkillUIManager.Instance != null)
        {
            SkillUIManager.Instance.TriggerKillFeedback();
        }
    }

    public void ReduceCooldown(float timeToReduce)
    {
        if (currentCooldown > 0f)
        {
            currentCooldown -= timeToReduce;
            
            if (SkillUIManager.Instance != null)
            {
                SkillUIManager.Instance.TriggerKillFeedback();
            }

            if (currentCooldown <= 0f)
            {
                currentCooldown = 0f;
            }
        }
    }
}