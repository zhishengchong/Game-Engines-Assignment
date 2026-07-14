using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class CoreCountGate : MonoBehaviour
{
    [Header("门引用")]
    [Tooltip("需要被打开的门。把挂有 GlitchDoor 的门拖到这里。")]
    public GlitchDoor targetDoor;

    [Header("解锁条件")]
    [Min(1)]
    [Tooltip("玩家当前至少需要持有多少个 Core。")]
    public int requiredCoreCount = 2;

    [Tooltip("多久检查一次 Core 数量。0.1 秒已经足够及时。")]
    [Min(0.02f)]
    public float checkInterval = 0.1f;

    [Header("可选视觉反馈")]
    [Tooltip("可选。显示当前 Core 数量，例如 CORE ACCESS 01 / 02。")]
    public TMP_Text statusText;

    public Color lockedTextColor = new Color(1f, 0.2f, 0.25f, 1f);
    public Color unlockedTextColor = new Color(0.1f, 1f, 0.9f, 1f);

    [Tooltip("门未解锁时显示的物体，例如红色指示灯。")]
    public GameObject[] lockedIndicators;

    [Tooltip("门解锁后显示的物体，例如青色指示灯。")]
    public GameObject[] unlockedIndicators;

    [Header("可选解锁反馈")]
    public GameObject unlockEffectPrefab;
    public Transform unlockEffectPoint;
    public AudioClip unlockSound;

    [Range(0f, 1f)]
    public float unlockSoundVolume = 1f;

    [Header("终端日志")]
    [Tooltip("解锁时是否在现有 Terminal Log 中显示信息。")]
    public bool sendTerminalLog = true;

    [Tooltip("显示在日志里的区域名称。")]
    public string gateDisplayName = "CORE ACCESS GATE";

    [Header("Debug")]
    public bool showDebugLogs = true;

    private bool _hasUnlocked = false;
    private float _checkTimer = 0f;
    private int _lastDisplayedCount = -1;

    private void Awake()
    {
        if (targetDoor == null)
        {
            targetDoor = GetComponent<GlitchDoor>();
        }

        if (targetDoor == null)
        {
            targetDoor = GetComponentInChildren<GlitchDoor>();
        }

        SetUnlockedVisualState(false);
    }

    private void Start()
    {
        // 立即检查一次。
        // 如果玩家进入场景时已经持有 2 个 Core，门会直接打开。
        EvaluateGate();
    }

    private void Update()
    {
        if (_hasUnlocked)
        {
            return;
        }

        _checkTimer -= Time.unscaledDeltaTime;

        if (_checkTimer > 0f)
        {
            return;
        }

        _checkTimer = checkInterval;
        EvaluateGate();
    }

    private void EvaluateGate()
    {
        if (_hasUnlocked)
        {
            return;
        }

        if (GameManager.Instance == null)
        {
            UpdateStatusDisplay(0);
            return;
        }

        int currentCoreCount = GameManager.Instance.GetCurrentBallsCount();

        UpdateStatusDisplay(currentCoreCount);

        if (currentCoreCount >= requiredCoreCount)
        {
            UnlockGate(currentCoreCount);
        }
    }

    private void UnlockGate(int currentCoreCount)
    {
        if (_hasUnlocked)
        {
            return;
        }

        // 先锁住状态，防止连续多次调用 OpenDoor。
        _hasUnlocked = true;

        SetUnlockedVisualState(true);
        UpdateStatusDisplay(currentCoreCount);

        if (unlockEffectPrefab != null)
        {
            Vector3 spawnPosition = unlockEffectPoint != null
                ? unlockEffectPoint.position
                : transform.position;

            Quaternion spawnRotation = unlockEffectPoint != null
                ? unlockEffectPoint.rotation
                : transform.rotation;

            Instantiate(unlockEffectPrefab, spawnPosition, spawnRotation);
        }

        if (unlockSound != null)
        {
            Vector3 soundPosition = unlockEffectPoint != null
                ? unlockEffectPoint.position
                : transform.position;

            AudioSource.PlayClipAtPoint(
                unlockSound,
                soundPosition,
                unlockSoundVolume
            );
        }

        if (sendTerminalLog && TerminalLogManager.Instance != null)
        {
            TerminalLogManager.Instance.AddLog(
                $"{requiredCoreCount:00}/{requiredCoreCount:00}",
                "ACCESS GRANTED",
                $"■ {gateDisplayName}"
            );
        }

        if (showDebugLogs)
        {
            Debug.Log(
                $"🔓 [CoreCountGate] {gateDisplayName} 已解锁。" +
                $"当前 Core：{currentCoreCount}，要求：{requiredCoreCount}。"
            );
        }

        if (targetDoor != null)
        {
            targetDoor.OpenDoor();
        }
        else
        {
            Debug.LogWarning(
                $"⚠️ [CoreCountGate] {gameObject.name} 没有设置 Target Door，" +
                "条件已经满足，但无法打开门。"
            );
        }
    }

    private void UpdateStatusDisplay(int currentCoreCount)
    {
        if (statusText == null)
        {
            return;
        }

        if (_lastDisplayedCount == currentCoreCount && !_hasUnlocked)
        {
            return;
        }

        _lastDisplayedCount = currentCoreCount;

        int displayCount = Mathf.Clamp(
            currentCoreCount,
            0,
            requiredCoreCount
        );

        if (_hasUnlocked)
        {
            statusText.text =
                $"CORE ACCESS\n" +
                $"{requiredCoreCount:00} / {requiredCoreCount:00}\n" +
                $"ACCESS GRANTED";

            statusText.color = unlockedTextColor;
        }
        else
        {
            statusText.text =
                $"CORE ACCESS\n" +
                $"{displayCount:00} / {requiredCoreCount:00}";

            statusText.color = lockedTextColor;
        }
    }

    private void SetUnlockedVisualState(bool unlocked)
    {
        SetObjectsActive(lockedIndicators, !unlocked);
        SetObjectsActive(unlockedIndicators, unlocked);
    }

    private void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
        {
            return;
        }

        foreach (GameObject obj in objects)
        {
            if (obj != null)
            {
                obj.SetActive(active);
            }
        }
    }

    public bool IsUnlocked()
    {
        return _hasUnlocked;
    }

    public void ForceUnlockForTesting()
    {
        if (!_hasUnlocked)
        {
            int currentCount = GameManager.Instance != null
                ? GameManager.Instance.GetCurrentBallsCount()
                : requiredCoreCount;

            UnlockGate(currentCount);
        }
    }
}