using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TrippyEffectController : MonoBehaviour
{
    [Header("Volume Reference")]
    [SerializeField] private Volume postProcessVolume;

    [Header("Animation Settings")]
    [SerializeField] private float animationSpeed = 2f;
    [SerializeField] private float maxDistortion = 0.5f;

    private DepthOfField depthOfField;
    private LensDistortion lensDistortion;
    private ChromaticAberration chromaticAberration;

    void Start()
    {
        // Try to get the profile components
        if (postProcessVolume.profile.TryGet(out depthOfField) &&
            postProcessVolume.profile.TryGet(out lensDistortion) &&
            postProcessVolume.profile.TryGet(out chromaticAberration))
        {
            // Enable overrides
            depthOfField.active = true;
            lensDistortion.active = true;
            chromaticAberration.active = true;
        }
    }

    void Update()
    {
        if (depthOfField == null || lensDistortion == null || chromaticAberration == null) return;

        // Use a sine wave to create a continuous breathing/pulsing rhythm
        float wave = Mathf.Sin(Time.time * animationSpeed);

        // Oscillate Focus Distance to make the blur shift closer and further away
        depthOfField.focusDistance.value = Mathf.Lerp(0.2f, 3.0f, (wave + 1f) / 2f);

        // Pulse the screen warping effect back and forth
        lensDistortion.intensity.value = wave * maxDistortion;

        // Match the color splitting intensity to the pulse peak
        chromaticAberration.intensity.value = Mathf.Abs(wave);
    }
}