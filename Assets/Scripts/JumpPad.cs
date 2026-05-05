using UnityEngine;
using StarterAssets; 

public class JumpPad : MonoBehaviour
{
    [Header("跳板设置 (Apex 手感)")]
    public float upwardForce = 20f;
    public float forwardForce = 35f; 
    
    [Header("音效反馈")]
    public AudioClip bounceSound;

    [Header("管道专用设置 (仅限管道前的跳板使用)")]
    [Tooltip("勾选此项，踩下跳板的瞬间就会撑爆 FOV！")]
    public bool isTunnelStarter = false;
    public float tunnelFOV = 110f;
    [Tooltip("镜头拉伸的速度 (越大拉伸越快，建议 25-35 模拟瞬间冲刺)")]
    public float fovExpandSpeed = 30f;
    
    private Rigidbody _rb;
    private bool _hasLanded = false; 

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_hasLanded) return;
        if (collision.transform.root.CompareTag("Player") || collision.gameObject.CompareTag("Player")) return; 

        if (collision.gameObject.CompareTag("Environment") || collision.gameObject.CompareTag("Untagged"))
        {
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.isKinematic = true; 
                _hasLanded = true;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            FirstPersonController fpc = other.GetComponent<FirstPersonController>();
            
            if (fpc != null)
            {
                // 1. 施加物理推力
                Vector3 launchVelocity = (other.transform.forward * forwardForce) + (Vector3.up * upwardForce);
                fpc.ApplyJumpPadForce(launchVelocity);
                
                // ==========================================
                // 2. 【核心新增】：起跳瞬间，极速撑爆镜头！
                // ==========================================
                if (isTunnelStarter && DashController.Instance != null)
                {
                    DashController.Instance.HoldFOV(tunnelFOV, fovExpandSpeed);
                }
                
                if (bounceSound != null)
                {
                    AudioSource.PlayClipAtPoint(bounceSound, transform.position);
                }
            }
        }
    }
}