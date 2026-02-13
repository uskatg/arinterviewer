using System.Collections;
using UnityEngine;

public class QuickMenuController : MonoBehaviour

{
    [Header("Scene Objects")]
    [SerializeField] private GameObject menuEmpty;
    [SerializeField] private GameObject interviewEmpty;
    [SerializeField] private GameObject analysisEmpty;

    [Header("Restart Target (IMPORTANT)")]
    [Tooltip("A CHILD of Interview that contains the session content to reset (e.g., AvatarRoot). Do NOT set this to the Interview root.")]
    [SerializeField] private GameObject interviewContentRoot;

    [Header("References")]
    [SerializeField] private GameObject quickPanel;
    [SerializeField] private CanvasGroup panelGroup;

    [Header("Animation")]
    [SerializeField] private float animDuration = 0.18f;
    [SerializeField] private float startScale = 0.92f;

    [Header("Pause Behavior")]
    [SerializeField] private bool pauseTimeWhenOpen = true;
    [Header("UI Objects")]
    [SerializeField] private GameObject quickToggle;

    private bool isOpen;
    private Coroutine animRoutine;
    private Vector3 baseScale;

    private void Awake()
    {
        if (quickPanel != null)
        {
            if (panelGroup == null)
                panelGroup = quickPanel.GetComponent<CanvasGroup>();

            baseScale = quickPanel.transform.localScale;
        }

        CollapseInstant();
    }

    // ---------------- TOGGLE ----------------
    public void ToggleQuickMenu() => SetOpen(!isOpen);
    public void OpenQuickMenu() => SetOpen(true);
    public void CloseQuickMenu() => SetOpen(false);

    private void SetOpen(bool open)
    {
        isOpen = open;

        if (pauseTimeWhenOpen)
            Time.timeScale = open ? 0f : 1f;

        Animate(open);
    }

    // ---------------- ANIMATION ----------------
    private void Animate(bool show)
    {
        if (quickPanel == null || panelGroup == null) return;

        if (animRoutine != null)
            StopCoroutine(animRoutine);

        animRoutine = StartCoroutine(AnimateRoutine(show));
    }

    private IEnumerator AnimateRoutine(bool show)
    {
        quickPanel.SetActive(true);

        float t = 0f;
        float fromA = panelGroup.alpha;
        float toA = show ? 1f : 0f;

        Vector3 hiddenScale = baseScale * startScale;
        Vector3 shownScale = baseScale;

        Vector3 fromS = quickPanel.transform.localScale;
        Vector3 toS = show ? shownScale : hiddenScale;

        if (show)
        {
            quickPanel.transform.localScale = hiddenScale;
            fromS = hiddenScale;
            toS = shownScale;
        }

        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;

        while (t < animDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / animDuration);
            p = p * p * (3f - 2f * p);

            panelGroup.alpha = Mathf.Lerp(fromA, toA, p);
            quickPanel.transform.localScale = Vector3.Lerp(fromS, toS, p);
            yield return null;
        }

        panelGroup.alpha = toA;
        quickPanel.transform.localScale = toS;
        panelGroup.interactable = show;
        panelGroup.blocksRaycasts = show;

        if (!show)
            quickPanel.SetActive(false);

        animRoutine = null;
    }

    private void CollapseInstant()
    {
        if (quickPanel == null || panelGroup == null) return;

        quickPanel.SetActive(false);
        panelGroup.alpha = 0f;
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;
        quickPanel.transform.localScale = baseScale * startScale;

        isOpen = false;
    }

    // ---------------- BUTTON ACTIONS ----------------
    public void Resume() => CloseQuickMenu();

    // ✅ Restart ONLY the session content (NOT Interview root)
public void Restart()
{
    Time.timeScale = 1f;
    StartCoroutine(RestartRoutine());
}

private IEnumerator RestartRoutine()
{
    // Close the quick menu
    CloseQuickMenu();
    yield return new WaitForSecondsRealtime(animDuration);

    // Ensure Interview stays active
    if (interviewEmpty != null)
        interviewEmpty.SetActive(true);

    // Reset interview content (child of Interview)
    if (interviewContentRoot == null)
    {
        Debug.LogError("[QuickMenuController] interviewContentRoot is not assigned.");
        yield break;
    }

    interviewContentRoot.SetActive(false);
    yield return null; // one frame
    interviewContentRoot.SetActive(true);

    // ✅ ALWAYS re-enable QuickToggle on restart
    if (quickToggle != null)
        quickToggle.SetActive(true);
}

    public void End()
    {
        Time.timeScale = 1f;
        StartCoroutine(SwitchStateRoutine(TargetState.Menu));
    }

    public void FinishInterview()
    {
        Time.timeScale = 1f;
        StartCoroutine(SwitchStateRoutine(TargetState.Analysis));
    }

    private enum TargetState { Menu, Analysis }

    private IEnumerator SwitchStateRoutine(TargetState target)
    {
        CloseQuickMenu();
        yield return new WaitForSecondsRealtime(animDuration);

        if (menuEmpty != null) menuEmpty.SetActive(target == TargetState.Menu);
        if (interviewEmpty != null) interviewEmpty.SetActive(false);
        if (analysisEmpty != null) analysisEmpty.SetActive(target == TargetState.Analysis);
    }
}