using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    public float speed = 40f;       
    public float lifeTime = 3f;     

    [Header("视觉反馈")]
    public GameObject enemyDeathEffect; 

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            // 1. 生成爆炸特效 (你做的那个超帅的赛博碎片)
            if (enemyDeathEffect != null)
            {
                Instantiate(enemyDeathEffect, other.transform.position, Quaternion.identity);
            }

            // 2. 通知玩家获得 Dash 权限
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                DashController dashCtrl = player.GetComponent<DashController>();
                if (dashCtrl != null) dashCtrl.OnEnemyKilled(other.transform.position);
            }

            // (已成功切除 JumpPadSkill 减CD的相关报错代码)

            // 3. 通知 GameManager 检查胜利条件
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CheckWinCondition();
            }

            // 4. 销毁敌人和子弹
            Destroy(other.gameObject);
            Destroy(gameObject); 
        }
        else if (other.CompareTag("Environment"))
        {
            Destroy(gameObject);
        }
    }
}