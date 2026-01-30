using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class QuickMenuController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject quickPanel;
    [SerializeField] private CanvasGroup panelGroup;

    [Header("Animation")]
    [SerializeField] private float animDuration = 0.18f;
    [SerializeField] private float startScale = 0.92f;

    [Header("Pause Behavior")]
    [SerializeField] private bool pauseTimeWhenOpen = true;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string analysisSceneName = "Analysis"; //

    private bool isOpen;
    private Coroutine animRoutine;

    private void Awake()
    {
        if (quickPanel != null && panelGroup == null)
            panelGroup = quickPanel.GetComponent<CanvasGroup>();

        CollapseInstant();
    }

    // ---------------- TOGGLE ----------------
    public void ToggleQuickMenu()
    {
        SetOpen(!isOpen);
    }

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

        Vector3 fromS = quickPanel.transform.localScale;
        Vector3 toS = show ? Vector3.one : Vector3.one * startScale;

        if (show)
        {
            quickPanel.transform.localScale = Vector3.one * startScale;
            fromS = quickPanel.transform.localScale;
            toS = Vector3.one;
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
        quickPanel.transform.localScale = Vector3.one * startScale;

        isOpen = false;
    }

    // ---------------- BUTTON ACTIONS ----------------
    public void Resume()
    {
        CloseQuickMenu();
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OpenSettings()
    {
        Debug.Log("Open Settings clicked");
        // hook into SettingsMenuController later
    }

    public void End()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // ---------------- NEW: FINISH INTERVIEW ----------------
    public void FinishInterview()
    {
        Debug.Log("Interview finished → Analysis scene");

        Time.timeScale = 1f;
        SceneManager.LoadScene(analysisSceneName);
    }
}