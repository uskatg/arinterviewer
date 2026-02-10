using UnityEngine;

public class BlobShadow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;          // avatar root / hips
    [SerializeField] private Transform ground;          // optional: ground plane (can be null)
    [SerializeField] private Renderer blobRenderer;     // quad's renderer

    [Header("Tuning")]
    [SerializeField] private float heightAboveGround = 0.01f;
    [SerializeField] private float baseScale = 0.8f;
    [SerializeField] private float maxAlpha = 0.6f;
    [SerializeField] private float fadeStartHeight = 0.2f;
    [SerializeField] private float fadeEndHeight = 1.2f;

    private MaterialPropertyBlock _mpb;

    private void Awake()
    {
        if (blobRenderer == null) blobRenderer = GetComponentInChildren<Renderer>();
        _mpb = new MaterialPropertyBlock();
    }

    private void LateUpdate()
    {
        if (target == null || blobRenderer == null) return;

        // Find ground Y
        float groundY = ground != null ? ground.position.y : transform.position.y;

        // Position under target
        Vector3 p = target.position;
        p.y = groundY + heightAboveGround;
        transform.position = p;

        // Keep it flat
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // Scale
        transform.localScale = new Vector3(baseScale, baseScale, baseScale);

        // Fade alpha with height
        float h = Mathf.Max(0f, target.position.y - groundY);
        float t = Mathf.InverseLerp(fadeEndHeight, fadeStartHeight, h); // higher -> less alpha
        float a = Mathf.Clamp01(t) * maxAlpha;

        blobRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor("_BaseColor", new Color(0f, 0f, 0f, a)); // URP Unlit uses _BaseColor
        blobRenderer.SetPropertyBlock(_mpb);
    }
}