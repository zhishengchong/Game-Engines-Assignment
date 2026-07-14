using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SpikeVisualFeedback : MonoBehaviour
{
    [Header("核心引用")]
    [Tooltip("Enemy_Spike_Root 上的 SpikeEnemy 脚本。")]
    public SpikeEnemy spikeEnemy;

    [Tooltip("Enemy_Spike_Root 上的 EnemyTarget 脚本。")]
    public EnemyTarget enemyTarget;

    [Tooltip("需要改变颜色的模型 Renderer。")]
    public Renderer[] targetRenderers;

    [Tooltip("ChargeTrail 上的 TrailRenderer。")]
    public TrailRenderer chargeTrail;

    [Header("状态颜色")]
    public Color idleColor =
        new Color(0.04f, 0.08f, 1f, 1f);

    public Color trackingColor =
        new Color(0.02f, 0.45f, 1f, 1f);

    public Color windupColor =
        new Color(1f, 0.01f, 0.35f, 1f);

    public Color chargingColor =
        new Color(1f, 0.01f, 0.6f, 1f);

    public Color stunnedColor =
        new Color(0.65f, 0.7f, 0.75f, 1f);

    public Color fallingColor =
        new Color(0.55f, 0.02f, 0.25f, 1f);

    [Header("Emission 强度")]
    [Min(0f)]
    public float idleEmission = 0.15f;

    [Min(0f)]
    public float trackingEmission = 0.8f;

    [Min(0f)]
    public float windupEmission = 5f;

    [Min(0f)]
    public float chargingEmission = 7f;

    [Min(0f)]
    public float stunnedEmission = 1.5f;

    [Min(0f)]
    public float fallingEmission = 1.5f;

    [Header("过渡与闪烁")]
    [Tooltip("颜色切换速度。")]
    public float transitionSpeed = 10f;

    [Tooltip("蓄力时闪烁速度。")]
    public float windupPulseSpeed = 24f;

    [Range(0f, 1f)]
    public float windupPulseStrength = 0.25f;

    [Tooltip("眩晕时白色闪烁速度。")]
    public float stunnedFlashSpeed = 9f;

    [Header("非致命命中反馈")]
    [Tooltip("受到枪械伤害时短暂闪烁的颜色。")]
    public Color hitFlashColor = Color.white;

    [Tooltip("单次命中闪光持续的真实时间。")]
    [Min(0.01f)]
    public float hitFlashDuration = 0.1f;

    [Tooltip("命中瞬间的发光强度。")]
    [Min(0f)]
    public float hitFlashEmission = 12f;

    [Tooltip(
        "每次命中增加多少闪光强度。" +
        "散弹多颗同时命中时会叠加。"
    )]
    [Range(0f, 1f)]
    public float hitFlashBaseStrength = 0.35f;

    [Tooltip(
        "伤害占最大生命值的比例，对闪光强度的影响。"
    )]
    [Min(0f)]
    public float hitDamageStrengthMultiplier = 2f;

    private static readonly int BaseColorId =
        Shader.PropertyToID("_BaseColor");

    private static readonly int ColorId =
        Shader.PropertyToID("_Color");

    private static readonly int EmissionColorId =
        Shader.PropertyToID("_EmissionColor");

    private MaterialPropertyBlock _propertyBlock;

    private Color _currentBaseColor;
    private Color _currentEmissionColor;

    private SpikeEnemy.SpikeState _previousState;
    private bool _hasPreviousState;

    private float _hitFlashTimer;
    private float _hitFlashStrength;

    private bool _damageEventSubscribed;

    private void Awake()
    {
        if (spikeEnemy == null)
        {
            spikeEnemy =
                GetComponent<SpikeEnemy>();
        }

        if (enemyTarget == null)
        {
            enemyTarget =
                GetComponent<EnemyTarget>();
        }

        if (chargeTrail == null)
        {
            chargeTrail =
                GetComponentInChildren<TrailRenderer>(
                    true
                );
        }

        if (targetRenderers == null ||
            targetRenderers.Length == 0)
        {
            FindModelRenderers();
        }

        _propertyBlock =
            new MaterialPropertyBlock();

        _currentBaseColor =
            idleColor;

        _currentEmissionColor =
            idleColor * idleEmission;

        if (chargeTrail != null)
        {
            chargeTrail.emitting = false;
            chargeTrail.Clear();
        }
    }

    private void OnEnable()
    {
        SubscribeToDamageEvent();
    }

    private void Start()
    {
        if (spikeEnemy == null)
        {
            Debug.LogError(
                $"[SpikeVisualFeedback] " +
                $"{gameObject.name} 没有找到 SpikeEnemy。"
            );

            enabled = false;
            return;
        }

        SubscribeToDamageEvent();

        HandleStateChanged(
            spikeEnemy.CurrentState
        );

        ApplyMaterialProperties();
    }

    private void Update()
    {
        if (spikeEnemy == null)
        {
            return;
        }

        SpikeEnemy.SpikeState currentState =
            spikeEnemy.CurrentState;

        if (!_hasPreviousState ||
            currentState != _previousState)
        {
            HandleStateChanged(
                currentState
            );
        }

        UpdateVisualColors(
            currentState
        );

        ApplyHitFlash();

        ApplyMaterialProperties();
    }

    // =========================================================
    // 引用与事件
    // =========================================================

    private void SubscribeToDamageEvent()
    {
        if (_damageEventSubscribed)
        {
            return;
        }

        if (enemyTarget == null)
        {
            enemyTarget =
                GetComponent<EnemyTarget>();
        }

        if (enemyTarget == null)
        {
            return;
        }

        enemyTarget.Damaged +=
            HandleEnemyDamaged;

        _damageEventSubscribed = true;
    }

    private void UnsubscribeFromDamageEvent()
    {
        if (!_damageEventSubscribed)
        {
            return;
        }

        if (enemyTarget != null)
        {
            enemyTarget.Damaged -=
                HandleEnemyDamaged;
        }

        _damageEventSubscribed = false;
    }

    private void HandleEnemyDamaged(
        float actualDamage,
        float remainingHealth,
        Vector3 hitPoint
    )
    {
        if (actualDamage <= 0f)
        {
            return;
        }

        float damageRatio = 0f;

        if (enemyTarget != null &&
            enemyTarget.MaxHealth > 0f)
        {
            damageRatio =
                actualDamage /
                enemyTarget.MaxHealth;
        }

        /*
         * 多颗散弹在同一帧命中时，
         * 每次事件都会继续叠加强度。
         */
        float addedStrength =
            hitFlashBaseStrength +
            damageRatio *
            hitDamageStrengthMultiplier;

        _hitFlashStrength =
            Mathf.Clamp01(
                _hitFlashStrength +
                addedStrength
            );

        _hitFlashTimer =
            Mathf.Max(
                _hitFlashTimer,
                hitFlashDuration
            );
    }

    private void FindModelRenderers()
    {
        Renderer[] allRenderers =
            GetComponentsInChildren<Renderer>(
                true
            );

        List<Renderer> validRenderers =
            new List<Renderer>();

        foreach (Renderer foundRenderer
                 in allRenderers)
        {
            if (foundRenderer == null)
            {
                continue;
            }

            if (foundRenderer is TrailRenderer)
            {
                continue;
            }

            if (foundRenderer is ParticleSystemRenderer)
            {
                continue;
            }

            validRenderers.Add(
                foundRenderer
            );
        }

        targetRenderers =
            validRenderers.ToArray();
    }

    // =========================================================
    // 状态切换
    // =========================================================

    private void HandleStateChanged(
        SpikeEnemy.SpikeState newState
    )
    {
        _previousState =
            newState;

        _hasPreviousState =
            true;

        if (chargeTrail == null)
        {
            return;
        }

        bool shouldEmit =
            newState ==
            SpikeEnemy.SpikeState.Charging;

        if (shouldEmit)
        {
            chargeTrail.Clear();
        }

        chargeTrail.emitting =
            shouldEmit;
    }

    // =========================================================
    // 状态颜色
    // =========================================================

    private void UpdateVisualColors(
        SpikeEnemy.SpikeState state
    )
    {
        Color targetBaseColor =
            idleColor;

        Color targetEmissionColor =
            idleColor * idleEmission;

        switch (state)
        {
            case SpikeEnemy.SpikeState.Idle:
            {
                targetBaseColor =
                    idleColor;

                targetEmissionColor =
                    idleColor *
                    idleEmission;

                break;
            }

            case SpikeEnemy.SpikeState.Tracking:
            {
                float pulse =
                    1f +
                    Mathf.Sin(
                        Time.time * 4f
                    ) *
                    0.08f;

                targetBaseColor =
                    trackingColor;

                targetEmissionColor =
                    trackingColor *
                    trackingEmission *
                    pulse;

                break;
            }

            case SpikeEnemy.SpikeState.Windup:
            {
                float progress =
                    spikeEnemy.WindupProgress;

                targetBaseColor =
                    Color.Lerp(
                        trackingColor,
                        windupColor,
                        progress
                    );

                float pulse =
                    1f +
                    Mathf.Sin(
                        Time.time *
                        windupPulseSpeed
                    ) *
                    windupPulseStrength;

                float emissionIntensity =
                    Mathf.Lerp(
                        trackingEmission,
                        windupEmission,
                        progress
                    );

                targetEmissionColor =
                    windupColor *
                    emissionIntensity *
                    pulse;

                break;
            }

            case SpikeEnemy.SpikeState.Charging:
            {
                float pulse =
                    1f +
                    Mathf.Sin(
                        Time.time * 30f
                    ) *
                    0.12f;

                targetBaseColor =
                    chargingColor;

                targetEmissionColor =
                    chargingColor *
                    chargingEmission *
                    pulse;

                break;
            }

            case SpikeEnemy.SpikeState.Recovering:
            {
                targetBaseColor =
                    Color.Lerp(
                        chargingColor,
                        trackingColor,
                        0.7f
                    );

                targetEmissionColor =
                    trackingColor *
                    trackingEmission;

                break;
            }

            case SpikeEnemy.SpikeState.Stunned:
            {
                float flash =
                    Mathf.PingPong(
                        Time.time *
                        stunnedFlashSpeed,
                        1f
                    );

                targetBaseColor =
                    Color.Lerp(
                        stunnedColor,
                        Color.white,
                        flash
                    );

                targetEmissionColor =
                    Color.white *
                    stunnedEmission *
                    flash;

                break;
            }

            case SpikeEnemy.SpikeState.Falling:
            {
                targetBaseColor =
                    fallingColor;

                targetEmissionColor =
                    fallingColor *
                    fallingEmission;

                break;
            }
        }

        float interpolation =
            1f -
            Mathf.Exp(
                -transitionSpeed *
                Time.deltaTime
            );

        _currentBaseColor =
            Color.Lerp(
                _currentBaseColor,
                targetBaseColor,
                interpolation
            );

        _currentEmissionColor =
            Color.Lerp(
                _currentEmissionColor,
                targetEmissionColor,
                interpolation
            );
    }

    // =========================================================
    // 命中闪光
    // =========================================================

    private void ApplyHitFlash()
    {
        if (_hitFlashTimer <= 0f)
        {
            _hitFlashTimer = 0f;
            _hitFlashStrength = 0f;
            return;
        }

        _hitFlashTimer -=
            Time.unscaledDeltaTime;

        float normalizedTime =
            Mathf.Clamp01(
                _hitFlashTimer /
                Mathf.Max(
                    0.01f,
                    hitFlashDuration
                )
            );

        /*
         * 开始非常亮，然后快速衰减。
         */
        float decay =
            normalizedTime *
            normalizedTime;

        float finalStrength =
            Mathf.Clamp01(
                decay *
                _hitFlashStrength
            );

        _currentBaseColor =
            Color.Lerp(
                _currentBaseColor,
                hitFlashColor,
                finalStrength
            );

        Color flashEmission =
            hitFlashColor *
            hitFlashEmission;

        _currentEmissionColor =
            Color.Lerp(
                _currentEmissionColor,
                flashEmission,
                finalStrength
            );

        if (_hitFlashTimer <= 0f)
        {
            _hitFlashTimer = 0f;
            _hitFlashStrength = 0f;
        }
    }

    // =========================================================
    // 材质属性
    // =========================================================

    private void ApplyMaterialProperties()
    {
        if (targetRenderers == null)
        {
            return;
        }

        foreach (Renderer targetRenderer
                 in targetRenderers)
        {
            if (targetRenderer == null)
            {
                continue;
            }

            _propertyBlock.Clear();

            targetRenderer.GetPropertyBlock(
                _propertyBlock
            );

            _propertyBlock.SetColor(
                BaseColorId,
                _currentBaseColor
            );

            _propertyBlock.SetColor(
                ColorId,
                _currentBaseColor
            );

            _propertyBlock.SetColor(
                EmissionColorId,
                _currentEmissionColor
            );

            targetRenderer.SetPropertyBlock(
                _propertyBlock
            );
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromDamageEvent();

        if (chargeTrail != null)
        {
            chargeTrail.emitting = false;
            chargeTrail.Clear();
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromDamageEvent();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        transitionSpeed =
            Mathf.Max(
                0f,
                transitionSpeed
            );

        hitFlashDuration =
            Mathf.Max(
                0.01f,
                hitFlashDuration
            );

        hitFlashEmission =
            Mathf.Max(
                0f,
                hitFlashEmission
            );

        hitDamageStrengthMultiplier =
            Mathf.Max(
                0f,
                hitDamageStrengthMultiplier
            );
    }
#endif
}