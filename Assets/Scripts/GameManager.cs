using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; 
using System.Collections;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI 引用")]
    public Text deathTextUI; 
    public Text winTextUI; 
    
    [Header("收集系统")]
    public TextMeshProUGUI ballCountTextUI; 
    public int totalBallsRequired = 3; 
    private int _currentBalls = 0; 

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
        UpdateBallUI();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // 玩家吃到球时触发 (仅加分，不再自动开门)
    public void AddBall()
    {
        if (_isLevelEnded) return;
        _currentBalls++;       
        UpdateBallUI();        
    }

    // ==========================================
    // 【公开接口】：供插槽查询和扣除玩家手里的核心
    // ==========================================
    public int GetCurrentBallsCount()
    {
        return _currentBalls;
    }

    public bool TrySpendBall()
    {
        if (_currentBalls > 0)
        {
            _currentBalls--; // 扣除一个
            UpdateBallUI();  // 刷新UI
            return true;
        }
        return false;
    }

    private void UpdateBallUI()
    {
        if (ballCountTextUI != null)
        {
            ballCountTextUI.text = "Cores: " + _currentBalls + " / " + totalBallsRequired;
        }
    }

    public void PlayerDied()
    {
        if (_isLevelEnded) return; 
        _isLevelEnded = true;
        Time.timeScale = 0f;
        DisablePlayerControls(); 
        StartCoroutine(RestartLevelRoutine());
    }

    // ==========================================
    // 【最终通关】：由关卡最后的 ExitPortal 脚本调用
    // ==========================================
    public void TriggerWin()
    {
        if (_isLevelEnded) return;
        _isLevelEnded = true;
        StartCoroutine(LevelClearedRoutine());
    }

    private IEnumerator LevelClearedRoutine()
    {
        DisablePlayerControls();
        Time.timeScale = 0.1f;

        if (winTextUI != null)
        {
            winTextUI.gameObject.SetActive(true);
            winTextUI.text = "LEVEL CLEARED\nWARPING TO NEXT LEVEL...";
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
            var playerInput = player.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (playerInput != null) playerInput.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}