using UnityEngine;
using UnityEngine.AI; 

public class ChaserEnemy : MonoBehaviour
{
    private NavMeshAgent _agent;
    private Transform _player;

    [Header("攻击设置")]
    [Tooltip("丧尸的攻击距离，只要小于这个距离直接秒杀玩家")]
    public float attackRange = 1.5f; 

    [Header("性能优化")]
    [Tooltip("每隔多少秒重新计算一次寻路路径？(0.2秒是性能与反应的完美平衡点)")]
    public float pathUpdateInterval = 0.2f;
    private float _pathUpdateTimer = 0f;

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _player = playerObj.transform;
        }
    }

    void Update()
    {
        if (_player != null && _agent != null && _agent.isActiveAndEnabled)
        {
            // ==========================================
            // 【核心优化】：寻路节流阀 (不再每帧计算路径)
            // ==========================================
            _pathUpdateTimer -= Time.deltaTime;
            if (_pathUpdateTimer <= 0f)
            {
                _agent.SetDestination(_player.position);
                _pathUpdateTimer = pathUpdateInterval; // 重置计时器
            }

            // 空间距离算法判定秒杀
            float distanceToPlayer = Vector3.Distance(transform.position, _player.position);
            
            if (distanceToPlayer <= attackRange)
            {
                TriggerPlayerDeath();
            }
        }
    }

    private void TriggerPlayerDeath()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayerDied();
        }
    }
}