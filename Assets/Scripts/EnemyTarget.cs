using System;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyTarget : MonoBehaviour
{
    [Header("📝 终端代号 (Terminal ID)")]
    [Tooltip("敌人死亡后显示的代号，例如 SPIKE、TURRET。")]
    public string targetCodeName = "CHASER";

    [Header("❤️ 生命值")]
    [Min(1f)]
    [Tooltip("敌人的最大生命值。")]
    public float maxHealth = 100f;

    [SerializeField]
    [Tooltip("当前生命值。运行时自动从 Max Health 开始。")]
    private float currentHealth;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    public float HealthNormalized
    {
        get
        {
            if (maxHealth <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(
                currentHealth / maxHealth
            );
        }
    }

    [Header("📈 击杀奖励")]
    [Tooltip("击杀此敌人奖励的分数。")]
    public float scoreReward = 1500f;

    [Tooltip("击杀此敌人倒流的时间，单位为秒。")]
    public float timeReward = 0.5f;

    [Header("💥 死亡视觉")]
    [Tooltip(
        "枪械击杀时使用的专属死亡效果。" +
        "留空时使用武器提供的备用效果。"
    )]
    public GameObject specificDeathEffect;

    [Tooltip(
        "掉出平台或撞击场景死亡时使用的效果。" +
        "留空时退回使用枪械死亡效果。"
    )]
    public GameObject environmentalDeathEffect;

    [Header("环境击杀设置")]
    [Tooltip(
        "环境击杀是否也提供分数、减时和 Dash 奖励。"
    )]
    public bool rewardEnvironmentalKill = true;

    [Header("运行状态")]
    [SerializeField]
    private bool isDead;

    public bool IsDead => isDead;

    /// <summary>
    /// 参数：
    /// 实际伤害、剩余生命值、命中位置。
    /// 后续可用于受击闪烁、血条等功能。
    /// </summary>
    public event Action<float, float, Vector3> Damaged;

    private void Awake()
    {
        maxHealth =
            Mathf.Max(1f, maxHealth);

        currentHealth = maxHealth;
        isDead = false;
    }

    // =========================================================
    // 正常伤害
    // =========================================================

    /// <summary>
    /// 对敌人造成伤害。
    /// 返回本次实际扣除的生命值。
    /// </summary>
    public float TakeDamage(
        float damage,
        Vector3 hitPoint,
        Vector3 hitNormal,
        GameObject fallbackHitEffect = null
    )
    {
        if (isDead)
        {
            return 0f;
        }

        if (damage <= 0f)
        {
            return 0f;
        }

        float healthBeforeDamage =
            currentHealth;

        currentHealth =
            Mathf.Max(
                0f,
                currentHealth - damage
            );

        float actualDamage =
            healthBeforeDamage -
            currentHealth;

        Damaged?.Invoke(
            actualDamage,
            currentHealth,
            hitPoint
        );

        if (currentHealth <= 0f)
        {
            CompleteDeath(
                hitPoint,
                hitNormal,
                specificDeathEffect,
                fallbackHitEffect,
                true
            );
        }
        else
        {
            // 非致命命中只播放普通命中特效，
            // 不播放完整的死亡碎裂效果。
            SpawnEffect(
                fallbackHitEffect,
                hitPoint,
                hitNormal
            );
        }

        return actualDamage;
    }

    // =========================================================
    // 兼容旧代码的直接死亡接口
    // =========================================================

    /// <summary>
    /// 保留旧接口，避免其他旧脚本报错。
    /// 调用此接口会无视生命值直接死亡。
    /// </summary>
    public void Die(
        Vector3 hitPoint,
        Vector3 hitNormal,
        GameObject fallbackEffect
    )
    {
        if (isDead)
        {
            return;
        }

        currentHealth = 0f;

        CompleteDeath(
            hitPoint,
            hitNormal,
            specificDeathEffect,
            fallbackEffect,
            true
        );
    }

    // =========================================================
    // 环境死亡
    // =========================================================

    /// <summary>
    /// Spike 掉下平台、撞到其他平台或进入 KillZone 时调用。
    /// 环境死亡无视剩余生命值。
    /// </summary>
    public void DieFromEnvironment(
        Vector3 deathPoint
    )
    {
        if (isDead)
        {
            return;
        }

        currentHealth = 0f;

        GameObject selectedEffect =
            environmentalDeathEffect != null
                ? environmentalDeathEffect
                : specificDeathEffect;

        CompleteDeath(
            deathPoint,
            Vector3.up,
            selectedEffect,
            null,
            rewardEnvironmentalKill
        );
    }

    // =========================================================
    // 统一死亡结算
    // =========================================================

    private void CompleteDeath(
        Vector3 impactPoint,
        Vector3 impactNormal,
        GameObject preferredDeathEffect,
        GameObject fallbackEffect,
        bool grantRewards
    )
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        currentHealth = 0f;

        Vector3 killPosition =
            transform.position;

        GameObject effectToPlay =
            preferredDeathEffect != null
                ? preferredDeathEffect
                : fallbackEffect;

        if (effectToPlay != null)
        {
            /*
             * 专属死亡 Prefab 放在敌人中心生成。
             * 普通备用命中特效才放在具体命中点。
             *
             * 这样 Spike 的死亡圆环和碎片不会偏到球体表面。
             */
            Vector3 effectPosition =
                preferredDeathEffect != null
                    ? killPosition
                    : impactPoint;

            SpawnEffect(
                effectToPlay,
                effectPosition,
                impactNormal
            );
        }

        if (grantRewards)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddBonusScore(
                    scoreReward,
                    timeReward,
                    targetCodeName,
                    killPosition
                );
            }

            if (DashController.Instance != null)
            {
                DashController.Instance.OnEnemyKilled(
                    impactPoint
                );
            }
        }

        Destroy(gameObject);
    }

    private void SpawnEffect(
        GameObject effectPrefab,
        Vector3 spawnPosition,
        Vector3 direction
    )
    {
        if (effectPrefab == null)
        {
            return;
        }

        Quaternion rotation =
            Quaternion.identity;

        if (direction.sqrMagnitude > 0.0001f)
        {
            rotation =
                Quaternion.LookRotation(
                    direction.normalized
                );
        }

        Instantiate(
            effectPrefab,
            spawnPosition,
            rotation
        );
    }

    // =========================================================
    // 可选公共接口
    // =========================================================

    public void ResetHealthToFull()
    {
        if (isDead)
        {
            return;
        }

        currentHealth =
            Mathf.Max(1f, maxHealth);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        maxHealth =
            Mathf.Max(1f, maxHealth);

        if (!Application.isPlaying)
        {
            currentHealth = maxHealth;
        }
        else
        {
            currentHealth =
                Mathf.Clamp(
                    currentHealth,
                    0f,
                    maxHealth
                );
        }
    }
#endif
}