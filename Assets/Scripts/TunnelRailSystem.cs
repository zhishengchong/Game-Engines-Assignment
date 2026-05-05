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

        if (VisualFeedbackController.Instance != null) 
        {
            VisualFeedbackController.Instance.TriggerTunnelEffect(chromaticDuration);
        }

        Vector3 currentPos = player.transform.position; 
        Vector3 tunnelDir = (endPoint.position - startPoint.position).normalized;
        float currentSpeed = cc != null ? Mathf.Max(cc.velocity.magnitude, initialSpeed) : initialSpeed;

        // ==========================================
        // 【防穿模重构】：计算管道的总长度
        // ==========================================
        float totalTunnelLength = Vector3.Distance(startPoint.position, endPoint.position);
        float distanceAlongLine = 0f;

        // 只要玩家在管道方向上的投影距离还没超过总长度，就继续牵引
        while (distanceAlongLine < totalTunnelLength)
        {
            currentSpeed += acceleration * Time.unscaledDeltaTime;

            Vector3 offsetFromStart = currentPos - startPoint.position;
            // 计算玩家当前在管道中轴线上究竟走到了哪一步
            distanceAlongLine = Vector3.Dot(offsetFromStart, tunnelDir);

            // 【终极保险】：如果在这一帧由于速度过快，直接算到了终点之外，立刻打破循环！
            if (distanceAlongLine >= totalTunnelLength)
            {
                break;
            }

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

        // ==========================================
        // 到达终点后的收尾动作
        // ==========================================
        player.transform.position = endPoint.position;
        if (cc != null) cc.enabled = true; 
        
        if (fpc != null) fpc.ApplyJumpPadForce(tunnelDir * exitForce);

        if (DashController.Instance != null) 
        {
            DashController.Instance.ReleaseFOV();
        }

        _isOccupied = false;
    }
}