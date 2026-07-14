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
    public float jumpPadVignettePunch = 0.85f;
    public float jumpPadVignetteSmoothness = 1f;
    public float vignetteDecaySpeed = 2.5f;

    private float _currentVignettePunch = 0f;
    private float _currentVignetteSmoothness = 0.2f;
    private float _baseVignetteSmoothness = 0.2f; 

    [Header("视觉参数：慢动作时 (超频爆发)")]
    public float slowSaturation = -100f; 
    public float slowExposure = -1.5f;   
    public float slowChromatic = 1f;     
    public float slowVignette = 0.5f;  

    [Header("视觉参数：正常速度时 (跑动)")]
    public float normalSaturation = 0f; 
    public float normalExposure = 0f;    
    public float normalChromatic = 0f;
    public float normalVignette = 0.15f; 

    [Header("视觉参数：死亡时 (被回溯接管)")]
    public float deathSaturation = -100f; 
    public float deathVignette = 0.6f;    
    public float deathTransitionSpeed = 5f; 

    [Header("视觉参数：极速穿梭色差 (Dash Chromatic)")]
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

    private bool _isRebooting = false; 

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

        _currentExposure = Mathf.Lerp(_currentExposure, 0f, Time.unscaledDeltaTime * flashDecaySpeed);
        _currentVignettePunch = Mathf.Lerp(_currentVignettePunch, 0f, Time.unscaledDeltaTime * vignetteDecaySpeed);
        _currentVignetteSmoothness = Mathf.Lerp(_currentVignetteSmoothness, _baseVignetteSmoothness, Time.unscaledDeltaTime * vignetteDecaySpeed);

        if (DashController.Instance != null && DashController.Instance.isDashing)
        {
            _dashEffectTimer = dashVisualDuration; 
        }

        // 🚨 核心阻断：如果正在回溯，锁死所有的常规视角更新！
        if (_isRebooting) return;

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

        _currentDistortion = Mathf.Lerp(_currentDistortion, targetDistortion, Time.unscaledDeltaTime * distortionSpeed);
        if (_lensDistortion != null)
            _lensDistortion.intensity.value = _currentDistortion;
            
        if (_paniniProjection != null)
            _paniniProjection.distance.value = Mathf.Lerp(_paniniProjection.distance.value, targetPanini, Time.unscaledDeltaTime * distortionSpeed);

        if (_lowPassFilter != null)
            _lowPassFilter.cutoffFrequency = Mathf.Lerp(_lowPassFilter.cutoffFrequency, targetFrequency, Time.unscaledDeltaTime * audioTransitionSpeed);

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

        if (Time.timeScale == 0f)
        {
            _colorAdjustments.saturation.value = Mathf.Lerp(_colorAdjustments.saturation.value, deathSaturation, Time.unscaledDeltaTime * deathTransitionSpeed);
            _vignette.intensity.value = Mathf.Clamp01(Mathf.Lerp(_vignette.intensity.value, deathVignette, Time.unscaledDeltaTime * deathTransitionSpeed) + _currentVignettePunch);
            _vignette.smoothness.value = _currentVignetteSmoothness;
            return; 
        }

        float t = Mathf.Clamp01((Time.timeScale - 0.05f) / (1f - 0.05f));
        
        float timeExposure = Mathf.Lerp(slowExposure, normalExposure, t);
        _colorAdjustments.postExposure.value = _currentExposure + timeExposure;
        _colorAdjustments.saturation.value = Mathf.Lerp(slowSaturation, normalSaturation, t);
        
        float baseVignette = Mathf.Lerp(slowVignette, normalVignette, t);
        _vignette.intensity.value = Mathf.Clamp01(baseVignette + _currentVignettePunch);
        _vignette.smoothness.value = _currentVignetteSmoothness;

        float timeChromatic = Mathf.Lerp(slowChromatic, normalChromatic, t);
        float finalChromaticTarget = (targetIntensity * dashChromatic) + timeChromatic;
        
        if (_chromaticAberration != null)
        {
            _chromaticAberration.intensity.value = Mathf.Lerp(_chromaticAberration.intensity.value, finalChromaticTarget, Time.unscaledDeltaTime * chromaticSpeed);
        }
    }

    public void TriggerTunnelEffect(float duration) { _dashEffectTimer = Mathf.Max(_dashEffectTimer, duration); }

    public void TriggerJumpPadFeedback()
    {
        _currentExposure = maxFlashExposure; 
        _currentDistortion = jumpPadDistortionPunch; 
        _currentVignettePunch = jumpPadVignettePunch;
        _currentVignetteSmoothness = jumpPadVignetteSmoothness;

        if (!_isShaking && Camera.main != null) StartCoroutine(ShakeCameraRoutine());
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

    // ==========================================
    // 🚨 阶段 3：高维白光 CRT 回溯 (Tracer Flashback)
    // ==========================================
    public void TriggerRebootVisuals(float duration)
    {
        if (!_isRebooting) StartCoroutine(RebootVisualRoutine(duration));
    }

    private IEnumerator RebootVisualRoutine(float duration)
    {
        _isRebooting = true; 

        if (_colorAdjustments != null)
        {
            _colorAdjustments.saturation.overrideState = true;
            _colorAdjustments.colorFilter.overrideState = true;
            _colorAdjustments.postExposure.overrideState = true;
            _colorAdjustments.contrast.overrideState = true; 
        }
        if (_chromaticAberration != null) _chromaticAberration.intensity.overrideState = true;
        if (_lensDistortion != null) _lensDistortion.intensity.overrideState = true;
        if (_vignette != null) _vignette.intensity.overrideState = true;

        if (_lowPassFilter != null) _lowPassFilter.cutoffFrequency = 400f; // 听觉瞬间坠入深海

        // 💥 阶段 A：起步爆发 (0.2秒) —— 瞬间闪白与空间向后塌陷
        float flashTime = 0.2f;
        float elapsed = 0f;
        
        while (elapsed < flashTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / flashTime;

            if (_colorAdjustments != null)
            {
                _colorAdjustments.saturation.value = Mathf.Lerp(normalSaturation, -100f, t);
                _colorAdjustments.postExposure.value = Mathf.Lerp(normalExposure, 3.5f, t); // 极其刺眼的高维白光
                _colorAdjustments.contrast.value = Mathf.Lerp(0f, 40f, t);
            }
            if (_lensDistortion != null) _lensDistortion.intensity.value = Mathf.Lerp(0f, -0.6f, t); // 空间后置拉扯
            if (_chromaticAberration != null) _chromaticAberration.intensity.value = Mathf.Lerp(0f, 1f, t);
            if (_vignette != null) _vignette.intensity.value = Mathf.Lerp(normalVignette, 0.5f, t);

            yield return null;
        }

        // 🚀 阶段 B：高维白光滑行 (匹配物理回溯的耗时，预留最后0.3秒恢复)
        float sustainTime = Mathf.Max(0f, duration - flashTime - 0.3f);
        elapsed = 0f;

        while (elapsed < sustainTime)
        {
            elapsed += Time.unscaledDeltaTime;
            
            // 维持高维白光，伴随极其微弱的电压频闪和强烈的色散
            if (_colorAdjustments != null)
                _colorAdjustments.postExposure.value = Random.Range(3.0f, 3.8f);
            
            if (_chromaticAberration != null) 
                _chromaticAberration.intensity.value = Random.Range(0.8f, 1.2f);
            
            if (_lensDistortion != null)
                _lensDistortion.intensity.value = Random.Range(-0.65f, -0.55f);

            yield return null;
        }

        // 🌊 阶段 C：现实重连 (0.3秒) —— 白光如潮水般褪去，色彩瞬间涌入
        float recoverTime = 0.3f;
        elapsed = 0f;

        while (elapsed < recoverTime)
        {
            elapsed += Time.unscaledDeltaTime;
            // 采用平滑曲线(SmoothStep)，让落地感更扎实
            float t = elapsed / recoverTime;
            float smoothT = t * t * (3f - 2f * t);

            if (_colorAdjustments != null)
            {
                _colorAdjustments.postExposure.value = Mathf.Lerp(3.5f, normalExposure, smoothT);
                _colorAdjustments.saturation.value = Mathf.Lerp(-100f, normalSaturation, smoothT);
                _colorAdjustments.contrast.value = Mathf.Lerp(40f, 0f, smoothT);
            }
            if (_lensDistortion != null) _lensDistortion.intensity.value = Mathf.Lerp(-0.6f, 0f, smoothT);
            if (_chromaticAberration != null) _chromaticAberration.intensity.value = Mathf.Lerp(1f, 0f, smoothT);
            if (_vignette != null) _vignette.intensity.value = Mathf.Lerp(0.5f, normalVignette, smoothT);
            if (_lowPassFilter != null) _lowPassFilter.cutoffFrequency = Mathf.Lerp(400f, normalFrequency, smoothT);

            yield return null;
        }

        // 兜底重置
        if (_lowPassFilter != null) _lowPassFilter.cutoffFrequency = normalFrequency;
        if (_colorAdjustments != null) 
        {
            _colorAdjustments.colorFilter.value = Color.white;
            _colorAdjustments.postExposure.value = normalExposure;
            _colorAdjustments.contrast.value = 0f;
            _colorAdjustments.saturation.value = normalSaturation;
        }
        if (_chromaticAberration != null) _chromaticAberration.intensity.value = 0f;
        if (_lensDistortion != null) _lensDistortion.intensity.value = 0f;
        
        _isRebooting = false; 
    }
}