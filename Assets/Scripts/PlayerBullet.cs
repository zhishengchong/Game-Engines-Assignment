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
            if (enemyDeathEffect != null)
            {
                Instantiate(enemyDeathEffect, other.transform.position, Quaternion.identity);
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                DashController dashCtrl = player.GetComponent<DashController>();
                if (dashCtrl != null) dashCtrl.OnEnemyKilled(other.transform.position);
            }

            Destroy(other.gameObject);
            Destroy(gameObject); 
        }
        // ==========================================
        // 【核心新增】：检测是否打中了靶心开关
        // ==========================================
        else if (other.CompareTag("TargetSwitch"))
        {
            TargetSwitch target = other.GetComponent<TargetSwitch>();
            if (target != null)
            {
                target.OnHit(); // 触发靶子！
            }
            Destroy(gameObject); // 子弹打中靶子后销毁
        }
        else if (other.CompareTag("Environment"))
        {
            Destroy(gameObject);
        }
    }
}