using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VisualFeedbackController : MonoBehaviour
{
    public static VisualFeedbackController Instance;

    [Header("后期处理配置")]
    public Volume globalVolume;

    [Header("跑酷粒子特效")]
    [Tooltip("把你绑定在摄像机上的 SpeedLines 粒子系统拖到这里！")]
    public ParticleSystem speedLines;
    [Tooltip("粒子特效消失的平滑速度 (数值越大，出管道后消散得越快！建议调到 15-20)")]
    public float particleFadeSpeed = 15f; // 【修改】：默认值加大，让散场更干脆

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

    [Header("视觉参数：极速穿梭色差 (Chromatic Aberration)")]
    public float dashChromatic = 1f; 
    public float chromaticSpeed = 15f;  
    public float dashVisualDuration = 0.3f; 

    private ColorAdjustments _colorAdjustments;
    private Vignette _vignette;
    private ChromaticAberration _chromaticAberration;

    private float _dashEffectTimer = 0f;

    // --- 粒子平滑控制专用变量 ---
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
        }

        if (speedLines != null)
        {
            _originalEmissionRate = speedLines.emission.rateOverTimeMultiplier;
            _originalSimSpeed = speedLines.main.simulationSpeed;
        }
    }

    void Update()
    {
        if (_colorAdjustments == null || _vignette == null) return;

        if (DashController.Instance != null && DashController.Instance.isDashing)
        {
            _dashEffectTimer = dashVisualDuration; 
        }

        float targetIntensity = 0f;

        if (_chromaticAberration != null)
        {
            if (_dashEffectTimer > 0f)
            {
                _dashEffectTimer -= Time.unscaledDeltaTime;
                _chromaticAberration.intensity.value = Mathf.Lerp(_chromaticAberration.intensity.value, dashChromatic, Time.unscaledDeltaTime * chromaticSpeed);
                targetIntensity = 1f; 
            }
            else
            {
                _chromaticAberration.intensity.value = Mathf.Lerp(_chromaticAberration.intensity.value, 0f, Time.unscaledDeltaTime * (chromaticSpeed * 0.5f));
                targetIntensity = 0f; 
            }
        }

        if (speedLines != null)
        {
            _currentParticleIntensity = Mathf.Lerp(_currentParticleIntensity, targetIntensity, Time.unscaledDeltaTime * particleFadeSpeed);

            var emission = speedLines.emission;
            var main = speedLines.main;

            // 1. 动态掐断发射器，出管道瞬间不发新粒子
            emission.rateOverTimeMultiplier = _originalEmissionRate * _currentParticleIntensity;
            
            // ==========================================
            // 【核心修复】：千万不要把速度降得太低！
            // 保证最低有 0.8 倍的速度，让剩下的粒子快速冲出屏幕，而不是卡在脸上
            // ==========================================
            main.simulationSpeed = Mathf.Max(_originalSimSpeed * 0.8f, _originalSimSpeed * _currentParticleIntensity);

            if (_currentParticleIntensity > 0.05f && !speedLines.isPlaying)
            {
                speedLines.Play(); 
            }
            else if (_currentParticleIntensity <= 0.05f && speedLines.isPlaying)
            {
                speedLines.Stop(); 
            }
        }

        if (Time.timeScale == 0f)
        {
            _colorAdjustments.saturation.value = Mathf.Lerp(_colorAdjustments.saturation.value, deathSaturation, Time.unscaledDeltaTime * deathTransitionSpeed);
            _vignette.intensity.value = Mathf.Lerp(_vignette.intensity.value, deathVignette, Time.unscaledDeltaTime * deathTransitionSpeed);
            return; 
        }

        float t = (Time.timeScale - 0.05f) / (1f - 0.05f);
        t = Mathf.Clamp01(t);

        _colorAdjustments.saturation.value = Mathf.Lerp(slowSaturation, normalSaturation, t);
        _vignette.intensity.value = Mathf.Lerp(slowVignette, normalVignette, t);
    }

    public void TriggerTunnelEffect(float duration)
    {
        _dashEffectTimer = Mathf.Max(_dashEffectTimer, duration);
    }
}