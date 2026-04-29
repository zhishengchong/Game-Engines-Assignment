using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    public float speed = 40f;       
    public float lifeTime = 3f;     

    [Header("视觉反馈")]
    [Tooltip("把做好的敌人死亡粒子特效预制体拖到这里")]
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
            // ==========================================
            // 【视觉反馈】：在敌人死亡的位置生成爆炸碎片！
            // ==========================================
            if (enemyDeathEffect != null)
            {
                Instantiate(enemyDeathEffect, other.transform.position, Quaternion.identity);
            }

            // 通知玩家获得了 Dash 权限
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                DashController dashCtrl = player.GetComponent<DashController>();
                if (dashCtrl != null)
                {
                    dashCtrl.OnEnemyKilled(other.transform.position);
                }
            }

            // 击杀敌人并销毁子弹
            Destroy(other.gameObject);
            Destroy(gameObject); 
        }
        else if (other.CompareTag("Environment"))
        {
            Destroy(gameObject);
        }
    }
}