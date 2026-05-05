using UnityEngine;
using StarterAssets; 
using System.Collections;

public class TunnelRailSystem : MonoBehaviour
{
    [Header("📍 轨道定位")]
    public Transform startPoint;
    public Transform endPoint;

    [Header("🌪️ 牵引流体力学")]
    public float initialSpeed = 20f;
    public float acceleration = 50f;
    public float suctionSmoothness = 10f;
    public float exitForce = 40f;

    [Header("✨ 视觉滤镜")]
    public float chromaticDuration = 2f; 

    private bool _isOccupied = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !_isOccupied)
        {
            StartCoroutine(TractorBeamRoutine(other.gameObject));
        }
    }

    private IEnumerator TractorBeamRoutine(GameObject player)
    {
        _isOccupied = true;

        CharacterController cc = player.GetComponent<CharacterController>();
        FirstPersonController fpc = player.GetComponent<FirstPersonController>();

        if (cc != null) cc.enabled = false;

        // 【优化】：不再在这里触发 FOV 变化了！只触发全屏色差特效。
        if (VisualFeedbackController.Instance != null) 
        {
            VisualFeedbackController.Instance.TriggerTunnelEffect(chromaticDuration);
        }

        Vector3 currentPos = player.transform.position; 
        Vector3 tunnelDir = (endPoint.position - startPoint.position).normalized;
        float currentSpeed = cc != null ? Mathf.Max(cc.velocity.magnitude, initialSpeed) : initialSpeed;

        while (Vector3.Distance(currentPos, endPoint.position) > 1.5f)
        {
            currentSpeed += acceleration * Time.unscaledDeltaTime;

            Vector3 offsetFromStart = currentPos - startPoint.position;
            float distanceAlongLine = Vector3.Dot(offsetFromStart, tunnelDir);
            Vector3 pointOnLine = startPoint.position + tunnelDir * distanceAlongLine;

            Vector3 targetPullPoint = pointOnLine + tunnelDir * suctionSmoothness;
            Vector3 moveDir = (targetPullPoint - currentPos).normalized;
            
            currentPos += moveDir * currentSpeed * Time.unscaledDeltaTime;
            player.transform.position = currentPos;

            if (VisualFeedbackController.Instance != null) 
            {
                VisualFeedbackController.Instance.TriggerTunnelEffect(0.2f);
            }

            yield return null;
        }

        player.transform.position = endPoint.position;
        if (cc != null) cc.enabled = true; 
        
        if (fpc != null) fpc.ApplyJumpPadForce(tunnelDir * exitForce);

        // ==========================================
        // 【视觉收尾】：到达终点抛出时，通知镜头开始缓缓恢复
        // ==========================================
        if (DashController.Instance != null) 
        {
            DashController.Instance.ReleaseFOV();
        }

        _isOccupied = false;
    }
}