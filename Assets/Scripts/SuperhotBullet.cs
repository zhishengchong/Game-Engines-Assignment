using UnityEngine;

public class SuperhotBullet : MonoBehaviour
{
    public float speed = 20f;       
    public float lifeTime = 5f;     

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
        // 1. 如果打中了玩家
        if (other.CompareTag("Player"))
        {
            // 通知 GameManager 触发死亡重载逻辑
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlayerDied();
            }
            
            // 销毁这发致命的子弹
            Destroy(gameObject);
        }
        // 2. 如果打中了墙壁等环境
        else if (other.CompareTag("Environment"))
        {
            Destroy(gameObject);
        }
    }
}