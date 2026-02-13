using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsMenuController : MonoBehaviour
{
    // ---------- Persist Keys ----------
    private const string KEY_SUBTITLES_ON = "SET_SUBTITLES_ON";
    private const string KEY_SUBTITLE_SIZE = "SET_SUBTITLE_SIZE";
    private const string KEY_VOICE_SPEED = "SET_VOICE_SPEED";
    private const string KEY_SPEECH_MODE = "SET_SPEECH_MODE";
    private const string KEY_SPEECH_LANG = "SET_SPEECH_LANG";
    private const string KEY_SPEECH_SENS = "SET_SPEECH_SENS";

    public enum SpeechMode { AlwaysOn = 0, PushToTalk = 1 }

    [Header("Panel")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private CanvasGroup panelGroup;

    [Header("Animation")]
    [SerializeField] private float animDuration = 0.18f;
    [SerializeField] private float startScale = 0.92f;
    private Coroutine animRoutine;

    [Header("UI - Subtitles")]
    [SerializeField] private Toggle subtitlesToggle;
    [SerializeField] private Slider subtitleSizeSlider;   // 0..2 (Small/Med/Large) OR 0.8..1.4
    [SerializeField] private TMP_Text subtitleSizeValue;

    [Header("UI - Voice")]
    [SerializeField] private Slider voiceSpeedSlider;     // e.g., 0.75..1.25
    [SerializeField] private TMP_Text voiceSpeedValue;

    [Header("UI - Speech Recognition")]
    [SerializeField] private TMP_Dropdown speechModeDropdown;   // 0 Always, 1 PTT
    [SerializeField] private TMP_Dropdown languageDropdown;     // 0 EN, 1 DE, ...
    [SerializeField] private Slider sensitivitySlider;          // 0..1
    [SerializeField] private TMP_Text sensitivityValue;

    private void Awake()
    {
        if (settingsPanel != null && panelGroup == null)
            panelGroup = settingsPanel.GetComponent<CanvasGroup>();

        HideInstant();
        LoadToUI();
        HookUIEvents();
    }

    // ---------- Open / Close ----------
    public void OpenSettings() => Animate(true);
    public void CloseSettings() => Animate(false);

    // ---------- UI wiring ----------
    private void HookUIEvents()
    {
        if (subtitlesToggle != null)
            subtitlesToggle.onValueChanged.AddListener(OnSubtitlesChanged);

        if (subtitleSizeSlider != null)
            subtitleSizeSlider.onValueChanged.AddListener(OnSubtitleSizeChanged);

        if (voiceSpeedSlider != null)
            voiceSpeedSlider.onValueChanged.AddListener(OnVoiceSpeedChanged);

        if (speechModeDropdown != null)
            speechModeDropdown.onValueChanged.AddListener(OnSpeechModeChanged);

        if (languageDropdown != null)
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);

        if (sensitivitySlider != null)
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
    }

    // ---------- Callbacks (save + update labels) ----------
    private void OnSubtitlesChanged(bool on)
    {
        PlayerPrefs.SetInt(KEY_SUBTITLES_ON, on ? 1 : 0);
        PlayerPrefs.Save();

        // Optional UX: disable size slider when subtitles off
        if (subtitleSizeSlider != null) subtitleSizeSlider.interactable = on;

        // TODO: notify subtitle system later
        // SubtitleManager.Instance.SetEnabled(on);
    }

    private void OnSubtitleSizeChanged(float v)
    {
        // Option A: treat as scale directly (recommended)
        PlayerPrefs.SetFloat(KEY_SUBTITLE_SIZE, v);
        PlayerPrefs.Save();
        if (subtitleSizeValue != null) subtitleSizeValue.text = $"{v:0.00}x";

        // TODO: SubtitleManager.Instance.SetScale(v);
    }

    private void OnVoiceSpeedChanged(float v)
    {
        PlayerPrefs.SetFloat(KEY_VOICE_SPEED, v);
        PlayerPrefs.Save();
        if (voiceSpeedValue != null) voiceSpeedValue.text = $"{v:0.00}x";

        // TODO: VoiceManager.Instance.SetSpeed(v);
    }

    private void OnSpeechModeChanged(int idx)
    {
        PlayerPrefs.SetInt(KEY_SPEECH_MODE, idx);
        PlayerPrefs.Save();

        // TODO: SpeechManager.Instance.SetMode((SpeechMode)idx);
    }

    private void OnLanguageChanged(int idx)
    {
        PlayerPrefs.SetInt(KEY_SPEECH_LANG, idx);
        PlayerPrefs.Save();

        // TODO: SpeechManager.Instance.SetLanguage(languageDropdown.options[idx].text);
    }

    private void OnSensitivityChanged(float v)
    {
        PlayerPrefs.SetFloat(KEY_SPEECH_SENS, v);
        PlayerPrefs.Save();
        if (sensitivityValue != null) sensitivityValue.text = $"{Mathf.RoundToInt(v * 100f)}%";

        // TODO: SpeechManager.Instance.SetSensitivity(v);
    }

    // ---------- Load saved settings into UI ----------
    private void LoadToUI()
    {
        bool subtitlesOn = PlayerPrefs.GetInt(KEY_SUBTITLES_ON, 1) == 1;
        float subtitleSize = PlayerPrefs.GetFloat(KEY_SUBTITLE_SIZE, 1.0f);
        float voiceSpeed = PlayerPrefs.GetFloat(KEY_VOICE_SPEED, 1.0f);
        int speechMode = PlayerPrefs.GetInt(KEY_SPEECH_MODE, (int)SpeechMode.PushToTalk);
        int speechLang = PlayerPrefs.GetInt(KEY_SPEECH_LANG, 0);
        float sens = PlayerPrefs.GetFloat(KEY_SPEECH_SENS, 0.6f);

        if (subtitlesToggle != null) subtitlesToggle.isOn = subtitlesOn;

        if (subtitleSizeSlider != null)
        {
            subtitleSizeSlider.value = subtitleSize;
            subtitleSizeSlider.interactable = subtitlesOn;
        }
        if (subtitleSizeValue != null) subtitleSizeValue.text = $"{subtitleSize:0.00}x";

        if (voiceSpeedSlider != null) voiceSpeedSlider.value = voiceSpeed;
        if (voiceSpeedValue != null) voiceSpeedValue.text = $"{voiceSpeed:0.00}x";

        if (speechModeDropdown != null) speechModeDropdown.value = speechMode;
        if (languageDropdown != null) languageDropdown.value = speechLang;

        if (sensitivitySlider != null) sensitivitySlider.value = sens;
        if (sensitivityValue != null) sensitivityValue.text = $"{Mathf.RoundToInt(sens * 100f)}%";
    }

    // ---------- Popup animation (same pattern you use) ----------
    private void Animate(bool show)
    {
        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(AnimateRoutine(show));
    }

    private IEnumerator AnimateRoutine(bool show)
    {
        settingsPanel.SetActive(true);

        float t = 0f;
        float fromA = panelGroup.alpha;
        float toA = show ? 1f : 0f;

        Vector3 fromS = settingsPanel.transform.localScale;
        Vector3 toS = show ? Vector3.one : Vector3.one * startScale;

        if (show)
        {
            settingsPanel.transform.localScale = Vector3.one * startScale;
            fromS = settingsPanel.transform.localScale;
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
            settingsPanel.transform.localScale = Vector3.Lerp(fromS, toS, p);
            yield return null;
        }

        panelGroup.alpha = toA;
        panelGroup.interactable = show;
        panelGroup.blocksRaycasts = show;

        animRoutine = null;
    }

    private void HideInstant()
    {
        if (settingsPanel == null || panelGroup == null) return;
        settingsPanel.SetActive(true);
        panelGroup.alpha = 0f;
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;
        settingsPanel.transform.localScale = Vector3.one * startScale;
    }
}