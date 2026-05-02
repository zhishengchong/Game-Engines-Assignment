using UnityEngine;
using UnityEngine.InputSystem;

public class JumpPadSkill : MonoBehaviour
{
    public static JumpPadSkill Instance; 

    [Header("技能设置")]
    public GameObject jumpPadPrefab; 
    public float throwForce = 10f;
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
        // 动态冷却
        if (currentCooldown > 0f)
        {
            float currentRate = Mathf.Lerp(slowMotionCooldownMultiplier, 1f, Time.timeScale);
            currentCooldown -= Time.unscaledDeltaTime * currentRate;
        }

        // 丢跳板
        if (Keyboard.current.zKey.wasPressedThisFrame && currentCooldown <= 0f)
        {
            ThrowJumpPad();
        }
    }

    private void ThrowJumpPad()
    {
        if (jumpPadPrefab == null || _mainCamera == null) return;

        currentCooldown = maxCooldown;

        // 恢复：解除视角限制，允许低头往脚下扔！
        Vector3 spawnPos = _mainCamera.transform.position + _mainCamera.transform.forward * 1.5f - new Vector3(0, 0.5f, 0);
        
        Quaternion flatRotation = Quaternion.Euler(0, _mainCamera.transform.eulerAngles.y, 0);
        GameObject pad = Instantiate(jumpPadPrefab, spawnPos, flatRotation);

        // 满门抄斩式物理屏蔽
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
        
        Debug.Log("🟩 动力跳板已部署！");
    }

    public void ReduceCooldown(float timeToReduce)
    {
        if (currentCooldown > 0f)
        {
            currentCooldown -= timeToReduce;
            
            // ==========================================
            // 【UI 联动】：呼叫 UI 播放击杀高光特效！
            // ==========================================
            if (SkillUIManager.Instance != null)
            {
                SkillUIManager.Instance.TriggerKillFeedback();
            }

            if (currentCooldown <= 0f)
            {
                currentCooldown = 0f;
                Debug.Log("🔥 击杀！跳板已就绪！");
            }
            else
            {
                Debug.Log($"🩸 击杀！技能冷却缩减！剩余 CD: {currentCooldown:F1} 秒");
            }
        }
    }
}