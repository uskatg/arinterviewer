using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class AnalysisReportUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI ratingScoreText;
    public TextMeshProUGUI feedbackSummaryText;
    public TextMeshProUGUI strengthsText;
    public TextMeshProUGUI areasToImproveText;
    public TextMeshProUGUI missionText;
    public TextMeshProUGUI voiceStats;

    [Header("Dependencies")]
    public InterviewManager interviewManager;

    private void OnEnable()
    {
        if (interviewManager == null)
        {
            interviewManager = FindObjectOfType<InterviewManager>();
        }

        if (interviewManager != null && !string.IsNullOrEmpty(interviewManager.currentSessionId))
        {
            Debug.Log($"Analysis UI Activated. Fetching report for Session: {interviewManager.currentSessionId}");
            StartCoroutine(GetReportRoutine(interviewManager.currentSessionId));
        }
        else
        {
            Debug.LogWarning("Analysis UI: Cannot fetch report. InterviewManager is missing or Session ID is empty.");
        }
    }

    private IEnumerator GetReportRoutine(string sessionId)
    {

        string baseUrl = interviewManager.backendUrl.TrimEnd('/');
        string requestUrl = $"{baseUrl}/report/{sessionId}";

        using (UnityWebRequest request = UnityWebRequest.Get(requestUrl))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Report API Error: {request.error}\nURL: {requestUrl}");
                UpdateUIWithError();
            }
            else
            {
                string jsonResponse = request.downloadHandler.text;
                Debug.Log($"Report Received: {jsonResponse}");

                try
                {
                    ReportResponse data = JsonUtility.FromJson<ReportResponse>(jsonResponse);
                    UpdateUI(data);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"JSON Parse Error: {e.Message}");
                }
            }
        }
    }

    private void UpdateUI(ReportResponse data)
    {
        if (ratingScoreText != null)
            ratingScoreText.text = $"{data.score}/100";

        if (feedbackSummaryText != null)
            feedbackSummaryText.text = data.feedback_summary;

        if (strengthsText != null)
            strengthsText.text = FormatListToBulletPoints(data.strengths);

        if (areasToImproveText != null)
            areasToImproveText.text = FormatListToBulletPoints(data.areas_for_improvement);

        if (missionText != null)
            missionText.text = data.mission;

        if (voiceStats != null)
            voiceStats.text = data.voice_analysis;
    }

    private void UpdateUIWithError()
    {
        if (feedbackSummaryText != null) feedbackSummaryText.text = "Failed to load report data.";
    }

    private string FormatListToBulletPoints(List<string> items)
    {
        if (items == null || items.Count == 0) return "None";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var item in items)
        {
            sb.AppendLine($"• {item}");
        }
        return sb.ToString();
    }

    [System.Serializable]
    public class ReportResponse
    {
        public string session_id;
        public int score;
        public string feedback_summary;
        public List<string> strengths;
        public List<string> areas_for_improvement;
        public string mission;
        public string voice_analysis;
    }
}