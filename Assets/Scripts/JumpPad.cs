using UnityEngine;
using StarterAssets; 

public class JumpPad : MonoBehaviour
{
    [Header("跳板设置 (Apex 手感)")]
    public float upwardForce = 20f;
    public float forwardForce = 35f; 
    
    [Header("音效反馈")]
    public AudioClip bounceSound;
    
    private Rigidbody _rb;
    private bool _hasLanded = false; 

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }

    // ==========================================
    // 落地检测与“焊死”系统
    // ==========================================
    private void OnCollisionEnter(Collision collision)
    {
        if (_hasLanded) return;

        // ==========================================
        // 【核心修复】：根节点身世排查法！
        // 如果撞到的这个零件（比如那个Untagged的Capsule），
        // 它的最顶层父物体是 Player，说明它是玩家身上的东西，绝对不焊死！
        // ==========================================
        if (collision.transform.root.CompareTag("Player") || collision.gameObject.CompareTag("Player")) 
        {
            return; 
        }

        // 只有真正撞到环境或无标签的地板时，才落地焊死
        if (collision.gameObject.CompareTag("Environment") || collision.gameObject.CompareTag("Untagged"))
        {
            if (_rb != null)
            {
                // Unity 6 下标准写法
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.isKinematic = true; 
                
                _hasLanded = true;
                Debug.Log("deployment");
            }
        }
    }

    // ==========================================
    // 触发弹射系统 (这个不受影响，依旧能抓取玩家)
    // ==========================================
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            FirstPersonController fpc = other.GetComponent<FirstPersonController>();
            
            if (fpc != null)
            {
                Vector3 launchVelocity = (other.transform.forward * forwardForce) + (Vector3.up * upwardForce);
                fpc.ApplyJumpPadForce(launchVelocity);
                
                if (bounceSound != null)
                {
                    AudioSource.PlayClipAtPoint(bounceSound, transform.position);
                }
            }
        }
    }
}