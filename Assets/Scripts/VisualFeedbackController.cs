using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class VisualFeedbackController : MonoBehaviour
{
    public static VisualFeedbackController Instance;

    [Header("后期处理配置")]
    public Volume globalVolume;

    [Header("跑酷粒子特效")]
    public ParticleSystem speedLines;
    public float particleFadeSpeed = 15f;

    [Header("🚀 1. 跳板过曝闪光特效 (Flash)")]
    public float maxFlashExposure = 2.5f; 
    [Tooltip("稍微调慢了白光消散的速度，让强光多刺眼一会")]
    public float flashDecaySpeed = 5f; 
    private float _currentExposure = 0f;

    [Header("💥 2. 镜头起跳震动 (Camera Shake)")]
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 0.2f;
    private bool _isShaking = false;

    [Header("🌌 3. 黑洞跃迁空间扭曲 (Warp)")]
    public float jumpPadDistortionPunch = -0.85f; 
    public float tunnelSustainedDistortion = -0.3f; 
    public float paniniDistance = 1f; 
    public float distortionSpeed = 10f;
    private float _currentDistortion = 0f;

    [Header("🎧 4. 深海听觉隔离 (Audio Low Pass)")]
    public float muffledFrequency = 1500f; 
    public float normalFrequency = 22000f; 
    public float audioTransitionSpeed = 10f;
    private AudioLowPassFilter _lowPassFilter;

    [Header("👁️ 5. 极速失焦黑视 (Tunnel Vision)")]
    [Tooltip("起跳瞬间屏幕四周变黑的厚度（0-1之间，建议 0.85）")]
    public float jumpPadVignettePunch = 0.85f;
    [Tooltip("深渊渐变柔和度（极其关键！设为1就是一圈圈向外深邃变黑，彻底消除贴纸感）")]
    public float jumpPadVignetteSmoothness = 1f;
    [Tooltip("黑视消散的速度（改小了！数字越小，在空中的黑视停留越久。建议 2-3）")]
    public float vignetteDecaySpeed = 2.5f;

    private float _currentVignettePunch = 0f;
    private float _currentVignetteSmoothness = 0.2f;
    private float _baseVignetteSmoothness = 0.2f; // 记录你原本游戏的暗角平滑度

    [Header("视觉参数：慢动作时 (停下)")]
    public float slowSaturation = -60f; 
    public float slowVignette = 0.45f;  

    [Header("视觉参数：正常速度时 (跑动)")]
    public float normalSaturation = 0f; 
    public float normalVignette = 0.15f; 

    [Header("视觉参数：死亡时")]
    public float deathSaturation = -100f; 
    public float deathVignette = 0.6f;    
    public float deathTransitionSpeed = 5f; 

    [Header("视觉参数：极速穿梭色差 (Chromatic)")]
    public float dashChromatic = 1f; 
    public float chromaticSpeed = 15f;  
    public float dashVisualDuration = 0.3f; 

    private ColorAdjustments _colorAdjustments;
    private Vignette _vignette;
    private ChromaticAberration _chromaticAberration;
    private LensDistortion _lensDistortion;
    private PaniniProjection _paniniProjection;

    private float _dashEffectTimer = 0f;
    
    private float _originalEmissionRate;
    private float _originalSimSpeed;
    private float _currentParticleIntensity = 0f;

    void Awake()
    {
        Instance = this; 
    }

    void Start()
    {
        if (globalVolume != null && globalVolume.profile != null)
        {
            globalVolume.profile.TryGet(out _colorAdjustments);
            globalVolume.profile.TryGet(out _vignette);
            globalVolume.profile.TryGet(out _chromaticAberration);
            globalVolume.profile.TryGet(out _lensDistortion);
            globalVolume.profile.TryGet(out _paniniProjection);

            // 记录下你原本在 Volume 面板里设置好的平滑度（0.2）
            if (_vignette != null)
            {
                _baseVignetteSmoothness = _vignette.smoothness.value;
                _currentVignetteSmoothness = _baseVignetteSmoothness;
            }
        }

        if (speedLines != null)
        {
            _originalEmissionRate = speedLines.emission.rateOverTimeMultiplier;
            _originalSimSpeed = speedLines.main.simulationSpeed;
        }

        if (Camera.main != null)
        {
            _lowPassFilter = Camera.main.GetComponent<AudioLowPassFilter>();
            if (_lowPassFilter == null) _lowPassFilter = Camera.main.gameObject.AddComponent<AudioLowPassFilter>();
            _lowPassFilter.cutoffFrequency = normalFrequency;
        }
    }

    void Update()
    {
        if (_colorAdjustments == null || _vignette == null) return;

        // --- 1. 爆发性特效的平滑消散 ---
        // 白光消散（比之前稍微慢了一点）
        _currentExposure = Mathf.Lerp(_currentExposure, 0f, Time.unscaledDeltaTime * flashDecaySpeed);
        _colorAdjustments.postExposure.value = _currentExposure;

        // 【核心修改】：黑圈使用专属的极慢消散速度！并且同时平滑还原厚度和渐变度
        _currentVignettePunch = Mathf.Lerp(_currentVignettePunch, 0f, Time.unscaledDeltaTime * vignetteDecaySpeed);
        _currentVignetteSmoothness = Mathf.Lerp(_currentVignetteSmoothness, _baseVignetteSmoothness, Time.unscaledDeltaTime * vignetteDecaySpeed);

        // --- 2. 管道状态计时器 ---
        if (DashController.Instance != null && DashController.Instance.isDashing)
        {
            _dashEffectTimer = dashVisualDuration; 
        }

        float targetIntensity = 0f;
        float targetDistortion = 0f;
        float targetPanini = 0f;
        float targetFrequency = normalFrequency;

        if (_dashEffectTimer > 0f)
        {
            _dashEffectTimer -= Time.unscaledDeltaTime;
            targetIntensity = 1f; 
            targetDistortion = tunnelSustainedDistortion; 
            targetPanini = paniniDistance;                
            targetFrequency = muffledFrequency;           
        }
        else
        {
            targetIntensity = 0f; 
            targetDistortion = 0f;               
            targetPanini = 0f;
            targetFrequency = normalFrequency;   
        }

        // --- 3. 空间折叠与色差过渡 ---
        if (_chromaticAberration != null)
            _chromaticAberration.intensity.value = Mathf.Lerp(_chromaticAberration.intensity.value, targetIntensity * dashChromatic, Time.unscaledDeltaTime * chromaticSpeed);
            
        _currentDistortion = Mathf.Lerp(_currentDistortion, targetDistortion, Time.unscaledDeltaTime * distortionSpeed);
        if (_lensDistortion != null)
            _lensDistortion.intensity.value = _currentDistortion;
            
        if (_paniniProjection != null)
            _paniniProjection.distance.value = Mathf.Lerp(_paniniProjection.distance.value, targetPanini, Time.unscaledDeltaTime * distortionSpeed);

        if (_lowPassFilter != null)
            _lowPassFilter.cutoffFrequency = Mathf.Lerp(_lowPassFilter.cutoffFrequency, targetFrequency, Time.unscaledDeltaTime * audioTransitionSpeed);

        // --- 4. 粒子衰减系统 ---
        if (speedLines != null)
        {
            _currentParticleIntensity = Mathf.Lerp(_currentParticleIntensity, targetIntensity, Time.unscaledDeltaTime * particleFadeSpeed);
            var emission = speedLines.emission;
            var main = speedLines.main;

            emission.rateOverTimeMultiplier = _originalEmissionRate * _currentParticleIntensity;
            main.simulationSpeed = Mathf.Max(_originalSimSpeed * 0.8f, _originalSimSpeed * _currentParticleIntensity);

            if (_currentParticleIntensity > 0.05f && !speedLines.isPlaying) speedLines.Play(); 
            else if (_currentParticleIntensity <= 0.05f && speedLines.isPlaying) speedLines.Stop(); 
        }

        // --- 5. 基础暗角与爆发暗角的完美融合 ---
        if (Time.timeScale == 0f)
        {
            _colorAdjustments.saturation.value = Mathf.Lerp(_colorAdjustments.saturation.value, deathSaturation, Time.unscaledDeltaTime * deathTransitionSpeed);
            _vignette.intensity.value = Mathf.Clamp01(Mathf.Lerp(_vignette.intensity.value, deathVignette, Time.unscaledDeltaTime * deathTransitionSpeed) + _currentVignettePunch);
            _vignette.smoothness.value = _currentVignetteSmoothness;
            return; 
        }

        float t = Mathf.Clamp01((Time.timeScale - 0.05f) / (1f - 0.05f));
        _colorAdjustments.saturation.value = Mathf.Lerp(slowSaturation, normalSaturation, t);
        
        float baseVignette = Mathf.Lerp(slowVignette, normalVignette, t);
        
        // 【核心修改】：应用厚度，同时应用极度丝滑的深渊渐变效果！
        _vignette.intensity.value = Mathf.Clamp01(baseVignette + _currentVignettePunch);
        _vignette.smoothness.value = _currentVignetteSmoothness;
    }

    public void TriggerTunnelEffect(float duration)
    {
        _dashEffectTimer = Mathf.Max(_dashEffectTimer, duration);
    }

    public void TriggerJumpPadFeedback()
    {
        _currentExposure = maxFlashExposure; 
        _currentDistortion = jumpPadDistortionPunch; 
        
        // 【新增大招】：起跳瞬间拉满黑视厚度和黑视平滑渐变！
        _currentVignettePunch = jumpPadVignettePunch;
        _currentVignetteSmoothness = jumpPadVignetteSmoothness;

        if (!_isShaking && Camera.main != null)
        {
            StartCoroutine(ShakeCameraRoutine());
        }
    }

    private IEnumerator ShakeCameraRoutine()
    {
        _isShaking = true;
        GameObject cameraRoot = GameObject.Find("PlayerCameraRoot");
        Transform targetTransform = cameraRoot != null ? cameraRoot.transform : Camera.main.transform;
        
        Vector3 originalPos = targetTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float currentMagnitude = Mathf.Lerp(shakeMagnitude, 0f, elapsed / shakeDuration);
            float x = Random.Range(-1f, 1f) * currentMagnitude;
            float y = Random.Range(-1f, 1f) * currentMagnitude;
            
            targetTransform.localPosition = originalPos + new Vector3(x, y, 0);
            elapsed += Time.unscaledDeltaTime; 
            yield return null;
        }

        targetTransform.localPosition = originalPos;
        _isShaking = false;
    }
}