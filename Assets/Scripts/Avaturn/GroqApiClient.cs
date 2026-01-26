using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.Net;

public class GroqApiClient : MonoBehaviour
{
    [Header("Groq Settings")]
    [Tooltip("Input your Groq API Key")]
    public string apiKey = "";

    private string modelName = "llama-3.1-8b-instant";
    private string apiUrl = "https://api.groq.com/openai/v1/chat/completions";

    public System.Action<string> OnAIResponseReceived;

    //void Awake()
    //{
    //    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
    //    ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
    //}

    //public class BypassCertificateHandler : CertificateHandler
    //{
    //    protected override bool ValidateCertificate(byte[] certificateData)
    //    {
    //        return true;
    //    }
    //}

    void Start()
    {
        StartCoroutine(SendRequestToGroq("Hello, I'm coming for interview. Please introduce your self."));
    }

    public void CancelPendingRequests()
    {
        StopAllCoroutines();
        Debug.Log("AI Thinking cancelled.");
    }

    public void Chat(string userMessage)
    {
        StartCoroutine(SendRequestToGroq(userMessage));
    }

    IEnumerator SendRequestToGroq(string userMessage)
    {
        Debug.Log("Thinking: " + userMessage + " ...");

        string jsonBody = $@"
        {{
            ""messages"": [
                {{
                    ""role"": ""user"",
                    ""content"": ""You are a professional interviewer. Keep your responses concise and professional.""
                }},
                {{
                    ""role"": ""user"",
                    ""content"": ""{EscapeJson(userMessage)}""
                }}
            ],
            ""model"": ""{modelName}"",
            ""temperature"": 0.7
        }}";

        // Create HTTP Request
        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            //request.certificateHandler = new BypassCertificateHandler();
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                ParseAndLogResponse(responseText);
            }
            else
            {
                Debug.LogError($"Groq API Error: {request.error}\n{request.downloadHandler.text}");
            }
        }
    }

    // Parse JSON
    void ParseAndLogResponse(string json)
    {
        var responseObj = JsonUtility.FromJson<GroqResponse>(json);

        if (responseObj != null && responseObj.choices != null && responseObj.choices.Length > 0)
        {
            string aiContent = responseObj.choices[0].message.content;
            Debug.Log("<color=green>Interviewer: </color>" + aiContent);

            OnAIResponseReceived?.Invoke(aiContent);
        }
    }

    // Process Special Chars in JSON
    string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n");
    }


    [System.Serializable]
    public class GroqResponse
    {
        public Choice[] choices;
    }

    [System.Serializable]
    public class Choice
    {
        public Message message;
    }

    [System.Serializable]
    public class Message
    {
        public string content;
        public string role;
    }
}