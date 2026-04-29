using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    [Header("武器属性")]
    public int maxAmmo = 3;             
    public int currentAmmo;
    
    [Header("射击配置")]
    public GameObject playerBulletPrefab; 
    public Transform firePoint;           

    void Start()
    {
        // 初始化弹药
        currentAmmo = maxAmmo;

        // ==========================================
        // 【开局防卡顿补丁】：强制开启刚体平滑插值
        // 确保武器在游戏刚开始、未被玩家触碰前，在慢动作下也能丝滑落地
        // ==========================================
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    public bool TryShoot()
    {
        if (currentAmmo > 0)
        {
            currentAmmo--;
            
            if (playerBulletPrefab != null && firePoint != null)
            {
                Instantiate(playerBulletPrefab, firePoint.position, firePoint.rotation);
            }
            
            Debug.Log("开火！剩余子弹: " + currentAmmo);
            return true;
        }
        else
        {
            Debug.Log("没子弹了！快把它扔出去！");
            return false;
        }
    }
}