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

    void Start()
    {
        // 初始化弹药
        currentAmmo = maxAmmo;

        // 开启刚体平滑插值
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        // ==========================================
        // 【音效初始化】：自动在武器身上挂载一个喇叭
        // ==========================================
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null) 
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false; // 防止游戏一开始就乱响
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
            
            // ==========================================
            // 【播放音效】：使用 PlayOneShot 允许声音叠加（连发不会互相打断）
            // ==========================================
            if (shootSound != null && _audioSource != null)
            {
                // 随机稍微改变一下音高，让每次开火听起来不那么单调机械
                _audioSource.pitch = Random.Range(0.9f, 1.1f);
                _audioSource.PlayOneShot(shootSound);
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