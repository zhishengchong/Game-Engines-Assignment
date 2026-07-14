using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI 引用 (状态)")]
    public Text deathTextUI;
    public Text winTextUI;

    [Header("UI 引用 (竞速与收集)")]
    public TextMeshProUGUI ballCountTextUI;
    public TextMeshProUGUI timerTextUI;
    public TextMeshProUGUI scoreTextUI;
    public TextMeshProUGUI comboTextUI;

    [Header("收集系统")]
    public int totalBallsRequired = 3;
    private int _currentBalls = 0;

    [Header("📈 竞速计分引擎")]
    public float startingScore = 0f;
    public float parTime = 45f;
    public float bonusScorePerSecond = 2000f;

    [Header("🔥 连击评价引擎 (Style Combo)")]
    public int currentComboTier = 0;
    public int maxComboTier = 5;
    public float comboDecayTime = 3.5f;
    private float _currentComboTimer = 0f;

    public float[] comboMultipliers = { 1.0f, 1.2f, 1.5f, 2.0f, 2.5f, 3.0f };

    [Header("🧃 减时蓄水池 (Stack Buffer)")]
    public TextMeshProUGUI stackBufferTextUI;
    public float rollSpeed = 10f;
    public float stackLingerTime = 1.2f;

    [Header("🧃 弹簧物理参数 (Spring Math)")]
    public float punchScaleForce = 0.4f;
    public float springStiffness = 150f;
    public float springDamping = 12f;

    private float _currentStackScale = 1f;
    private float _stackScaleVelocity = 0f;

    private float _currentStackLingerTimer = 0f;
    private float _pendingTimeReduction = 0f;
    private float _displayPendingTime = 0f;

    private RectTransform _timerRect;
    private Vector2 _origStackPos;
    private bool _isCashInFlying = false;

    private Coroutine _timerFlashCoroutine;
    private Coroutine _scoreHighlightCoroutine;

    [Header("💥 视觉特效与颜色 (VFX)")]
    public GameObject floatingTextPrefab;

    public Color scoreColor = new Color(1f, 0.8f, 0.1f);
    public Color defaultScoreColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    public Color defaultTimerColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    public Color timeColor = new Color(0.1f, 0.9f, 0.9f);
    public Color penaltyColor = new Color(0.9f, 0.1f, 0.1f);

    [Header("🎧 音效引擎：动态连杀系统")]
    public AudioClip rewindSound;
    public float comboWindow = 2.0f;
    public float basePitch = 1.0f;
    public float pitchStep = 0.15f;
    public float maxPitch = 2.0f;

    private AudioSource _audioSource;
    private float _lastKillTime = -100f;
    private int _comboCount = 0;

    private float _elapsedTime = 0f;
    private float _currentScore = 0f;
    private bool _isTiming = false;
    private bool _isLevelEnded = false;

    private GameObject _player;

    void Awake()
    {
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
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        _player = GameObject.FindGameObjectWithTag("Player");

        if (deathTextUI != null)
        {
            deathTextUI.gameObject.SetActive(false);
        }

        if (winTextUI != null)
        {
            winTextUI.gameObject.SetActive(false);
        }

        if (comboTextUI != null)
        {
            comboTextUI.text = "";
        }

        if (timerTextUI != null)
        {
            _timerRect = timerTextUI.GetComponent<RectTransform>();
            timerTextUI.color = defaultTimerColor;
        }

        if (scoreTextUI != null)
        {
            scoreTextUI.color = defaultScoreColor;
        }

        if (stackBufferTextUI != null)
        {
            _origStackPos = stackBufferTextUI.rectTransform.anchoredPosition;
            stackBufferTextUI.gameObject.SetActive(false);
        }

        UpdateBallUI();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        StartTimer();
    }

    void Update()
    {
        if (_isTiming && !_isLevelEnded)
        {
            _elapsedTime += Time.unscaledDeltaTime;

            if (currentComboTier > 0)
            {
                _currentComboTimer -= Time.unscaledDeltaTime;

                if (_currentComboTimer <= 0f)
                {
                    DowngradeCombo();
                }
            }

            if (_pendingTimeReduction > 0f && !_isCashInFlying)
            {
                _currentStackLingerTimer -= Time.unscaledDeltaTime;

                if (_currentStackLingerTimer <= 0f)
                {
                    CashInPendingPool();
                }
            }

            if (stackBufferTextUI != null && stackBufferTextUI.gameObject.activeSelf && !_isCashInFlying)
            {
                float targetScale = 1f;

                _stackScaleVelocity += (targetScale - _currentStackScale) * springStiffness * Time.unscaledDeltaTime;
                _stackScaleVelocity = Mathf.Lerp(_stackScaleVelocity, 0f, springDamping * Time.unscaledDeltaTime);
                _currentStackScale += _stackScaleVelocity * Time.unscaledDeltaTime;

                Vector3 shakeOffset = Vector3.zero;

                if (_pendingTimeReduction > 3.0f)
                {
                    float intensity = Mathf.Clamp((_pendingTimeReduction - 3f) * 2f, 0f, 8f);
                    shakeOffset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * intensity;
                }

                stackBufferTextUI.rectTransform.localScale = Vector3.one * Mathf.Max(0.1f, _currentStackScale);
                stackBufferTextUI.rectTransform.anchoredPosition = _origStackPos + (Vector2)shakeOffset;

                if (Mathf.Abs(_displayPendingTime - _pendingTimeReduction) > 0.01f)
                {
                    _displayPendingTime = Mathf.Lerp(_displayPendingTime, _pendingTimeReduction, rollSpeed * Time.unscaledDeltaTime);

                    string hexTime = ColorUtility.ToHtmlStringRGB(timeColor);
                    stackBufferTextUI.text = $"<color=#{hexTime}>-{_displayPendingTime:F1}s</color>";
                }
            }

            UpdateTimerAndScoreUI();
        }
    }

    public void StartTimer()
    {
        _isTiming = true;
        _elapsedTime = 0f;
        _currentScore = startingScore;
    }

    public void StopTimer()
    {
        _isTiming = false;
    }

    public void AddBonusScore(float scoreBonus, float timeReduction = 0f, string enemyName = "UNKNOWN", Vector3 killPos = default)
    {
        if (!_isTiming || _isLevelEnded) return;

        float finalScore = scoreBonus;
        float finalTimeRed = timeReduction;

        if (scoreBonus > 0f || timeReduction > 0f)
        {
            float multiplier = comboMultipliers[Mathf.Clamp(currentComboTier, 0, comboMultipliers.Length - 1)];

            finalScore *= multiplier;
            finalTimeRed *= multiplier;

            UpgradeCombo();
        }

        if (floatingTextPrefab != null && killPos != default)
        {
            string combinedText = "";

            if (finalScore != 0f)
            {
                string prefix = finalScore > 0f ? "+" : "";
                string hexColor = ColorUtility.ToHtmlStringRGB(finalScore > 0f ? scoreColor : penaltyColor);
                combinedText += $"<color=#{hexColor}>{prefix}{finalScore:F0} PTS</color>";
            }

            if (finalTimeRed != 0f)
            {
                if (!string.IsNullOrEmpty(combinedText))
                {
                    combinedText += "\n";
                }

                string prefix = finalTimeRed > 0f ? "-" : "+";
                string hexColor = ColorUtility.ToHtmlStringRGB(finalTimeRed > 0f ? timeColor : penaltyColor);
                combinedText += $"<color=#{hexColor}>{prefix}{Mathf.Abs(finalTimeRed):F1}s</color>";
            }

            if (!string.IsNullOrEmpty(combinedText))
            {
                Vector3 spawnPos = killPos + Vector3.up * 1.5f;
                GameObject textObj = Instantiate(floatingTextPrefab, spawnPos, Quaternion.identity);

                FloatingText floatingText = textObj.GetComponent<FloatingText>();
                if (floatingText != null)
                {
                    floatingText.Setup(combinedText, Color.white);
                }
            }
        }

        if (TerminalLogManager.Instance != null)
        {
            if (finalScore > 0f)
            {
                TerminalLogManager.Instance.AddLog($"+{finalScore:F0}", "ELIMINATED", $"■ {enemyName}");
            }
            else if (finalScore < 0f)
            {
                TerminalLogManager.Instance.AddLog($"{finalScore:F0}", "SYS_ERROR", $"■ {enemyName}");
            }
        }

        if (finalScore > 0f)
        {
            _currentScore += finalScore;

            if (_scoreHighlightCoroutine != null)
            {
                StopCoroutine(_scoreHighlightCoroutine);
            }

            _scoreHighlightCoroutine = StartCoroutine(ScoreHighlightRoutine());
        }
        else if (finalScore < 0f)
        {
            _currentScore += finalScore;
        }

        if (finalTimeRed > 0f)
        {
            _pendingTimeReduction += finalTimeRed;
            _currentStackLingerTimer = stackLingerTime;

            if (stackBufferTextUI != null)
            {
                stackBufferTextUI.gameObject.SetActive(true);

                if (_isCashInFlying)
                {
                    _isCashInFlying = false;
                    stackBufferTextUI.rectTransform.anchoredPosition = _origStackPos;
                }

                _currentStackScale += punchScaleForce;
                _stackScaleVelocity += 2.5f;
            }
        }
        else if (finalTimeRed < 0f)
        {
            _elapsedTime = Mathf.Max(0f, _elapsedTime - finalTimeRed);
            TriggerTimerCalmFlash(true);
        }

        if (finalTimeRed > 0f && rewindSound != null && _audioSource != null)
        {
            if (Time.time - _lastKillTime <= comboWindow)
            {
                _comboCount++;
            }
            else
            {
                _comboCount = 0;
            }

            _lastKillTime = Time.time;

            float calculatedPitch = basePitch + (_comboCount * pitchStep);
            calculatedPitch = Mathf.Min(calculatedPitch, maxPitch);

            _audioSource.pitch = calculatedPitch + Random.Range(-0.05f, 0.05f);
            _audioSource.volume = Random.Range(0.85f, 1.0f);
            _audioSource.PlayOneShot(rewindSound);
        }

        UpdateTimerAndScoreUI();
    }

    private void CashInPendingPool()
    {
        if (_pendingTimeReduction > 0f)
        {
            float timeToCash = _pendingTimeReduction;
            _pendingTimeReduction = 0f;

            if (stackBufferTextUI != null && stackBufferTextUI.gameObject.activeSelf)
            {
                StartCoroutine(FlyAndCashInRoutine(timeToCash));
            }
            else
            {
                ExecuteCashIn(timeToCash);
            }
        }
    }

    private IEnumerator FlyAndCashInRoutine(float time)
    {
        if (stackBufferTextUI == null || _timerRect == null)
        {
            ExecuteCashIn(time);
            yield break;
        }

        _isCashInFlying = true;

        float duration = 0.25f;
        float elapsed = 0f;

        Vector3 startPos = stackBufferTextUI.rectTransform.anchoredPosition;
        Vector3 targetPos = _timerRect.anchoredPosition;

        Vector3 startScale = Vector3.one * _currentStackScale;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = elapsed / duration;
            float moveT = t * t;

            stackBufferTextUI.rectTransform.anchoredPosition = Vector3.Lerp(startPos, targetPos, moveT);
            stackBufferTextUI.rectTransform.localScale = Vector3.Lerp(startScale, Vector3.zero, moveT);

            yield return null;
        }

        stackBufferTextUI.gameObject.SetActive(false);
        stackBufferTextUI.rectTransform.anchoredPosition = _origStackPos;
        stackBufferTextUI.rectTransform.localScale = Vector3.one;

        _displayPendingTime = 0f;
        _currentStackScale = 1f;
        _stackScaleVelocity = 0f;
        _isCashInFlying = false;

        ExecuteCashIn(time);
    }

    private void ExecuteCashIn(float time)
    {
        _elapsedTime = Mathf.Max(0f, _elapsedTime - time);

        Debug.Log($"[系统通告] 独立清算！秒表扣除 {time:F1}秒！");

        TriggerTimerCalmFlash(false);
        UpdateTimerAndScoreUI();
    }

    private void UpgradeCombo()
    {
        if (currentComboTier < maxComboTier)
        {
            currentComboTier++;
        }

        _currentComboTimer = comboDecayTime;
    }

    private void DowngradeCombo()
    {
        currentComboTier--;

        if (currentComboTier > 0)
        {
            _currentComboTimer = comboDecayTime;
        }
        else
        {
            _currentComboTimer = 0f;
        }
    }

    public void BreakCombo()
    {
        _comboCount = 0;
        currentComboTier = 0;
        _currentComboTimer = 0f;

        CashInPendingPool();
    }

    private IEnumerator ScoreHighlightRoutine()
    {
        if (scoreTextUI == null)
        {
            yield break;
        }

        float elapsed = 0f;
        float duration = 0.35f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            float scale = 1f + 0.5f * Mathf.Sin(t * Mathf.PI) * Mathf.Exp(-t * 5f);

            scoreTextUI.rectTransform.localScale = Vector3.one * scale;
            scoreTextUI.color = Color.Lerp(scoreColor, defaultScoreColor, t);

            yield return null;
        }

        scoreTextUI.rectTransform.localScale = Vector3.one;
        scoreTextUI.color = defaultScoreColor;
    }

    private void TriggerTimerCalmFlash(bool isPenalty)
    {
        if (timerTextUI == null) return;

        if (_timerFlashCoroutine != null)
        {
            StopCoroutine(_timerFlashCoroutine);
        }

        _timerFlashCoroutine = StartCoroutine(TimerFlashRoutine(isPenalty));
    }

    private IEnumerator TimerFlashRoutine(bool isPenalty)
    {
        if (timerTextUI == null)
        {
            yield break;
        }

        float elapsed = 0f;
        float duration = 0.45f;

        Color targetColor = isPenalty ? penaltyColor : timeColor;

        timerTextUI.color = targetColor;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            timerTextUI.color = Color.Lerp(targetColor, defaultTimerColor, elapsed / duration);

            yield return null;
        }

        timerTextUI.color = defaultTimerColor;
    }

    private void UpdateTimerAndScoreUI()
    {
        if (timerTextUI != null)
        {
            int minutes = Mathf.FloorToInt(_elapsedTime / 60F);
            int seconds = Mathf.FloorToInt(_elapsedTime - minutes * 60);
            int milliseconds = Mathf.FloorToInt((_elapsedTime - minutes * 60 - seconds) * 100);

            timerTextUI.text = string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
        }

        if (scoreTextUI != null)
        {
            scoreTextUI.text = Mathf.FloorToInt(_currentScore).ToString();
        }

        if (comboTextUI != null)
        {
            if (currentComboTier == 0)
            {
                comboTextUI.text = "";
            }
            else
            {
                string[] tierNames = { "NONE", "C", "B", "A", "S", "SS" };

                string currentName = tierNames[Mathf.Clamp(currentComboTier, 0, tierNames.Length - 1)];
                float currentMultiplier = comboMultipliers[Mathf.Clamp(currentComboTier, 0, comboMultipliers.Length - 1)];

                comboTextUI.text = $"STYLE: {currentName} (x{currentMultiplier:F1})";

                if (currentComboTier >= 4)
                {
                    comboTextUI.color = new Color(0.9f, 0.1f, 0.1f);
                }
                else
                {
                    comboTextUI.color = Color.white;
                }
            }
        }
    }

    public void AddBall()
    {
        if (_isLevelEnded) return;

        _currentBalls++;
        UpdateBallUI();
    }

    public int GetCurrentBallsCount()
    {
        return _currentBalls;
    }

    public bool TrySpendBall()
    {
        if (_currentBalls > 0)
        {
            _currentBalls--;
            UpdateBallUI();
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

    // =========================================================
    // DeadZone / 敌方子弹调用这里
    // 不重开场景，不传送出生点，而是触发 PlayerHealth 的回溯系统
    // =========================================================
    public void PlayerDied()
    {
        if (_isLevelEnded) return;

        if (_player == null)
        {
            _player = GameObject.FindGameObjectWithTag("Player");
        }

        if (_player == null)
        {
            Debug.LogWarning("⚠️ [GameManager] PlayerDied 被调用，但找不到 Player。");
            return;
        }

        PlayerHealth health = _player.GetComponent<PlayerHealth>();

        if (health == null)
        {
            Debug.LogWarning("⚠️ [GameManager] PlayerDied 被调用，但 Player 身上没有 PlayerHealth。");
            return;
        }

        // 造成一次必死伤害，让 PlayerHealth 自己执行：
        // 扣分 / 加时间惩罚 / 断连击 / 视觉回溯 / 位置回溯
        float lethalDamage = Mathf.Max(health.currentHealth, health.maxHealth) + 9999f;
        health.TakeDamage(lethalDamage);
    }

    public void TriggerWin()
    {
        if (_isLevelEnded) return;

        _isLevelEnded = true;

        StopTimer();
        CashInPendingPool();

        float timeSaved = Mathf.Max(0f, parTime - _elapsedTime);
        float timeBonusScore = timeSaved * bonusScorePerSecond;

        _currentScore += timeBonusScore;

        CheckAndSaveEasterEgg();

        StartCoroutine(LevelClearedRoutine(timeSaved, timeBonusScore));
    }

    private void CheckAndSaveEasterEgg()
    {
        if (RunDataManager.Instance == null) return;

        // 旧版逻辑依赖 WeaponInteraction 判断是否携带 GlitchCore。
        // 现在已经废弃捡枪/丢枪系统，所以这里先关闭旧彩蛋保存。
        RunDataManager.Instance.hasGlitchCore = false;
    }

    private IEnumerator LevelClearedRoutine(float timeSaved, float timeBonus)
    {
        SetPlayerControls(false, true);

        Time.timeScale = 0.1f;

        if (winTextUI != null)
        {
            winTextUI.gameObject.SetActive(true);

            winTextUI.text =
                $"LEVEL CLEARED\n\n" +
                $"FINAL TIME: {_elapsedTime:F2}s\n" +
                $"TIME SAVED: {timeSaved:F2}s\n" +
                $"TIME BONUS: +{Mathf.FloorToInt(timeBonus)}\n\n" +
                $"TOTAL SCORE: {Mathf.FloorToInt(_currentScore)}\n\n" +
                $"WARPING...";
        }

        yield return new WaitForSecondsRealtime(4f);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

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

    private void SetPlayerControls(bool enabled, bool unlockCursorWhenDisabled)
    {
        if (_player == null)
        {
            _player = GameObject.FindGameObjectWithTag("Player");
        }

        if (_player != null)
        {
            var fpc = _player.GetComponent<StarterAssets.FirstPersonController>();
            if (fpc != null)
            {
                fpc.enabled = enabled;
            }

            var alwaysWeapon = _player.GetComponent<AlwaysEquippedWeaponController>();
            if (alwaysWeapon != null)
            {
                alwaysWeapon.enabled = enabled;
            }

            var dash = _player.GetComponent<DashController>();
            if (dash != null)
            {
                dash.enabled = enabled;
            }

            var playerInput = _player.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (playerInput != null)
            {
                playerInput.enabled = enabled;
            }

            var input = _player.GetComponent<StarterAssets.StarterAssetsInputs>();
            if (input != null && !enabled)
            {
                input.move = Vector2.zero;
                input.look = Vector2.zero;
                input.jump = false;
                input.sprint = false;
            }
        }

        if (enabled)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else if (unlockCursorWhenDisabled)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}