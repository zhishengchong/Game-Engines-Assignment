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
    public Color cooldownColor = new Color(0.3f, 0.3f, 0.3f, 1f); 
    public Color flashColor = new Color(1f, 0.8f, 0f, 1f); 
    public float punchScale = 1.3f; 

    private Color _readyColor; 
    private Image _skillIconImage;
    private Vector3 _originalScale;
    
    private bool _isAnimating = false;
    private bool _wasOnCooldown = false;

    // 【新增】：用于缓存上一次显示的数字，避免每帧产生字符串垃圾
    private float _lastDisplayedCD = -1f;

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

            if (!_isAnimating && _skillIconImage != null) 
            {
                _skillIconImage.color = cooldownColor; 
            }
            
            if (cooldownMask != null) cooldownMask.fillAmount = currentCD / maxCD;
            
            if (cooldownText != null)
            {
                cooldownText.gameObject.SetActive(true);
                
                // ==========================================
                // 【核心优化】：字符串节流阀
                // 将浮点数向下取整到一位小数（例如 12.34 取为 12.3）
                // ==========================================
                float roundedCD = Mathf.Ceil(currentCD * 10f) / 10f; 
                
                // 只有当这个小数发生变化时（每0.1秒），才执行耗费性能的 ToString
                if (roundedCD != _lastDisplayedCD)
                {
                    cooldownText.text = roundedCD.ToString("F1");
                    _lastDisplayedCD = roundedCD;
                }
            }
        }
        else
        {
            if (_wasOnCooldown)
            {
                _wasOnCooldown = false;
                _lastDisplayedCD = -1f; // 重置缓存
                TriggerReadyFeedback(); 
            }

            if (!_isAnimating && _skillIconImage != null) 
            {
                _skillIconImage.color = _readyColor; 
            }

            if (cooldownMask != null) cooldownMask.fillAmount = 0;
            if (cooldownText != null) cooldownText.gameObject.SetActive(false);
        }
    }

    public void TriggerKillFeedback()
    {
        if (skillIconTransform == null) return;
        StopAllCoroutines();
        StartCoroutine(PunchEffectCoroutine(flashColor, punchScale));
    }

    public void TriggerReadyFeedback()
    {
        if (skillIconTransform == null) return;
        StopAllCoroutines();
        StartCoroutine(PunchEffectCoroutine(new Color(1f, 1f, 1f, 1f), 1.15f));
    }

    private IEnumerator PunchEffectCoroutine(Color targetColor, float targetScale)
    {
        _isAnimating = true;

        skillIconTransform.localScale = _originalScale * targetScale;
        if (_skillIconImage != null) _skillIconImage.color = targetColor;

        float elapsed = 0f;
        float duration = 0.2f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; 
            float t = elapsed / duration;
            
            skillIconTransform.localScale = Vector3.Lerp(_originalScale * targetScale, _originalScale, t);
            
            Color endColor = (JumpPadSkill.Instance.currentCooldown > 0) ? cooldownColor : _readyColor;
            if (_skillIconImage != null) _skillIconImage.color = Color.Lerp(targetColor, endColor, t);
            
            yield return null;
        }

        skillIconTransform.localScale = _originalScale;
        if (_skillIconImage != null) _skillIconImage.color = (JumpPadSkill.Instance.currentCooldown > 0) ? cooldownColor : _readyColor;
        
        _isAnimating = false;
    }
}
