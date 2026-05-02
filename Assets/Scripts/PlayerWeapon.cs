using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    [Header("武器属性")]
    public int maxAmmo = 3;             
    public int currentAmmo;
    
    [Header("射击配置")]
    public GameObject playerBulletPrefab; 
    public Transform firePoint;           

    [Header("音效设置")]
    [Tooltip("把你找好的开火音效拖到这里")]
    public AudioClip shootSound;
    private AudioSource _audioSource;

    // 新增：需要获取主摄像机来进行准星校准
    private Camera _mainCamera;

    void Start()
    {
        currentAmmo = maxAmmo;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null) 
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false; 
        }

        // 初始化摄像机
        _mainCamera = Camera.main;
    }

    public bool TryShoot()
    {
        if (currentAmmo > 0)
        {
            currentAmmo--;
            
            if (playerBulletPrefab != null && firePoint != null && _mainCamera != null)
            {
                // ==========================================
                // 【核心修复】：FPS 准星视差校准系统
                // ==========================================
                
                // 1. 从屏幕正中心（准星位置）射出一条隐形射线
                Ray ray = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                RaycastHit hit;
                
                // 2. 确定子弹最终要飞向的“目标点”
                Vector3 targetPoint;
                if (Physics.Raycast(ray, out hit))
                {
                    // 如果准星指着墙壁或敌人，目标点就是打中的那个点
                    targetPoint = hit.point;
                }
                else
                {
                    // 如果准星指着天空（什么都没打中），目标点就是极远处的一个点
                    targetPoint = ray.GetPoint(1000f);
                }

                // 3. 计算从【枪口】到【目标点】的确切飞行方向
                Vector3 directionWithoutSpread = targetPoint - firePoint.position;

                // 4. 生成子弹，并强制它看向那个目标点！
                GameObject currentBullet = Instantiate(playerBulletPrefab, firePoint.position, Quaternion.identity);
                currentBullet.transform.forward = directionWithoutSpread.normalized;
            }
            
            if (shootSound != null && _audioSource != null)
            {
                _audioSource.pitch = Random.Range(0.9f, 1.1f);
                _audioSource.PlayOneShot(shootSound);
            }
            
            return true;
        }
        else
        {
            Debug.Log("没子弹了！快把它扔出去！");
            return false;
        }
    }
}