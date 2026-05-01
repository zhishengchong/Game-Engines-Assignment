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
    public Text winTextUI; 

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
    }

    // --- 失败逻辑 ---
    public void PlayerDied()
    {
        if (_isLevelEnded) return; 
        _isLevelEnded = true;

        Time.timeScale = 0f;
        DisablePlayerControls(); // 失败时禁用控制
        StartCoroutine(RestartLevelRoutine());
    }

    // --- 胜利逻辑 ---
    public void CheckWinCondition()
    {
        if (_isLevelEnded) return;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        if (enemies.Length <= 1)
        {
            _isLevelEnded = true;
            StartCoroutine(LevelClearedRoutine());
        }
    }

    private IEnumerator LevelClearedRoutine()
    {
        Debug.Log("【LEVEL CLEARED】关卡清除！");

        // ==========================================
        // 【补丁】：胜利瞬间，立刻锁死玩家的所有输入和移动
        // ==========================================
        DisablePlayerControls();

        // 胜利瞬间进入极慢动作
        Time.timeScale = 0.1f;

        if (winTextUI != null)
        {
            winTextUI.gameObject.SetActive(true);
            winTextUI.text = "LEVEL CLEARED\nSUPER! HOT!";
        }

        // 等待 3 秒（现实时间）
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

    // 【通用方法】：统一禁用玩家的所有交互
    private void DisablePlayerControls()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // 1. 禁用移动和视角
            var fpc = player.GetComponent<StarterAssets.FirstPersonController>();
            if (fpc != null) fpc.enabled = false;

            // 2. 禁用武器系统
            var weaponInt = player.GetComponent<WeaponInteraction>();
            if (weaponInt != null) weaponInt.enabled = false;

            // 3. 禁用 Dash 系统
            var dashCtrl = player.GetComponent<DashController>();
            if (dashCtrl != null) dashCtrl.enabled = false;

            // 4. 彻底切断输入源
            var playerInput = player.GetComponent<PlayerInput>();
            if (playerInput != null) playerInput.enabled = false;

            // 5. 额外保险：如果是胜利状态，鼠标指针可以解锁（方便以后点击 UI）
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}