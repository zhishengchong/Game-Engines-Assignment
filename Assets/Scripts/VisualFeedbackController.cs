using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VisualFeedbackController : MonoBehaviour
{
    [Header("后期处理配置")]
    public Volume globalVolume;

    [Header("视觉参数：慢动作时 (停下)")]
    public float slowSaturation = -60f; 
    public float slowVignette = 0.45f;  

    [Header("视觉参数：正常速度时 (跑动)")]
    public float normalSaturation = 0f; 
    public float normalVignette = 0.15f; 

    [Header("视觉参数：死亡时")]
    public float deathSaturation = -100f; // 彻底黑白
    public float deathVignette = 0.6f;    // 强烈的四周黑影
    public float deathTransitionSpeed = 5f; // 死亡滤镜的蔓延速度

    private ColorAdjustments _colorAdjustments;
    private Vignette _vignette;

    void Start()
    {
        if (globalVolume != null && globalVolume.profile != null)
        {
            globalVolume.profile.TryGet(out _colorAdjustments);
            globalVolume.profile.TryGet(out _vignette);
        }
    }

    void Update()
    {
        if (_colorAdjustments == null || _vignette == null) return;

        // ==========================================
        // 【电影级死亡过渡】：当时间静止时，滤镜缓慢蔓延
        // ==========================================
        if (Time.timeScale == 0f)
        {
            // 使用 unscaledDeltaTime 配合 Lerp，让色彩和暗角如潮水般平滑褪去和包裹
            _colorAdjustments.saturation.value = Mathf.Lerp(_colorAdjustments.saturation.value, deathSaturation, Time.unscaledDeltaTime * deathTransitionSpeed);
            _vignette.intensity.value = Mathf.Lerp(_vignette.intensity.value, deathVignette, Time.unscaledDeltaTime * deathTransitionSpeed);
            return; 
        }

        // 平时的平滑过渡逻辑 (根据 timeScale)
        float t = (Time.timeScale - 0.05f) / (1f - 0.05f);
        t = Mathf.Clamp01(t);

        _colorAdjustments.saturation.value = Mathf.Lerp(slowSaturation, normalSaturation, t);
        _vignette.intensity.value = Mathf.Lerp(slowVignette, normalVignette, t);
    }
}