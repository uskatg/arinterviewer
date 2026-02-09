using Meta.WitAi;
using Meta.WitAi.Json;
using Meta.WitAi.TTS.Utilities;
using Oculus.Voice;
using System;
using System.Collections;
using System.Collections.Generic;
//using System.Diagnostics;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;

public class InterviewManager : MonoBehaviour
{
    [SerializeField] private GameObject loadingOverlay;
    [Header("Configuration")]
    public string backendUrl = "http://127.0.0.1:8000/v1/interview";

    [Header("Component Reference")]
    public AppVoiceExperience voiceExperience;
    public TTSSpeaker ttsSpeaker;

    [Header("Runtime State")]
    public bool isListening = false;
    public string currentSessionId;

    [Header("Debug")]
    public bool startOnPlay = true;

    private void Start()
    {
        if (voiceExperience != null)
        {
            voiceExperience.VoiceEvents.OnFullTranscription.AddListener(OnFullTranscription);
            voiceExperience.VoiceEvents.OnStartListening.AddListener(() => isListening = true);
            voiceExperience.VoiceEvents.OnStoppedListening.AddListener(() => isListening = false);
        }

        if (startOnPlay)
        {
            StartCoroutine(InitSessionRoutine());
        }
    }

    private void OnDestroy()
    {
        if (voiceExperience != null)
        {
            voiceExperience.VoiceEvents.OnFullTranscription.RemoveListener(OnFullTranscription);
        }
    }

    private void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame && !isListening)
        {
            ActivateVoice();
        }
        else if (Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            StopListening();
        }
    }

    void StopListening()
    {
        if (voiceExperience == null) return;
        Debug.Log("Stop recording, sending now...");
        isListening = false;
        voiceExperience.Deactivate();
    }

    public void ActivateVoice()
    {
        if (voiceExperience != null)
        {
            Debug.Log("Mic Activated...");
            voiceExperience.Activate();
        }
    }

    private void OnFullTranscription(string transcript)
    {
        if (string.IsNullOrEmpty(currentSessionId)) return;
        Debug.Log($"User said: {transcript}");
        StartCoroutine(ReplyAndGetNextRoutine(transcript));
    }

    private void ClearChatUI() 
    {
        // Logic to destroy old chat bubbles or clear TextMeshPro fields
        Debug.Log("Clearing old chat UI elements and console logs for new session.");

    }
    public void OnStartNewInterviewClicked()
    {
        // 1. Stop everything currently happening
        StopAllCoroutines(); // Stops any pending API calls
        if (ttsSpeaker != null) ttsSpeaker.Stop(); // Stop the AI from talking
        if (voiceExperience != null) voiceExperience.Deactivate(); // Stop the mic

        // 2. Visual Feedback
        if (loadingOverlay != null) loadingOverlay.SetActive(true);
        ClearChatUI();

        // 3. Reuse your existing Routine
        StartCoroutine(RestartFlow());
    }

    private IEnumerator RestartFlow()
    {
        // Reuse your existing initialization logic
        yield return StartCoroutine(InitSessionRoutine());

        // Hide the overlay once the new session is ready
        if (loadingOverlay != null) loadingOverlay.SetActive(false);
    }
    private IEnumerator InitSessionRoutine()
    {
        if (loadingOverlay != null) loadingOverlay.SetActive(true);
        InterviewInitRequest req = new InterviewInitRequest
        {
            job_position = "Software Developer",
            interviewer_mode = "social",
            job_description = "Looking for a C# expert with VR experience.",
            cv_data = GetDummyCV()
        };

        string json = JsonUtility.ToJson(req);
        // Debug.Log($"Sending Init JSON: {json}");

        yield return PostRequest("/init", json, (response) =>
        {
            var resObj = JsonUtility.FromJson<InterviewInitResponse>(response);
            currentSessionId = resObj.session_id;
            Debug.Log($"Session Init Success: {resObj.message} (ID: {currentSessionId})");

            // 2. Hide the overlay only AFTER the request succeeds
            if (loadingOverlay != null) loadingOverlay.SetActive(false);
            StartCoroutine(GetNextQuestionRoutine());
        });
    }

    private IEnumerator ReplyAndGetNextRoutine(string userText)
    {
        InterviewReplyRequest replyReq = new InterviewReplyRequest
        {
            session_id = currentSessionId,
            user_text = userText
        };

        yield return PostRequest("/reply", JsonUtility.ToJson(replyReq), (response) =>
        {
            var resObj = JsonUtility.FromJson<InterviewReplyResponse>(response);
            Debug.Log($"AI Feedback: {resObj.feedback}");
        });

        yield return GetNextQuestionRoutine();
    }

    private IEnumerator GetNextQuestionRoutine()
    {
        InterviewNextRequest nextReq = new InterviewNextRequest
        {
            session_id = currentSessionId
        };

        yield return PostRequest("/next", JsonUtility.ToJson(nextReq), (response) =>
        {
            var resObj = JsonUtility.FromJson<InterviewNextResponse>(response);
            Debug.Log($"AI Question ({resObj.message_type}): {resObj.interviewer_text}");

            Speak(resObj.interviewer_text);
        });
    }

    private void Speak(string text)
    {
        if (ttsSpeaker != null) ttsSpeaker.Speak(text);
    }

    private IEnumerator PostRequest(string endpoint, string jsonBody, Action<string> onSuccess)
    {
        using (UnityWebRequest request = new UnityWebRequest(backendUrl + endpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"API Error {endpoint}: {request.error}\nResponse: {request.downloadHandler.text}");
            }
            else
            {
                onSuccess?.Invoke(request.downloadHandler.text);
            }
        }
    }

    private CVData GetDummyCV()
    {
        return new CVData
        {
            name = "John Doe",
            email = "john@example.com",
            phone = "123456789",
            job_title = "Junior Developer",
            skills = new List<string> { "C#", "Unity", "Python", "FastAPI" },
            education = new List<Education>
            {
                new Education { school = "Tech University", degree = "B.Sc Computer Science", start = "2018", end = "2022" }
            },
            experience = new List<Experience>
            {
                new Experience
                {
                    company = "Game Corp",
                    title = "Intern",
                    start = "2021",
                    end = "2022",
                    bullets = new List<string> { "Fixed bugs", "Implemented UI" }
                }
            },
            projects = new List<Project>
            {
                new Project
                {
                    name = "VR Interview Sim",
                    tech = new List<string> { "Unity", "Meta SDK" },
                    bullets = new List<string> { "Created voice interaction", "Integrated LLM" }
                }
            }
        };
    }

    [Serializable]
    public class Education
    {
        public string degree;
        public string school;
        public string start;
        public string end;
    }

    [Serializable]
    public class Experience
    {
        public string title;
        public string company;
        public string start;
        public string end;
        public List<string> bullets;
    }

    [Serializable]
    public class Project
    {
        public string name;
        public List<string> tech;
        public List<string> bullets;
    }

    [Serializable]
    public class CVData
    {
        public string job_title;
        public string name;
        public string email;
        public string phone;
        public List<Education> education;
        public List<Experience> experience;
        public List<Project> projects;
        public List<string> skills;
    }

    [Serializable]
    public class InterviewInitRequest
    {
        public CVData cv_data;
        public string job_position;
        public string job_description; // Optional in python, nullable string here is fine
        public string interviewer_mode;
    }

    [Serializable]
    public class InterviewInitResponse
    {
        public string session_id;
        public string message;
    }

    [Serializable]
    public class InterviewNextRequest
    {
        public string session_id;
    }

    [Serializable]
    public class InterviewNextResponse
    {
        public string session_id;
        public string interviewer_text;
        public string message_type; // "question" etc.
    }

    [Serializable]
    public class InterviewReplyRequest
    {
        public string session_id;
        public string user_text;
    }

    [Serializable]
    public class InterviewReplyResponse
    {
        public string session_id;
        public string feedback;
    }
}