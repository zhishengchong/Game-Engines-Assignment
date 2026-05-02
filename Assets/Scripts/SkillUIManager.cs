using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SkillUIManager : MonoBehaviour
{
    public static SkillUIManager Instance;

    [Header("UI 绑定")]
    public Image cooldownMask; 
    public TextMeshProUGUI cooldownText; 
    public RectTransform skillIconTransform; 

    [Header("视觉反馈设置")]
    [Tooltip("技能冷却时的颜色 (深灰色变暗)")]
    public Color cooldownColor = new Color(0.3f, 0.3f, 0.3f, 1f); 
    [Tooltip("击杀减CD时的金色闪光")]
    public Color flashColor = new Color(1f, 0.8f, 0f, 1f); 
    [Tooltip("击杀时的放大倍数")]
    public float punchScale = 1.3f; 

    private Color _readyColor; // 技能就绪时的高亮原色（自动获取）
    private Image _skillIconImage;
    private Vector3 _originalScale;
    
    // 状态标记
    private bool _isAnimating = false;
    private bool _wasOnCooldown = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (skillIconTransform != null)
        {
            _originalScale = skillIconTransform.localScale;
            _skillIconImage = skillIconTransform.GetComponent<Image>();
            if (_skillIconImage != null) 
            {
                // 自动记录你原本设定的正常亮色
                _readyColor = _skillIconImage.color; 
            }
        }
    }

    void Update()
    {
        if (JumpPadSkill.Instance == null) return;

        float currentCD = JumpPadSkill.Instance.currentCooldown;
        float maxCD = JumpPadSkill.Instance.maxCooldown;

        if (currentCD > 0)
        {
            _wasOnCooldown = true;

            // ==========================================
            // 冷却中：图标变暗 (只有不在播放击杀特效时才强行变暗)
            // ==========================================
            if (!_isAnimating && _skillIconImage != null) 
            {
                _skillIconImage.color = cooldownColor; 
            }
            
            if (cooldownMask != null) cooldownMask.fillAmount = currentCD / maxCD;
            if (cooldownText != null)
            {
                cooldownText.gameObject.SetActive(true);
                cooldownText.text = currentCD.ToString("F1");
            }
        }
        else
        {
            // ==========================================
            // 刚好冷却完毕的那一瞬间！
            // ==========================================
            if (_wasOnCooldown)
            {
                _wasOnCooldown = false;
                TriggerReadyFeedback(); // 触发“叮！就绪”的弹跳特效
            }

            // 日常就绪状态：保持高亮
            if (!_isAnimating && _skillIconImage != null) 
            {
                _skillIconImage.color = _readyColor; 
            }

            if (cooldownMask != null) cooldownMask.fillAmount = 0;
            if (cooldownText != null) cooldownText.gameObject.SetActive(false);
        }
    }

    // 由 JumpPadSkill 击杀减CD时调用
    public void TriggerKillFeedback()
    {
        if (skillIconTransform == null) return;
        StopAllCoroutines();
        StartCoroutine(PunchEffectCoroutine(flashColor, punchScale));
    }

    // 技能彻底冷却完毕时的专属反馈
    public void TriggerReadyFeedback()
    {
        if (skillIconTransform == null) return;
        StopAllCoroutines();
        // 冷却好时闪一下纯白光，稍微放大一点提醒玩家
        StartCoroutine(PunchEffectCoroutine(new Color(1f, 1f, 1f, 1f), 1.15f));
    }

    // 通用动画播放器
    private IEnumerator PunchEffectCoroutine(Color targetColor, float targetScale)
    {
        _isAnimating = true;

        // 瞬间放大并变色
        skillIconTransform.localScale = _originalScale * targetScale;
        if (_skillIconImage != null) _skillIconImage.color = targetColor;

        float elapsed = 0f;
        float duration = 0.2f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; 
            float t = elapsed / duration;
            
            skillIconTransform.localScale = Vector3.Lerp(_originalScale * targetScale, _originalScale, t);
            
            // 动画结束时，根据当前是否在冷却，极其平滑地退回到 灰色(冷却中) 或 亮色(就绪)
            Color endColor = (JumpPadSkill.Instance.currentCooldown > 0) ? cooldownColor : _readyColor;
            if (_skillIconImage != null) _skillIconImage.color = Color.Lerp(targetColor, endColor, t);
            
            yield return null;
        }

        // 确保动画结束后参数严丝合缝归位
        skillIconTransform.localScale = _originalScale;
        if (_skillIconImage != null) _skillIconImage.color = (JumpPadSkill.Instance.currentCooldown > 0) ? cooldownColor : _readyColor;
        
        _isAnimating = false;
    }
}