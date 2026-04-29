using UnityEngine;

public class TurretEnemy : MonoBehaviour
{
    [Header("感知设置 (AI Perception)")]
    public float visionRange = 20f;        
    public float visionAngle = 90f;        
    public float proximityRadius = 3f;
    public LayerMask obstacleMask; 

    [Header("战斗设置")]
    public GameObject bulletPrefab;        
    public Transform firePoint;            
    public float fireRate = 1.5f;          
    public float rotationSpeed = 120f;     

    [Header("AI 状态")]
    [SerializeField] private bool _hasSpottedPlayer = false;
    
    // 【核心新增】：记录玩家消失前的最后位置
    private Vector3 _lastKnownPosition;

    private Transform _player;
    private float _fireTimer;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _player = playerObj.transform;
        }
    }

    void Update()
    {
        if (_player == null) return;

        // 1. 每帧都检查当前是否能清晰地看到玩家（视线无遮挡）
        bool currentLineOfSight = CanSeeOrSensePlayer();

        if (currentLineOfSight)
        {
            // 看到玩家了！激活警觉状态，并更新最后看到的位置
            _hasSpottedPlayer = true;
            _lastKnownPosition = _player.position;
            
            // 传参 true，允许开火
            EngagePlayer(true); 
        }
        else if (_hasSpottedPlayer)
        {
            // 玩家躲进了掩体，失去了视线，但敌人依然记得玩家
            // 传参 false，只瞄准最后看到的位置，不开火
            EngagePlayer(false); 
        }
    }

    // 将判断逻辑拆分得更清晰
    private bool CanSeeOrSensePlayer()
    {
        Vector3 dirToPlayer = (_player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, _player.position);

        bool inRange = false;

        // 1. 在背后近战范围内
        if (distanceToPlayer <= proximityRadius) 
        {
            inRange = true;
        }
        // 2. 或者在视锥体范围内
        else if (distanceToPlayer <= visionRange)
        {
            float angleToPlayer = Vector3.Angle(transform.forward, dirToPlayer);
            if (angleToPlayer <= visionAngle / 2f)
            {
                inRange = true;
            }
        }

        // 3. 终极验证：如果距离和角度都对，再检查中间有没有墙！
        if (inRange)
        {
            return CheckLineOfSight();
        }

        return false;
    }

    // 专属的防穿墙射线检测
    private bool CheckLineOfSight()
    {
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;
        Vector3 rayEnd = _player.position + Vector3.up * 0.5f;
        
        // 如果中间没有碰到 obstacleMask (Environment)，说明视线畅通
        return !Physics.Linecast(rayStart, rayEnd, obstacleMask);
    }

    // 根据是否有视线来决定交战策略
    private void EngagePlayer(bool hasLineOfSight)
    {
        // 如果能看到，就瞄准玩家当前位置；如果躲起来了，就瞄准他消失的那个点
        Vector3 targetPos = hasLineOfSight ? _player.position : _lastKnownPosition;
        Vector3 dirToTarget = targetPos - transform.position;
        dirToTarget.y = 0; 
        
        // 执行转向
        if (dirToTarget.sqrMagnitude > 0.001f)
        {
            float targetAngle = Mathf.Atan2(dirToTarget.x, dirToTarget.z) * Mathf.Rad2Deg;
            float currentAngle = transform.eulerAngles.y;
            float step = rotationSpeed * Time.deltaTime;
            float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, step);
            transform.rotation = Quaternion.Euler(0, newAngle, 0);
        }

        // 只有在确定没有墙壁遮挡的情况下，才扣动扳机
        if (hasLineOfSight)
        {
            _fireTimer -= Time.deltaTime;
            if (_fireTimer <= 0f)
            {
                Shoot();
                _fireTimer = fireRate;
            }
        }
        else
        {
            // 可选：当玩家躲回掩体时，重置开火冷却。
            // 这样玩家探头出来的瞬间，敌人不会因为憋了一发子弹而直接“秒杀”玩家，给玩家反应时间。
            _fireTimer = fireRate; 
        }
    }

    private void Shoot()
    {
        if (bulletPrefab != null && firePoint != null)
        {
            Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, proximityRadius);
        Gizmos.color = Color.yellow;
        Vector3 leftRayRotation = Quaternion.Euler(0, -visionAngle / 2f, 0) * transform.forward;
        Vector3 rightRayRotation = Quaternion.Euler(0, visionAngle / 2f, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, leftRayRotation * visionRange);
        Gizmos.DrawRay(transform.position, rightRayRotation * visionRange);
    }
}