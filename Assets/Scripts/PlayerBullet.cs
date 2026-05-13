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
            // 1. 生成爆炸特效
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

            // (已切除旧版的 GameManager.CheckWinCondition()，现在打死敌人不再触发胜利)

            // 3. 销毁敌人和子弹
            Destroy(other.gameObject);
            Destroy(gameObject);
        }
        else if (other.CompareTag("Environment"))
        {
            Destroy(gameObject);
        }
    }
}