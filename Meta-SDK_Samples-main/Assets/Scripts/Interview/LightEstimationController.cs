using UnityEngine;

/// Meta-friendly lighting controller:
/// - No ARFoundation dependency
/// - Drives Key/Fill/Rim from a simple "environment brightness" signal
/// - Keeps your smoothing, clamps, fallback capture, and blending workflow
///
/// Attach this to a stable object (e.g., LightingController), NOT to an AR camera.
/// Reference your Key/Fill/Rim lights in the inspector.
public class LightEstimationController : MonoBehaviour
{
    [Header("Environment Source (Meta-friendly)")]
    [Tooltip("0 = ignore environment, 1 = fully driven by environment estimation")]
    [Range(0f, 1f)] public float envBlend = 1f;

    [Tooltip("Manual brightness fallback (0..2 typical). Use this if no automatic signal is available.")]
    [Range(0f, 2f)] public float manualBrightness = 1f;

    [Tooltip("Optional: assign a component that implements IEnvironmentLightProvider to provide brightness/color automatically.")]
    public MonoBehaviour environmentProviderBehaviour;

    private IEnvironmentLightProvider _provider;

    [Header("Lights to drive")]
    public Light keyLight;   // main light (directional recommended)
    public Light fillLight;  // soft ambient fill
    public Light rimLight;   // optional rim

    [Header("Smoothing (higher = slower changes, less jitter)")]
    [Range(0f, 25f)] public float smooth = 10f;

    [Header("Intensity scaling + clamps")]
    public float keyIntensityMultiplier = 1.0f;
    public float fillFromAmbientMultiplier = 0.6f;

    public float keyMin = 0.2f;
    public float keyMax = 2.5f;

    public float fillMin = 0.0f;
    public float fillMax = 1.5f;

    [Header("Color blending")]
    [Range(0f, 1f)]
    public float useAmbientColorAmount = 0.35f; // 0 = keep your light colors, 1 = tint by environment

    [Header("Fallback values (used when env data is missing)")]
    public bool captureFallbackOnStart = true;

    private float _fallbackKeyIntensity;
    private float _fallbackFillIntensity;
    private float _fallbackRimIntensity;

    private Color _fallbackKeyColor;
    private Color _fallbackFillColor;
    private Color _fallbackRimColor;

    private Quaternion _fallbackKeyRotation;

    // Smoothed current values
    private float _curKeyIntensity;
    private float _curFillIntensity;
    private float _curRimIntensity;

    private Color _curKeyColor;
    private Color _curFillColor;
    private Color _curRimColor;

    private Quaternion _curKeyRotation;

    private void Awake()
    {
        if (environmentProviderBehaviour != null)
            _provider = environmentProviderBehaviour as IEnvironmentLightProvider;
    }

    private void Start()
    {
        if (captureFallbackOnStart)
            CaptureFallback();

        // Initialize smoothed values from current lights
        if (keyLight)
        {
            _curKeyIntensity = keyLight.intensity;
            _curKeyColor = keyLight.color;
            _curKeyRotation = keyLight.transform.rotation;
        }
        if (fillLight)
        {
            _curFillIntensity = fillLight.intensity;
            _curFillColor = fillLight.color;
        }
        if (rimLight)
        {
            _curRimIntensity = rimLight.intensity;
            _curRimColor = rimLight.color;
        }
    }

    public void CaptureFallback()
    {
        if (keyLight)
        {
            _fallbackKeyIntensity = keyLight.intensity;
            _fallbackKeyColor = keyLight.color;
            _fallbackKeyRotation = keyLight.transform.rotation;
        }
        if (fillLight)
        {
            _fallbackFillIntensity = fillLight.intensity;
            _fallbackFillColor = fillLight.color;
        }
        if (rimLight)
        {
            _fallbackRimIntensity = rimLight.intensity;
            _fallbackRimColor = rimLight.color;
        }
    }

    private void Update()
    {
        // Default targets = fallback
        float targetKeyIntensity = _fallbackKeyIntensity;
        float targetFillIntensity = _fallbackFillIntensity;
        float targetRimIntensity = _fallbackRimIntensity;

        Color targetKeyColor = _fallbackKeyColor;
        Color targetFillColor = _fallbackFillColor;
        Color targetRimColor = _fallbackRimColor;

        Quaternion targetKeyRot = _fallbackKeyRotation;

        // Read environment signal (provider preferred, manual fallback)
        bool hasEnv = false;
        float brightness = manualBrightness;
        Color envColor = Color.white;
        Vector3? mainLightDir = null;

        if (_provider != null && _provider.TryGetEnvironment(out var env))
        {
            hasEnv = true;
            brightness = env.brightness;
            envColor = env.color;
            mainLightDir = env.mainLightDirection;
        }

        // 1) Main light direction (optional)
        if (mainLightDir.HasValue)
        {
            Vector3 dir = mainLightDir.Value;
            if (dir.sqrMagnitude > 0.0001f)
                targetKeyRot = Quaternion.LookRotation(-dir.normalized, Vector3.up);
        }

        // 2) Brightness -> intensities
        // Treat brightness roughly like "averageBrightness" from AR systems (0..~2).
        float mappedKey = Mathf.Clamp(brightness * keyIntensityMultiplier, keyMin, keyMax);
        float mappedFill = Mathf.Clamp(brightness * fillFromAmbientMultiplier, fillMin, fillMax);

        targetKeyIntensity = mappedKey;
        targetFillIntensity = mappedFill;

        // 3) Ambient tint (optional)
        targetKeyColor  = Color.Lerp(_fallbackKeyColor,  envColor * _fallbackKeyColor,  useAmbientColorAmount);
        targetFillColor = Color.Lerp(_fallbackFillColor, envColor * _fallbackFillColor, useAmbientColorAmount);
        targetRimColor  = Color.Lerp(_fallbackRimColor,  envColor * _fallbackRimColor,  useAmbientColorAmount);

        // 4) Rim ties to key (keeps it stable in MR)
        if (rimLight)
            targetRimIntensity = Mathf.Clamp(targetKeyIntensity * 0.35f, 0f, 1.2f);

        // If provider missing, we still treat manual as "hasEnv"
        if (!hasEnv) hasEnv = true;

        // Blend env targets with fallback targets
        float b = envBlend;

        // Smoothing
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        float t = 1f - Mathf.Exp(-smooth * dt);

        if (keyLight)
        {
            _curKeyIntensity = Mathf.Lerp(_curKeyIntensity, Mathf.Lerp(_fallbackKeyIntensity, targetKeyIntensity, b), t);
            _curKeyColor = Color.Lerp(_curKeyColor, Color.Lerp(_fallbackKeyColor, targetKeyColor, b), t);
            _curKeyRotation = Quaternion.Slerp(_curKeyRotation, Quaternion.Slerp(_fallbackKeyRotation, targetKeyRot, b), t);

            keyLight.intensity = _curKeyIntensity;
            keyLight.color = _curKeyColor;
            keyLight.transform.rotation = _curKeyRotation;
        }

        if (fillLight)
        {
            _curFillIntensity = Mathf.Lerp(_curFillIntensity, Mathf.Lerp(_fallbackFillIntensity, targetFillIntensity, b), t);
            _curFillColor = Color.Lerp(_curFillColor, Color.Lerp(_fallbackFillColor, targetFillColor, b), t);

            fillLight.intensity = _curFillIntensity;
            fillLight.color = _curFillColor;
        }

        if (rimLight)
        {
            _curRimIntensity = Mathf.Lerp(_curRimIntensity, Mathf.Lerp(_fallbackRimIntensity, targetRimIntensity, b), t);
            _curRimColor = Color.Lerp(_curRimColor, Color.Lerp(_fallbackRimColor, targetRimColor, b), t);

            rimLight.intensity = _curRimIntensity;
            rimLight.color = _curRimColor;
        }
    }

    // -----------------------------
    // Optional provider interface
    // -----------------------------
    public interface IEnvironmentLightProvider
    {
        bool TryGetEnvironment(out EnvironmentLight env);
    }

    public struct EnvironmentLight
    {
        public float brightness;              // ~0..2 typical
        public Color color;                   // white if unknown
        public Vector3? mainLightDirection;   // null if unknown
    }
}