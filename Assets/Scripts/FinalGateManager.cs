using UnityEngine;

public class FinalGateManager : MonoBehaviour
{
    [Header("大门与传送门组件")]
    public GlitchDoor finalGate; 
    [Tooltip("把挂有 GlitchPortal 脚本的传送门物体拖到这里")]
    public GlitchPortal glitchPortal;

    public int totalSocketsRequired = 3;
    private int _currentFilledSockets = 0;

    void Start()
    {
        // 【核心修改】：开局不隐藏传送门了，让它处于第 0 阶段（一条隐形的线或极小状态）
        if (glitchPortal != null)
        {
            glitchPortal.gameObject.SetActive(true);
            glitchPortal.SetStage(0); 
        }
    }

    // 每个插槽成功放入球时调用
    public void OnSocketFilled()
    {
        _currentFilledSockets++;

        // 【核心驱动】：每塞入一个核心，传送门就前进一步！
        if (glitchPortal != null)
        {
            glitchPortal.SetStage(_currentFilledSockets);
        }

        if (_currentFilledSockets >= totalSocketsRequired)
        {
            TriggerGateRelease();
        }
    }

    private void TriggerGateRelease()
    {
        Debug.Log("🔓 [大门总控] 3个核心全部就位！正在执行现实删除！");
        if (finalGate != null) finalGate.OpenDoor(); // 大门崩溃消失
    }
}