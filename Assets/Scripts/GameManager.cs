using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; 
using System.Collections;
using UnityEngine.InputSystem; 

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI 引用")]
    public Text deathTextUI; 

    private bool _isGameOver = false;

    void Awake()
    {
        // 单例模式初始化
        if (Instance == null) 
        {
            Instance = this;
        }
        else 
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 游戏开始时隐藏死亡 UI
        if (deathTextUI != null)
        {
            deathTextUI.gameObject.SetActive(false);
        }
    }

    // 当玩家被击中时调用的核心方法
    public void PlayerDied()
    {
        if (_isGameOver) return; 
        _isGameOver = true;

        // 1. 强制时间绝对静止
        Time.timeScale = 0f;

        // 2. 停用时间管理器，防止死亡瞬间通过按键恢复时间
        TimeManager timeManager = FindAnyObjectByType<TimeManager>();
        if (timeManager != null) 
        {
            timeManager.enabled = false;
        }

        // ==========================================
        // 【防僵尸补丁】：彻底瘫痪玩家的所有操作能力！
        // ==========================================
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // 禁用第一人称移动和视角转动
            var fpc = player.GetComponent<StarterAssets.FirstPersonController>();
            if (fpc != null) fpc.enabled = false;

            // 禁用武器交互（开火、丢枪、拾取）
            var weaponInt = player.GetComponent<WeaponInteraction>();
            if (weaponInt != null) weaponInt.enabled = false;

            // 【核心连线】：禁用冲刺能力，防止死亡后还能飞天
            var dashCtrl = player.GetComponent<DashController>();
            if (dashCtrl != null) dashCtrl.enabled = false;

            // 禁用输入系统
            var playerInput = player.GetComponent<PlayerInput>();
            if (playerInput != null) playerInput.enabled = false;
        }

        // 启动死亡重置协程
        StartCoroutine(RestartLevelRoutine());
    }

    private IEnumerator RestartLevelRoutine()
    {
        // 显示死亡提示
        if (deathTextUI != null)
        {
            deathTextUI.gameObject.SetActive(true);
        }

        // 设定倒计时时间
        float countdown = 1.5f;

        // 在 Time.timeScale 为 0 的情况下，使用 unscaledDeltaTime 进行倒数
        while (countdown > 0f)
        {
            if (deathTextUI != null)
            {
                deathTextUI.text = "YOU DIED\n重启倒计时: " + countdown.ToString("F1") + "s";
            }
            
            countdown -= Time.unscaledDeltaTime;
            yield return null; 
        }

        // 恢复时间缩放并重新加载场景
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}