using UnityEngine;
using UnityEngine.InputSystem;

public class CoreSocket : MonoBehaviour
{
    [Header("机关关联")]
    [Tooltip("把场景里控制大门的总控组件拖到这里")]
    public FinalGateManager gateManager;
    [Tooltip("在这个插槽模型下建一个默认隐藏的球体模型，拖到这里。塞入成功后它会显形！")]
    public GameObject visualCoreMesh; 

    [Header("互动特效")]
    public GameObject insertEffectPrefab;
    public AudioClip insertSound;

    private bool _isFilled = false;
    private bool _playerInside = false;

    void Start()
    {
        // 游戏一开始，插槽里应该是空的
        if (visualCoreMesh != null) visualCoreMesh.SetActive(false);
    }

    void Update()
    {
        // 只有玩家在范围内、插槽还空着、且按下了交互键 E
        if (_playerInside && !_isFilled && Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryPlugCore();
        }
    }

    private void TryPlugCore()
    {
        if (GameManager.Instance == null) return;

        // 检查玩家手里有没有球
        if (GameManager.Instance.GetCurrentBallsCount() > 0)
        {
            // 成功扣除玩家心里的核心
            if (GameManager.Instance.TrySpendBall())
            {
                _isFilled = true;
                
                // 核心显形！
                if (visualCoreMesh != null) visualCoreMesh.SetActive(true);

                // 反馈爆浆
                if (insertEffectPrefab != null) Instantiate(insertEffectPrefab, transform.position, insertEffectPrefab.transform.rotation);
                if (insertSound != null) AudioSource.PlayClipAtPoint(insertSound, transform.position);

                // 汇报给大门总控
                if (gateManager != null) gateManager.OnSocketFilled();

                Debug.Log($"🟩 [插槽] {gameObject.name} 成功置入能量核！");
            }
        }
        else
        {
            Debug.Log("⚠️ [插槽] 你身上没有多余的核心！快去地图里寻找！");
        }
    }

    // 利用触发器检测玩家是否站在插槽前
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInside = true;
            // 💡 这里后续你可以加上“请按 E 放入核心”的 UI 提示
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInside = false;
        }
    }
}