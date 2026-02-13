using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ModeMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject modePanel;
    [SerializeField] private CanvasGroup modeGroup;

    [Header("Animation")]
    [SerializeField] private float animDuration = 0.18f;
    [SerializeField] private float startScale = 0.92f;

    [Header("Selection Icons")]
    [SerializeField] private Image socialSelectIcon;
    [SerializeField] private Image technicalSelectIcon;

    [Header("Selection Sprites")]
    [SerializeField] private Sprite emptyCircleSprite;
    [SerializeField] private Sprite tickSprite;

    private Coroutine animRoutine;

    private enum Mode { None, Social, Technical }
    private Mode selectedMode = Mode.None;

    private void Awake()
    {
        // Auto-grab CanvasGroup if not assigned
        if (modePanel != null && modeGroup == null)
            modeGroup = modePanel.GetComponent<CanvasGroup>();

        HideInstant();
        UpdateSelectionUI();
    }

    // ---------------- Panel Control ----------------
    public void OpenModePanel() => Animate(true);
    public void CloseModePanel() => Animate(false);

    private void Animate(bool show)
    {
        if (animRoutine != null)
            StopCoroutine(animRoutine);

        animRoutine = StartCoroutine(AnimateRoutine(show));
    }

    private IEnumerator AnimateRoutine(bool show)
    {
        float t = 0f;

        float fromAlpha = modeGroup.alpha;
        float toAlpha = show ? 1f : 0f;

        Vector3 fromScale = modePanel.transform.localScale;
        Vector3 toScale = show ? Vector3.one : Vector3.one * startScale;

        if (show)
        {
            modePanel.transform.localScale = Vector3.one * startScale;
            fromScale = modePanel.transform.localScale;
        }

        modeGroup.interactable = false;
        modeGroup.blocksRaycasts = false;

        while (t < animDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / animDuration);
            p = p * p * (3f - 2f * p);

            modeGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, p);
            modePanel.transform.localScale = Vector3.Lerp(fromScale, toScale, p);

            yield return null;
        }

        modeGroup.alpha = toAlpha;
        modePanel.transform.localScale = toScale;

        if (show)
        {
            modeGroup.interactable = true;
            modeGroup.blocksRaycasts = true;
        }

        animRoutine = null;
    }

    private void HideInstant()
    {
        if (modePanel == null || modeGroup == null)
            return;

        modeGroup.alpha = 0f;
        modeGroup.interactable = false;
        modeGroup.blocksRaycasts = false;
        modePanel.transform.localScale = Vector3.one * startScale;
    }

    // ---------------- Mode Selection ----------------
    public void SelectSocialMode()
    {
        selectedMode = (selectedMode == Mode.Social) ? Mode.None : Mode.Social;
        UpdateSelectionUI();
        Debug.Log($"Selected Mode: {selectedMode}");
    }

    public void SelectTechnicalMode()
    {
        selectedMode = (selectedMode == Mode.Technical) ? Mode.None : Mode.Technical;
        UpdateSelectionUI();
        Debug.Log($"Selected Mode: {selectedMode}");
    }

    private void UpdateSelectionUI()
    {
        if (socialSelectIcon != null)
            socialSelectIcon.sprite = (selectedMode == Mode.Social) ? tickSprite : emptyCircleSprite;

        if (technicalSelectIcon != null)
            technicalSelectIcon.sprite = (selectedMode == Mode.Technical) ? tickSprite : emptyCircleSprite;
    }
}