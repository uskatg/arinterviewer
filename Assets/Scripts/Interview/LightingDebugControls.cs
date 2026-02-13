using UnityEngine;

public class LightingDebugControls : MonoBehaviour
{
    [Header("Reference")]
    public LightEstimationController lighting;

    [Header("Adjustment")]
    public float step = 0.1f;
    public float minBrightness = 0f;
    public float maxBrightness = 2f;

    [Header("Quest buttons (Meta / OVRInput)")]
    public bool useOVRInput = true;

    // Map buttons:
    // A / X -> increase
    // B / Y -> decrease
    public OVRInput.Button increaseButton = OVRInput.Button.One;
    public OVRInput.Button decreaseButton = OVRInput.Button.Two;

    [Header("Keyboard fallback (Editor)")]
    public KeyCode increaseKey = KeyCode.Equals; // + on many keyboards (often shift+=)
    public KeyCode decreaseKey = KeyCode.Minus;

    void Reset()
    {
        if (!lighting) lighting = GetComponent<LightEstimationController>();
    }

    void Awake()
    {
        if (!lighting) lighting = GetComponent<LightEstimationController>();
        if (!lighting)
        {
            Debug.LogError("LightingDebugControls: No LightEstimationController found.");
        }
    }

    void Update()
    {
        if (!lighting) return;

        bool inc = false;
        bool dec = false;

        // Meta Quest input (requires Meta/OVR to be in project)
        if (useOVRInput)
        {
            inc |= OVRInput.GetDown(increaseButton);
            dec |= OVRInput.GetDown(decreaseButton);

            // Optional: allow X/Y too (left controller)
            inc |= OVRInput.GetDown(OVRInput.Button.Three);
            dec |= OVRInput.GetDown(OVRInput.Button.Four);
        }

        // Keyboard fallback (Editor)
        inc |= Input.GetKeyDown(increaseKey);
        dec |= Input.GetKeyDown(decreaseKey);

        if (inc) Adjust(+step);
        if (dec) Adjust(-step);
    }

    private void Adjust(float delta)
    {
        lighting.manualBrightness = Mathf.Clamp(lighting.manualBrightness + delta, minBrightness, maxBrightness);
        Debug.Log($"[Lighting] manualBrightness = {lighting.manualBrightness:0.00}");
    }
}