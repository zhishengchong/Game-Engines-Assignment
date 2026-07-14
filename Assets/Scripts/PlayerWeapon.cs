using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

using System.Collections;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class PlayerWeapon : MonoBehaviour
{
    [Header("武器属性")]
    public int maxAmmo = 2;
    public int currentAmmo;

    [Header("🔫 双管散弹配置（腰射）")]
    public Transform leftBarrelPoint;
    public Transform rightBarrelPoint;

    [Tooltip(
        "散弹扩散系数。数值越大，弹丸分布越散。"
    )]
    public float spreadAngle = 0.04f;

    public float hipfireRange = 30f;

    [Min(1)]
    [Tooltip("每次腰射真正发出的独立弹丸数量。")]
    public int hipfirePelletCount = 10;

    [Min(0f)]
    [Tooltip("每颗散弹命中造成的伤害。")]
    public float pelletDamage = 12f;

    [Header("🎯 独头弹模式配置（右键瞄准）")]
    public Transform centerBarrelPoint;
    public float slugRange = 150f;

    [Min(0f)]
    [Tooltip("独头弹命中造成的伤害。")]
    public float slugDamage = 100f;

    public float slugTracerWidth = 0.1f;
    public float slugTracerSpeed = 800f;

    [Header("👊 程序化后坐力")]
    public Transform weaponVisualMesh;
    public float kickbackZ = 0.4f;
    public float kickbackRotX = 15f;
    public float recoilRecoverSpeed = 8f;

    [Header("🧱 动态防穿模")]
    [Tooltip("只选择实际环境墙体的 Layer。")]
    public LayerMask environmentLayer;

    [Tooltip("靠近墙壁多少米时开始抬枪。")]
    public float weaponLength = 1.2f;

    public Vector3 wallAvoidPosOffset =
        new Vector3(
            0f,
            -0.1f,
            -0.3f
        );

    public Vector3 wallAvoidRotOffset =
        new Vector3(
            50f,
            -20f,
            0f
        );

    public float avoidSmoothSpeed = 12f;

    [Header("基础射击配置")]
    [Tooltip(
        "子弹能够命中的 Layer。" +
        "至少应包含 Default、Environment、Enemy。"
    )]
    public LayerMask hitLayers;

    [Tooltip(
        "普通、非致命的敌人命中特效。" +
        "一发腰射对每个敌人最多生成一次。"
    )]
    public GameObject enemyHitEffect;

    public GameObject environmentHitEffect;

    public Material tracerMaterial;
    public float tracerWidth = 0.02f;
    public float tracerSpeed = 500f;
    public float tracerLength = 4f;

    public AudioClip shootSound;

    [Header("运行时伤害调试")]
    [Tooltip(
        "开启后，每次开枪会在 Console 显示命中弹丸数和实际伤害。"
    )]
    public bool logDamageResults = false;

    [SerializeField]
    [Tooltip("上一发腰射有多少颗弹丸碰到了 EnemyTarget。")]
    private int lastHipfirePelletsHit;

    [SerializeField]
    [Tooltip("上一发腰射实际扣除了多少生命值。")]
    private float lastHipfireDamageDealt;

    [SerializeField]
    [Tooltip("上一发独头弹实际扣除了多少生命值。")]
    private float lastSlugDamageDealt;

    public bool IsAiming => _isAiming;

    public int LastHipfirePelletsHit =>
        lastHipfirePelletsHit;

    public float LastHipfireDamageDealt =>
        lastHipfireDamageDealt;

    public float LastSlugDamageDealt =>
        lastSlugDamageDealt;

    private float _currentWallWeight;

    private Vector3 _originalMeshPos;
    private Quaternion _originalMeshRot;

    private Vector3 _targetMeshPos;
    private Quaternion _targetMeshRot;

    private bool _isAiming;

    private AudioSource _audioSource;
    private Camera _mainCamera;

    private readonly List<LineRenderer> _tracerPool =
        new List<LineRenderer>();

    private const float GoldenAngleRadians =
        2.39996323f;

    /// <summary>
    /// 记录一发腰射对某个敌人的所有有效命中。
    /// 最后统一生成一次普通命中特效。
    /// </summary>
    private sealed class HipfireEnemyHitData
    {
        public Vector3 hitPointSum;
        public Vector3 hitNormalSum;

        public int damagingPelletCount;
        public float totalDamage;
    }

    private void Start()
    {
        currentAmmo = maxAmmo;
        _mainCamera = Camera.main;

        _audioSource =
            GetComponent<AudioSource>();

        if (_audioSource == null)
        {
            _audioSource =
                gameObject.AddComponent<AudioSource>();

            _audioSource.playOnAwake =
                false;
        }

        if (weaponVisualMesh != null)
        {
            _originalMeshPos =
                weaponVisualMesh.localPosition;

            _originalMeshRot =
                weaponVisualMesh.localRotation;

            _targetMeshPos =
                _originalMeshPos;

            _targetMeshRot =
                _originalMeshRot;
        }
    }

    private void Update()
    {
        UpdateWallAvoidance();
        UpdateAimingState();
    }

    // =========================================================
    // 动态防穿模
    // =========================================================

    private void UpdateWallAvoidance()
    {
        float targetWallWeight = 0f;

        if (_mainCamera != null)
        {
            Ray wallRay =
                new Ray(
                    _mainCamera.transform.position,
                    _mainCamera.transform.forward
                );

            if (Physics.Raycast(
                    wallRay,
                    out RaycastHit wallHit,
                    weaponLength,
                    environmentLayer,
                    QueryTriggerInteraction.Ignore
                ))
            {
                targetWallWeight =
                    1f -
                    wallHit.distance /
                    Mathf.Max(
                        0.01f,
                        weaponLength
                    );

                targetWallWeight =
                    Mathf.Clamp01(
                        targetWallWeight
                    );
            }
        }

        _currentWallWeight =
            Mathf.Lerp(
                _currentWallWeight,
                targetWallWeight,
                Time.deltaTime *
                avoidSmoothSpeed
            );

        if (weaponVisualMesh == null)
        {
            return;
        }

        Vector3 basePosition =
            _originalMeshPos +
            wallAvoidPosOffset *
            _currentWallWeight;

        Quaternion baseRotation =
            _originalMeshRot *
            Quaternion.Euler(
                wallAvoidRotOffset *
                _currentWallWeight
            );

        _targetMeshPos =
            Vector3.Lerp(
                _targetMeshPos,
                basePosition,
                Time.deltaTime *
                recoilRecoverSpeed
            );

        _targetMeshRot =
            Quaternion.Slerp(
                _targetMeshRot,
                baseRotation,
                Time.deltaTime *
                recoilRecoverSpeed
            );

        weaponVisualMesh.localPosition =
            _targetMeshPos;

        weaponVisualMesh.localRotation =
            _targetMeshRot;
    }

    private void UpdateAimingState()
    {
        if (transform.parent != null &&
            Mouse.current != null)
        {
            _isAiming =
                Mouse.current.rightButton.isPressed;
        }
        else
        {
            _isAiming = false;
        }
    }

    // =========================================================
    // 开火入口
    // =========================================================

    public bool TryShoot()
    {
        if (currentAmmo <= 0)
        {
            Debug.Log("⚠️ 没子弹了！");
            return false;
        }

        currentAmmo--;

        PlayShootSound();
        ApplyWeaponRecoil();

        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        if (_mainCamera == null)
        {
            Debug.LogWarning(
                "[PlayerWeapon] 没有找到 Main Camera，无法执行射线射击。"
            );

            return true;
        }

        if (_isAiming)
        {
            FireSlug();
        }
        else
        {
            FireHipfire();
        }

        return true;
    }

    private void PlayShootSound()
    {
        if (shootSound == null ||
            _audioSource == null)
        {
            return;
        }

        _audioSource.pitch =
            Random.Range(
                0.9f,
                1.1f
            );

        _audioSource.PlayOneShot(
            shootSound
        );
    }

    private void ApplyWeaponRecoil()
    {
        if (weaponVisualMesh == null)
        {
            return;
        }

        _targetMeshPos +=
            new Vector3(
                0f,
                0f,
                -kickbackZ
            );

        _targetMeshRot *=
            Quaternion.Euler(
                -kickbackRotX,
                0f,
                Random.Range(
                    -2f,
                    2f
                )
            );
    }

    // =========================================================
    // 独头弹
    // =========================================================

    private void FireSlug()
    {
        lastSlugDamageDealt = 0f;

        Vector3 rayOrigin =
            _mainCamera.ViewportToWorldPoint(
                new Vector3(
                    0.5f,
                    0.5f,
                    0f
                )
            );

        Vector3 shootDirection =
            _mainCamera.transform.forward;

        Ray ray =
            new Ray(
                rayOrigin,
                shootDirection
            );

        Vector3 endPoint;

        if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                slugRange,
                hitLayers,
                QueryTriggerInteraction.Collide
            ))
        {
            endPoint = hit.point;

            EnemyTarget enemyTarget =
                hit.collider.GetComponentInParent<EnemyTarget>();

            if (enemyTarget != null)
            {
                /*
                 * 独头弹只有一条射线，
                 * 可以继续让 EnemyTarget 自己生成一次命中特效。
                 */
                lastSlugDamageDealt =
                    enemyTarget.TakeDamage(
                        slugDamage,
                        hit.point,
                        hit.normal,
                        enemyHitEffect
                    );

                if (logDamageResults)
                {
                    Debug.Log(
                        $"[独头弹] 命中 {enemyTarget.targetCodeName}，" +
                        $"实际伤害：{lastSlugDamageDealt:F0}，" +
                        $"剩余生命：" +
                        $"{enemyTarget.CurrentHealth:F0}/" +
                        $"{enemyTarget.MaxHealth:F0}"
                    );
                }
            }
            else
            {
                HandleNonEnemyTargetHit(
                    hit,
                    null,
                    null
                );
            }
        }
        else
        {
            endPoint =
                ray.GetPoint(
                    slugRange
                );

            if (logDamageResults)
            {
                Debug.Log(
                    "[独头弹] 未命中敌人。"
                );
            }
        }

        Transform visualOrigin =
            centerBarrelPoint != null
                ? centerBarrelPoint
                : leftBarrelPoint;

        if (visualOrigin != null)
        {
            StartCoroutine(
                SpawnTracerRoutine(
                    visualOrigin.position,
                    endPoint,
                    slugTracerWidth,
                    slugTracerSpeed
                )
            );
        }
    }

    // =========================================================
    // 腰射散弹
    // =========================================================

    private void FireHipfire()
    {
        lastHipfirePelletsHit = 0;
        lastHipfireDamageDealt = 0f;

        int safePelletCount =
            Mathf.Max(
                1,
                hipfirePelletCount
            );

        float randomPatternRotation =
            Random.Range(
                0f,
                Mathf.PI * 2f
            );

        HashSet<TargetSwitch> switchesHitThisShot =
            new HashSet<TargetSwitch>();

        HashSet<GameObject> fallbackEnemiesKilled =
            new HashSet<GameObject>();

        /*
         * 每个 EnemyTarget 都有一份汇总记录。
         *
         * 多颗弹丸仍然逐颗伤害，
         * 但普通命中特效在所有弹丸结束后统一生成一次。
         */
        Dictionary<EnemyTarget, HipfireEnemyHitData>
            enemyHitsThisShot =
                new Dictionary<
                    EnemyTarget,
                    HipfireEnemyHitData
                >();

        for (int pelletIndex = 0;
             pelletIndex < safePelletCount;
             pelletIndex++)
        {
            Vector2 pelletOffset =
                CalculatePelletOffset(
                    pelletIndex,
                    safePelletCount,
                    randomPatternRotation
                );

            Vector3 rayOrigin =
                _mainCamera.ViewportToWorldPoint(
                    new Vector3(
                        0.5f,
                        0.5f,
                        0f
                    )
                );

            Vector3 spreadDirection =
                _mainCamera.transform.forward +
                _mainCamera.transform.right *
                pelletOffset.x *
                spreadAngle +
                _mainCamera.transform.up *
                pelletOffset.y *
                spreadAngle;

            spreadDirection.Normalize();

            Ray pelletRay =
                new Ray(
                    rayOrigin,
                    spreadDirection
                );

            Vector3 endPoint;

            if (Physics.Raycast(
                    pelletRay,
                    out RaycastHit hit,
                    hipfireRange,
                    hitLayers,
                    QueryTriggerInteraction.Collide
                ))
            {
                endPoint = hit.point;

                EnemyTarget enemyTarget =
                    hit.collider.GetComponentInParent<EnemyTarget>();

                if (enemyTarget != null)
                {
                    lastHipfirePelletsHit++;

                    /*
                     * 关键修复：
                     * 这里把 fallbackHitEffect 设为 null。
                     *
                     * 否则每颗非致命弹丸都会在
                     * EnemyTarget.TakeDamage() 中生成一次特效。
                     */
                    float actualDamage =
                        enemyTarget.TakeDamage(
                            pelletDamage,
                            hit.point,
                            hit.normal,
                            null
                        );

                    lastHipfireDamageDealt +=
                        actualDamage;

                    /*
                     * 只有真正扣到血的弹丸才进入汇总。
                     * 敌人死亡后，后续弹丸即使仍碰到 Collider，
                     * TakeDamage 返回0，不再增加普通命中特效。
                     */
                    if (actualDamage > 0f)
                    {
                        if (!enemyHitsThisShot.TryGetValue(
                                enemyTarget,
                                out HipfireEnemyHitData hitData
                            ))
                        {
                            hitData =
                                new HipfireEnemyHitData();

                            enemyHitsThisShot.Add(
                                enemyTarget,
                                hitData
                            );
                        }

                        hitData.hitPointSum +=
                            hit.point;

                        hitData.hitNormalSum +=
                            hit.normal;

                        hitData.damagingPelletCount++;

                        hitData.totalDamage +=
                            actualDamage;
                    }
                }
                else
                {
                    HandleNonEnemyTargetHit(
                        hit,
                        switchesHitThisShot,
                        fallbackEnemiesKilled
                    );
                }
            }
            else
            {
                endPoint =
                    pelletRay.GetPoint(
                        hipfireRange
                    );
            }

            Transform visualOrigin =
                GetPelletVisualOrigin(
                    pelletIndex
                );

            if (visualOrigin != null)
            {
                StartCoroutine(
                    SpawnTracerRoutine(
                        visualOrigin.position,
                        endPoint,
                        tracerWidth,
                        tracerSpeed
                    )
                );
            }
        }

        /*
         * 所有弹丸伤害结算结束后，
         * 每个仍然存活的敌人最多生成一次普通命中特效。
         */
        SpawnConsolidatedHipfireHitEffects(
            enemyHitsThisShot
        );

        if (logDamageResults)
        {
            Debug.Log(
                $"[腰射] 敌人命中弹丸：" +
                $"{lastHipfirePelletsHit}/{safePelletCount}，" +
                $"实际总伤害：" +
                $"{lastHipfireDamageDealt:F0}"
            );
        }
    }

    private void SpawnConsolidatedHipfireHitEffects(
        Dictionary<
            EnemyTarget,
            HipfireEnemyHitData
        > enemyHits
    )
    {
        if (enemyHitEffect == null ||
            enemyHits == null ||
            enemyHits.Count == 0)
        {
            return;
        }

        foreach (
            KeyValuePair<
                EnemyTarget,
                HipfireEnemyHitData
            > pair in enemyHits
        )
        {
            EnemyTarget target =
                pair.Key;

            HipfireEnemyHitData hitData =
                pair.Value;

            if (target == null ||
                target.IsDead ||
                hitData == null ||
                hitData.damagingPelletCount <= 0)
            {
                continue;
            }

            Vector3 averageHitPoint =
                hitData.hitPointSum /
                hitData.damagingPelletCount;

            Vector3 averageHitNormal =
                hitData.hitNormalSum;

            if (averageHitNormal.sqrMagnitude <=
                0.0001f)
            {
                averageHitNormal =
                    Vector3.up;
            }
            else
            {
                averageHitNormal.Normalize();
            }

            SpawnImpactEffect(
                enemyHitEffect,
                averageHitPoint,
                averageHitNormal
            );
        }
    }

    /// <summary>
    /// 第一颗弹丸位于中心。
    /// 其他弹丸按照黄金角分布在圆形区域内。
    /// </summary>
    private Vector2 CalculatePelletOffset(
        int pelletIndex,
        int totalPellets,
        float rotationOffset
    )
    {
        if (pelletIndex <= 0 ||
            totalPellets <= 1)
        {
            return Vector2.zero;
        }

        int outerPelletCount =
            totalPellets - 1;

        int outerIndex =
            pelletIndex - 1;

        float normalizedRadius =
            (outerIndex + 1f) /
            Mathf.Max(
                1f,
                outerPelletCount
            );

        float radius =
            Mathf.Sqrt(
                normalizedRadius
            );

        float angle =
            rotationOffset +
            outerIndex *
            GoldenAngleRadians;

        return new Vector2(
            Mathf.Cos(angle) * radius,
            Mathf.Sin(angle) * radius
        );
    }

    private Transform GetPelletVisualOrigin(
        int pelletIndex
    )
    {
        bool useLeftBarrel =
            pelletIndex % 2 == 0;

        Transform preferredOrigin =
            useLeftBarrel
                ? leftBarrelPoint
                : rightBarrelPoint;

        if (preferredOrigin != null)
        {
            return preferredOrigin;
        }

        if (leftBarrelPoint != null)
        {
            return leftBarrelPoint;
        }

        if (rightBarrelPoint != null)
        {
            return rightBarrelPoint;
        }

        return centerBarrelPoint;
    }

    // =========================================================
    // 非 EnemyTarget 命中处理
    // =========================================================

    private void HandleNonEnemyTargetHit(
        RaycastHit hit,
        HashSet<TargetSwitch> switchesHitThisShot,
        HashSet<GameObject> fallbackEnemiesKilled
    )
    {
        TargetSwitch targetSwitch =
            hit.collider.GetComponentInParent<TargetSwitch>();

        if (targetSwitch != null)
        {
            if (switchesHitThisShot == null ||
                !switchesHitThisShot.Contains(
                    targetSwitch
                ))
            {
                targetSwitch.OnHit();

                switchesHitThisShot?.Add(
                    targetSwitch
                );
            }

            return;
        }

        if (hit.collider.CompareTag("Enemy"))
        {
            GameObject fallbackEnemy =
                hit.collider.gameObject;

            if (fallbackEnemiesKilled != null &&
                fallbackEnemiesKilled.Contains(
                    fallbackEnemy
                ))
            {
                return;
            }

            SpawnImpactEffect(
                enemyHitEffect,
                hit.point,
                hit.normal
            );

            if (DashController.Instance != null)
            {
                DashController.Instance.OnEnemyKilled(
                    hit.point
                );
            }

            fallbackEnemiesKilled?.Add(
                fallbackEnemy
            );

            Destroy(
                fallbackEnemy
            );

            return;
        }

        if (hit.collider.CompareTag("Environment") ||
            hit.collider.CompareTag("Untagged"))
        {
            SpawnImpactEffect(
                environmentHitEffect,
                hit.point,
                hit.normal
            );
        }
    }

    private void SpawnImpactEffect(
        GameObject effectPrefab,
        Vector3 position,
        Vector3 normal
    )
    {
        if (effectPrefab == null)
        {
            return;
        }

        Quaternion rotation =
            normal.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(
                    normal.normalized
                )
                : Quaternion.identity;

        Instantiate(
            effectPrefab,
            position,
            rotation
        );
    }

    // =========================================================
    // 曳光对象池
    // =========================================================

    private LineRenderer GetPooledTracer()
    {
        foreach (LineRenderer tracer
                 in _tracerPool)
        {
            if (tracer == null)
            {
                continue;
            }

            if (!tracer.gameObject.activeInHierarchy)
            {
                tracer.gameObject.SetActive(
                    true
                );

                return tracer;
            }
        }

        GameObject tracerObject =
            new GameObject(
                "BulletTracer"
            );

        tracerObject.transform.SetParent(
            transform
        );

        LineRenderer line =
            tracerObject.AddComponent<LineRenderer>();

        line.useWorldSpace = true;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;

        line.shadowCastingMode =
            ShadowCastingMode.Off;

        line.receiveShadows = false;

        if (tracerMaterial != null)
        {
            line.material =
                tracerMaterial;
        }
        else
        {
            Shader fallbackShader =
                Shader.Find(
                    "Sprites/Default"
                );

            if (fallbackShader != null)
            {
                line.material =
                    new Material(
                        fallbackShader
                    );
            }
        }

        _tracerPool.Add(
            line
        );

        return line;
    }

    private IEnumerator SpawnTracerRoutine(
        Vector3 startPosition,
        Vector3 endPosition,
        float width,
        float speed
    )
    {
        LineRenderer line =
            GetPooledTracer();

        line.startWidth = 0f;
        line.endWidth = width;
        line.positionCount = 2;

        float totalDistance =
            Vector3.Distance(
                startPosition,
                endPosition
            );

        float movedDistance = 0f;

        while (movedDistance <
               totalDistance)
        {
            movedDistance +=
                speed *
                Time.unscaledDeltaTime;

            Vector3 headPosition =
                Vector3.MoveTowards(
                    startPosition,
                    endPosition,
                    movedDistance
                );

            Vector3 tailPosition =
                Vector3.MoveTowards(
                    startPosition,
                    endPosition,
                    Mathf.Max(
                        0f,
                        movedDistance -
                        tracerLength
                    )
                );

            line.SetPosition(
                0,
                tailPosition
            );

            line.SetPosition(
                1,
                headPosition
            );

            yield return null;
        }

        line.gameObject.SetActive(
            false
        );
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        maxAmmo =
            Mathf.Max(
                1,
                maxAmmo
            );

        hipfirePelletCount =
            Mathf.Max(
                1,
                hipfirePelletCount
            );

        pelletDamage =
            Mathf.Max(
                0f,
                pelletDamage
            );

        slugDamage =
            Mathf.Max(
                0f,
                slugDamage
            );

        spreadAngle =
            Mathf.Max(
                0f,
                spreadAngle
            );

        hipfireRange =
            Mathf.Max(
                0.1f,
                hipfireRange
            );

        slugRange =
            Mathf.Max(
                0.1f,
                slugRange
            );
    }
#endif
}