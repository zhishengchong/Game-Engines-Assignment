using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.InputSystem;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI 引用")]
    public Text deathTextUI;
    public Text winTextUI;

    [Header("收集系统 (New)")]
    public TextMeshProUGUI ballCountTextUI; // 用来显示吃了多少个球的 UI
    public int totalBallsRequired = 3; // 过关需要收集的球数，随时可以在面板修改
    private int _currentBalls = 0; // 当前已经收集的数量

    private bool _isLevelEnded = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (deathTextUI != null) deathTextUI.gameObject.SetActive(false);
        if (winTextUI != null) winTextUI.gameObject.SetActive(false);

        // 游戏刚开始，刷新一下UI显示为 "0 / 3"
        UpdateBallUI();
    }

    // ==========================================
    // 【核心新增】：玩家吃到球时触发
    // ==========================================
    public void AddBall()
    {
        if (_isLevelEnded) return;

        _currentBalls++;       // 收集数 + 1
        UpdateBallUI();        // 刷新屏幕上的数字

        // 检查：是否收集够了设定的总数？
        if (_currentBalls >= totalBallsRequired)
        {
            _isLevelEnded = true;
            StartCoroutine(LevelClearedRoutine()); // 触发胜利大招！
        }
    }

    // 刷新屏幕 UI 的函数
    private void UpdateBallUI()
    {
        if (ballCountTextUI != null)
        {
            ballCountTextUI.text = "Balls: " + _currentBalls + " / " + totalBallsRequired;
        }
    }

    // --- 失败逻辑 ---
    public void PlayerDied()
    {
        if (_isLevelEnded) return;
        _isLevelEnded = true;

        Time.timeScale = 0f;
        DisablePlayerControls();
        StartCoroutine(RestartLevelRoutine());
    }

    private IEnumerator LevelClearedRoutine()
    {
        Debug.Log("【LEVEL CLEARED】收集完毕，关卡清除！");
        DisablePlayerControls();
        Time.timeScale = 0.1f;

        if (winTextUI != null)
        {
            winTextUI.gameObject.SetActive(true);
            winTextUI.text = "LEVEL CLEARED\nALL BALLS COLLECTED!";
        }

        yield return new WaitForSecondsRealtime(3f);

        Time.timeScale = 1f;
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private IEnumerator RestartLevelRoutine()
    {
        if (deathTextUI != null) deathTextUI.gameObject.SetActive(true);
        float countdown = 1.5f;

        while (countdown > 0f)
        {
            if (deathTextUI != null)
                deathTextUI.text = "YOU DIED\nReset Time: " + countdown.ToString("F1") + "s";

            countdown -= Time.unscaledDeltaTime;
            yield return null;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void DisablePlayerControls()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var fpc = player.GetComponent<StarterAssets.FirstPersonController>();
            if (fpc != null) fpc.enabled = false;

            var weaponInt = player.GetComponent<WeaponInteraction>();
            if (weaponInt != null) weaponInt.enabled = false;

            var dashCtrl = player.GetComponent<DashController>();
            if (dashCtrl != null) dashCtrl.enabled = false;

            var playerInput = player.GetComponent<PlayerInput>();
            if (playerInput != null) playerInput.enabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}