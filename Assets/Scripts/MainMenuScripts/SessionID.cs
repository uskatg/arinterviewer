using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class PlayButtonSessionStarter : MonoBehaviour
{
    [Header("Backend URL")]
    [SerializeField] private string baseUrl = "http://localhost:8000";

    public static string CurrentSessionId;

    // ---- Request Body Model ----
    [Serializable]
    private class InitRequest
    {
        public string mode;
        public string difficulty;
    }

    // ---- Response Model ----
    [Serializable]
    private class InitResponse
    {
        public string session_id;
    }

    // Assign this to Play Button OnClick()
    public void StartInterview()
    {
        StartCoroutine(StartSessionCoroutine());
    }

    private IEnumerator StartSessionCoroutine()
    {
        string url = baseUrl.TrimEnd('/') + "/v1/interview/init";

        // 🔹 Create request body
        InitRequest requestData = new InitRequest
        {
            mode = "standard",
            difficulty = "normal"
        };

        string jsonBody = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        request.timeout = 30;

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to start interview: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);
            yield break;
        }

        string jsonResponse = request.downloadHandler.text;
        Debug.Log("Init Response: " + jsonResponse);

        InitResponse response = JsonUtility.FromJson<InitResponse>(jsonResponse);

        if (response != null && !string.IsNullOrEmpty(response.session_id))
        {
            CurrentSessionId = response.session_id;
            Debug.Log("New Session ID: " + CurrentSessionId);

            // Optional:
            // SceneManager.LoadScene("InterviewScene");
        }
        else
        {
            Debug.LogError("Session ID missing in response.");
        }
    }
}
