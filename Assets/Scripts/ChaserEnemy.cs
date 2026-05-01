using UnityEngine;
using UnityEngine.AI; 

public class ChaserEnemy : MonoBehaviour
{
    private NavMeshAgent _agent;
    private Transform _player;

    [Header("攻击设置")]
    [Tooltip("丧尸的攻击距离，只要小于这个距离直接秒杀玩家")]
    public float attackRange = 1.5f; 

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
            // 1. 寻路追击
            _agent.SetDestination(_player.position);

            // ==========================================
            // 【核心修复】：抛弃物理碰撞，直接用空间距离算法！
            // ==========================================
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