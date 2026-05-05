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
    }

    void Update()
    {
        if (_colorAdjustments == null || _vignette == null) return;

        if (DashController.Instance != null && DashController.Instance.isDashing)
        {
            _dashEffectTimer = dashVisualDuration; 
        }

        if (_chromaticAberration != null)
        {
            if (_dashEffectTimer > 0f)
            {
                _dashEffectTimer -= Time.unscaledDeltaTime;
                _chromaticAberration.intensity.value = Mathf.Lerp(_chromaticAberration.intensity.value, dashChromatic, Time.unscaledDeltaTime * chromaticSpeed);

                // ==========================================
                // 【核心新增】：触发屏幕重影时，同步喷射动漫速度线！
                // ==========================================
                if (speedLines != null && !speedLines.isPlaying)
                {
                    speedLines.Play();
                }
            }
            else
            {
                _chromaticAberration.intensity.value = Mathf.Lerp(_chromaticAberration.intensity.value, 0f, Time.unscaledDeltaTime * (chromaticSpeed * 0.5f));

                // ==========================================
                // 【核心新增】：重影消退时，干净利落地掐断速度线！
                // ==========================================
                if (speedLines != null && speedLines.isPlaying)
                {
                    speedLines.Stop();
                }
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