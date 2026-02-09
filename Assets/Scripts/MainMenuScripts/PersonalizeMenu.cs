using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PersonalizeMenu : MonoBehaviour
{
    [System.Serializable]
    public class CvResult
    {
        public string job_title;
        public string name;
        public string email;
        public string phone;
        // extended fields can be added here
    }

    [System.Serializable]
    public class ProfileMeta
    {
        public int id;
        public string title;      // stable display title
        public string jsonPath;   // absolute path to stored json
    }

    [System.Serializable]
    public class ProfilesIndex
    {
        public List<ProfileMeta> profiles = new List<ProfileMeta>();
        public int selectedId = -1;
    }

    private const string PROFILE_COUNTER_KEY = "PROFILE_COUNTER";          // Profile 1/2/3... (fallback names)
    private const string PROFILE_ID_COUNTER_KEY = "PROFILE_ID_COUNTER";    // unique id per saved profile
    private const string INDEX_PATH_KEY = "PROFILES_INDEX_PATH";           // optional, debugging

    [Header("Backend")]
    [SerializeField] private string backendUrl = "http://localhost:8000/cv/parse";

    [Header("Panel")]
    [SerializeField] private GameObject personalizePanel;
    [SerializeField] private CanvasGroup panelGroup;

    [Header("Animation")]
    [SerializeField] private float animDuration = 0.18f;
    [SerializeField] private float startScale = 0.92f;

    [Header("CV Upload")]
    [SerializeField] private string[] allowedExtensions = new[] { ".pdf" };
    [SerializeField] private string savedFileName = "cv.pdf";
    public string SavedCvPath { get; private set; }

    [Header("Storage")]
    [Tooltip("File name for the profile index stored in persistentDataPath.")]
    [SerializeField] private string profilesIndexFileName = "profiles_index.json";
    public string ProfilesIndexPath { get; private set; }

    [Header("Profile UI")]
    [SerializeField] private Transform profilesContainer;
    [SerializeField] private GameObject profileButtonPrefab;
    [SerializeField] private Sprite emptyCircleSprite;
    [SerializeField] private Sprite tickSprite;

    [Header("Profile Selection")]
    [Tooltip("If true: only one profile can be selected at a time (radio behavior).")]
    [SerializeField] private bool singleSelect = true;

    [Header("Rebuild on Start")]
    [SerializeField] private bool rebuildProfileOnStart = true;

    [Header("Testing / Reset")]
    [Tooltip("If enabled, and there are NO saved profiles on disk, counters are reset so the first profile becomes 'Profile 1'.")]
    [SerializeField] private bool resetCountersIfNoProfiles = true;

    private Coroutine animRoutine;
    private Button selectedProfileButton;

    private ProfilesIndex index = new ProfilesIndex();

    private void Awake()
    {
        if (personalizePanel != null && panelGroup == null)
            panelGroup = personalizePanel.GetComponent<CanvasGroup>();

        SavedCvPath = Path.Combine(Application.persistentDataPath, savedFileName);
        ProfilesIndexPath = Path.Combine(Application.persistentDataPath, profilesIndexFileName);

        PlayerPrefs.SetString(INDEX_PATH_KEY, ProfilesIndexPath);
        PlayerPrefs.Save();

        HideInstant();

        if (rebuildProfileOnStart)
            LoadSavedJsonAndRebuildUI();
    }

    // ---------- UI Buttons ----------
    public void OpenPersonalizePanel() => Animate(true);
    public void ClosePersonalizePanel() => Animate(false);

    public void UploadCvButton()
    {
#if UNITY_EDITOR
        string path = EditorUtility.OpenFilePanel("Select CV (PDF)", "", "pdf");
        if (string.IsNullOrEmpty(path)) return;

        if (!IsAllowed(path))
        {
            Debug.LogWarning($"File type not allowed: {Path.GetExtension(path)}");
            return;
        }

        SaveFileToPersistentPath(path);
        StartCoroutine(ParseCvWithBackend(SavedCvPath));
        return;
#endif
        Debug.LogWarning("Upload CV on Quest/Android requires a native file picker plugin.");
    }

    private IEnumerator ParseCvWithBackend(string pdfPath)
    {
        if (!File.Exists(pdfPath))
        {
            Debug.LogError($"CV file not found at: {pdfPath}");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(backendUrl))
        {
            Debug.LogError("backendUrl is empty. Assign it in the Inspector.");
            yield break;
        }

        Debug.Log($"[CV] Sending to backend: {backendUrl}");
        Debug.Log($"[CV] Local PDF path: {pdfPath}");

        byte[] bytes = File.ReadAllBytes(pdfPath);

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", bytes, Path.GetFileName(pdfPath), "application/pdf");

        using (UnityWebRequest req = UnityWebRequest.Post(backendUrl, form))
        {
            req.timeout = 60;

#if UNITY_2022_1_OR_NEWER
            req.SetRequestHeader("Expect", "");
#endif

            yield return req.SendWebRequest();

            Debug.Log($"[CV] Backend HTTP status: {req.responseCode}");

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[CV] Request failed: {req.error}");
                Debug.LogError($"[CV] Response body: {req.downloadHandler.text}");
                yield break;
            }

            string json = req.downloadHandler.text;
            Debug.Log("[CV] Backend response JSON: " + json);

            CvResult res = null;
            try { res = JsonUtility.FromJson<CvResult>(json); } catch { }

            // IMPORTANT: If there are no saved profiles on disk, reset counters
            // so the first new upload becomes "Profile 1" (instead of Profile 12 from old tests).
            EnsureCountersAreSaneBeforeCreatingFallbackName();

            string title = (!string.IsNullOrEmpty(res?.job_title))
                ? res.job_title
                : GetNextFallbackProfileName(); // increments ONLY on new upload

            SaveNewProfile(json, title);
            LoadSavedJsonAndRebuildUI();
        }
    }

    // ---------- FIX: Reset counters if there are no saved profiles ----------
    private void EnsureCountersAreSaneBeforeCreatingFallbackName()
    {
        if (!resetCountersIfNoProfiles) return;

        // If there is no index file or index contains zero profiles, treat as "no CV uploaded"
        bool hasAnyProfiles = false;

        if (File.Exists(ProfilesIndexPath))
        {
            try
            {
                string idxJson = File.ReadAllText(ProfilesIndexPath);
                var loaded = JsonUtility.FromJson<ProfilesIndex>(idxJson);
                hasAnyProfiles = (loaded != null && loaded.profiles != null && loaded.profiles.Count > 0);
            }
            catch
            {
                hasAnyProfiles = false;
            }
        }

        if (!hasAnyProfiles)
        {
            // Reset fallback naming counter (Profile 1 next time)
            PlayerPrefs.SetInt(PROFILE_COUNTER_KEY, 0);

            // Optional: also reset id counter to keep ids small during testing
            // If you DON'T want ids to reset, comment this out.
            PlayerPrefs.SetInt(PROFILE_ID_COUNTER_KEY, 0);

            PlayerPrefs.Save();

            Debug.Log("[CV] No saved profiles detected -> reset PROFILE_COUNTER (and PROFILE_ID counter). Next fallback will be 'Profile 1'.");
        }
    }

    // ---------- Save per-profile JSON + update index ----------
    private void SaveNewProfile(string json, string displayTitle)
    {
        // Always load index fresh here (important if file exists but in-memory index is empty)
        LoadIndexFromDisk(forceReload: true);

        int newId = GetNextProfileId();
        string jsonFileName = $"cv_profile_{newId}.json";
        string jsonPath = Path.Combine(Application.persistentDataPath, jsonFileName);

        try
        {
            File.WriteAllText(jsonPath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[CV] Failed to save profile JSON: " + e.Message);
            return;
        }

        var meta = new ProfileMeta
        {
            id = newId,
            title = displayTitle,
            jsonPath = jsonPath
        };

        index.profiles.Add(meta);
        index.selectedId = newId;
        SaveIndexToDisk();

        Debug.Log($"[CV] Saved new profile id={newId}, title='{displayTitle}', json='{jsonPath}'");
    }

    // ---------- Stable "Profile N" naming (only for NEW profiles) ----------
    private string GetNextFallbackProfileName()
    {
        int count = PlayerPrefs.GetInt(PROFILE_COUNTER_KEY, 0);
        count++;
        PlayerPrefs.SetInt(PROFILE_COUNTER_KEY, count);
        PlayerPrefs.Save();
        return $"Profile {count}";
    }

    // ---------- Unique profile ID generator ----------
    private int GetNextProfileId()
    {
        int id = PlayerPrefs.GetInt(PROFILE_ID_COUNTER_KEY, 0);
        id++;
        PlayerPrefs.SetInt(PROFILE_ID_COUNTER_KEY, id);
        PlayerPrefs.Save();
        return id;
    }

    // ---------- Index persistence ----------
    private void LoadIndexFromDisk(bool forceReload = false)
    {
        if (!forceReload && index != null && index.profiles != null && index.profiles.Count > 0)
            return;

        if (!File.Exists(ProfilesIndexPath))
        {
            index = new ProfilesIndex();
            return;
        }

        try
        {
            string json = File.ReadAllText(ProfilesIndexPath);
            var loaded = JsonUtility.FromJson<ProfilesIndex>(json);
            index = loaded != null ? loaded : new ProfilesIndex();
            if (index.profiles == null) index.profiles = new List<ProfileMeta>();
        }
        catch (System.Exception e)
        {
            Debug.LogError("[CV] Failed to load profiles index: " + e.Message);
            index = new ProfilesIndex();
        }
    }

    private void SaveIndexToDisk()
    {
        try
        {
            if (index.profiles == null) index.profiles = new List<ProfileMeta>();
            string json = JsonUtility.ToJson(index, prettyPrint: true);
            File.WriteAllText(ProfilesIndexPath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[CV] Failed to save profiles index: " + e.Message);
        }
    }

    // ---------- Load saved JSON and rebuild UI (MULTI PROFILES) ----------
    public void LoadSavedJsonAndRebuildUI()
    {
        index = new ProfilesIndex();
        LoadIndexFromDisk(forceReload: true);

        ClearProfilesContainer();

        if (index.profiles == null || index.profiles.Count == 0)
        {
            Debug.Log("[CV] No saved profiles found to rebuild UI.");
            return;
        }

        foreach (var meta in index.profiles)
        {
            bool selected = (meta.id == index.selectedId);
            CreateProfileButton(meta.title, selected: selected, profileId: meta.id);
        }

        Debug.Log($"[CV] Rebuilt UI with {index.profiles.Count} profile(s). SelectedId={index.selectedId}");
    }

    private void ClearProfilesContainer()
    {
        if (profilesContainer == null) return;

        for (int i = profilesContainer.childCount - 1; i >= 0; i--)
            Destroy(profilesContainer.GetChild(i).gameObject);

        selectedProfileButton = null;
    }

    // ---------- Create job title button ----------
    private void CreateProfileButton(string title, bool selected, int profileId)
    {
        if (profilesContainer == null || profileButtonPrefab == null)
        {
            Debug.LogError("ProfilesContainer or ProfileButtonPrefab not assigned.");
            return;
        }

        GameObject btnObj = Instantiate(profileButtonPrefab, profilesContainer);
        btnObj.SetActive(true);
        btnObj.transform.SetAsLastSibling();

        TMP_Text titleText = btnObj.GetComponentInChildren<TMP_Text>(true);
        if (titleText != null) titleText.text = title;

        Transform iconTf = btnObj.transform.Find("SelectIcon");
        if (iconTf == null) iconTf = btnObj.transform.Find("SelectIcon P");

        if (iconTf == null)
        {
            Debug.LogWarning("SelectIcon not found on profile button prefab. Name it 'SelectIcon' (recommended).");
            return;
        }

        Image iconImg = iconTf.GetComponent<Image>();
        if (iconImg == null)
        {
            Debug.LogWarning("SelectIcon object has no Image component.");
            return;
        }

        Button btn = btnObj.GetComponent<Button>();
        if (btn == null)
        {
            Debug.LogWarning("ProfileButton prefab root needs a Button component.");
            return;
        }

        SetProfileSelected(btn, iconImg, selected);

        if (singleSelect && selected)
            selectedProfileButton = btn;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            if (!singleSelect)
            {
                bool isSelectedNow = (iconImg.sprite == tickSprite);
                iconImg.sprite = isSelectedNow ? emptyCircleSprite : tickSprite;
                return;
            }

            UnselectAllProfileIcons();
            SetProfileSelected(btn, iconImg, true);
            selectedProfileButton = btn;

            index.selectedId = profileId;
            SaveIndexToDisk();

            Debug.Log($"[CV] Selected profile id={profileId}, title='{title}'");
        });
    }

    private void UnselectAllProfileIcons()
    {
        if (profilesContainer == null) return;

        for (int i = 0; i < profilesContainer.childCount; i++)
        {
            var child = profilesContainer.GetChild(i);
            var icon = FindIconImage(child);
            if (icon != null) icon.sprite = emptyCircleSprite;
        }
    }

    private void SetProfileSelected(Button btn, Image iconImg, bool selected)
    {
        iconImg.sprite = selected ? tickSprite : emptyCircleSprite;
    }

    private Image FindIconImage(Transform root)
    {
        Transform iconTf = root.Find("SelectIcon");
        if (iconTf == null) iconTf = root.Find("SelectIcon P");
        return iconTf != null ? iconTf.GetComponent<Image>() : null;
    }

    // ---------- Animation ----------
    private void Animate(bool show)
    {
        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(AnimateRoutine(show));
    }

    private IEnumerator AnimateRoutine(bool show)
    {
        personalizePanel.SetActive(true);

        float t = 0f;
        float fromA = panelGroup.alpha;
        float toA = show ? 1f : 0f;

        Vector3 fromS = personalizePanel.transform.localScale;
        Vector3 toS = show ? Vector3.one : Vector3.one * startScale;

        if (show)
        {
            personalizePanel.transform.localScale = Vector3.one * startScale;
            fromS = personalizePanel.transform.localScale;
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
            personalizePanel.transform.localScale = Vector3.Lerp(fromS, toS, p);
            yield return null;
        }

        panelGroup.alpha = toA;
        panelGroup.interactable = show;
        panelGroup.blocksRaycasts = show;

        animRoutine = null;
    }

    private void HideInstant()
    {
        personalizePanel.SetActive(true);
        panelGroup.alpha = 0f;
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;
        personalizePanel.transform.localScale = Vector3.one * startScale;
    }

    // ---------- File Saving ----------
    private bool IsAllowed(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        foreach (var a in allowedExtensions)
            if (ext == a) return true;
        return false;
    }

    private void SaveFileToPersistentPath(string sourcePath)
    {
        try
        {
            File.Copy(sourcePath, SavedCvPath, overwrite: true);
            PlayerPrefs.SetString("CV_PATH", SavedCvPath);
            PlayerPrefs.Save();
            Debug.Log($"CV saved to: {SavedCvPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save CV: {e.Message}");
        }
    }
   // Clear Button
    public void ClearAllProfiles()
{
    // 1) Clear UI immediately
    ClearProfilesContainer();

    // 2) Delete all saved profile JSON files
    try
    {
        string dir = Application.persistentDataPath;
        string[] files = Directory.GetFiles(dir, "cv_profile_*.json");

        foreach (var f in files)
        {
            File.Delete(f);
        }

        Debug.Log($"[CV] Deleted {files.Length} profile json file(s).");
    }
    catch (System.Exception e)
    {
        Debug.LogError("[CV] Failed deleting profile json files: " + e.Message);
    }

    // 3) Delete the index file
    try
    {
        if (File.Exists(ProfilesIndexPath))
        {
            File.Delete(ProfilesIndexPath);
            Debug.Log("[CV] Deleted profiles_index.json");
        }
    }
    catch (System.Exception e)
    {
        Debug.LogError("[CV] Failed deleting profiles index: " + e.Message);
    }

    // 4) Reset counters + selection
    PlayerPrefs.SetInt(PROFILE_COUNTER_KEY, 0);
    PlayerPrefs.SetInt(PROFILE_ID_COUNTER_KEY, 0);
    PlayerPrefs.Save();

    // Reset in-memory index
    index = new ProfilesIndex();

    Debug.Log("[CV] Cleared all profiles. Next upload will start from Profile 1.");
}
}