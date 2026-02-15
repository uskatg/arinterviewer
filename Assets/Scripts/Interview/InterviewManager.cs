using Meta.WitAi;
using Meta.WitAi.Json;
using Meta.WitAi.TTS.Utilities;
using Oculus.Voice;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using System.Text.RegularExpressions;

public class InterviewManager : MonoBehaviour
{
    [Header("Configuration")]
    public string backendUrl = "http://192.168.2.107:8000/v1/interview";

    [Header("Component Reference")]
    public TTSSpeaker ttsSpeaker;
    public AudioAnalyzer audioAnalyzer;

    [Header("Runtime State")]
    public bool isListening = false;
    public string currentSessionId;

    [Header("Debug")]
    public bool startOnPlay = true;

    // TTS queue control
    private Queue<string> _speechQueue = new Queue<string>();
    private bool _isSpeaking = false;
    private bool _currentClipFinished = true;

    private void OnEnable()
    {
        if (audioAnalyzer == null)
        {
            audioAnalyzer = GetComponent<AudioAnalyzer>();
            if (audioAnalyzer == null) Debug.LogError("InterviewManager: WARNING! AudioAnalyzer component is missing.");
            else Debug.Log("InterviewManager: Auto-connected to AudioAnalyzer.");
        }

        // Subscribe to our new Custom STT event
        if (audioAnalyzer != null)
        {
            audioAnalyzer.OnTranscriptionComplete += OnFullTranscription;
        }

        // register TTS
        if (ttsSpeaker != null)
        {
            // when one audio finished, turn to true
            ttsSpeaker.Events.OnAudioClipPlaybackFinished.AddListener((msg) => {
                _currentClipFinished = true;
            });
        }

        if (startOnPlay)
        {
            StartCoroutine(InitSessionRoutine());
        }
    }

    private void OnDestroy()
    {
        if (audioAnalyzer != null)
        {
            audioAnalyzer.OnTranscriptionComplete -= OnFullTranscription;
        }

        if (ttsSpeaker != null)
        {
            ttsSpeaker.Events.OnAudioClipPlaybackFinished.RemoveAllListeners();
        }
    }

    private void Update()
    {
        bool isDown = false;
        bool isUp = false;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame) isDown = true;
            if (Keyboard.current.spaceKey.wasReleasedThisFrame) isUp = true;
        }

        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger) ||
            OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
        {
            isDown = true;
        }

        if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger) ||
            OVRInput.GetUp(OVRInput.Button.SecondaryIndexTrigger))
        {
            isUp = true;
        }

        if (isDown && !isListening)
        {
            ActivateVoice();
        }
        else if (isUp && isListening)
        {
            StopListening();
        }
    }

    void StopListening()
    {
        isListening = false;
        Debug.Log("Stop recording, sending to AudioAnalyzer...");
        if (audioAnalyzer != null)
        {
            audioAnalyzer.StopRecordingAndAnalyze(false);
        }
    }

    public void ActivateVoice()
    {
        if (_isSpeaking)
        {
            StopAllCoroutines();
            _speechQueue.Clear();
            ttsSpeaker.Stop();
            _isSpeaking = false;
        }

        isListening = true;
        Debug.Log("Mic Activated...");
        if (audioAnalyzer != null)
        {
            audioAnalyzer.StartRecording();
        }
    }

    // Now triggered manually by AudioAnalyzer instead of Wit.ai
    private void OnFullTranscription(string transcript, string voiceReport)
    {
        if (string.IsNullOrEmpty(currentSessionId)) return;
        Debug.Log($"User said: {transcript}");
        Debug.Log($"Voice Report: {voiceReport}");

        StartCoroutine(ReplyAndGetNextRoutine(transcript, voiceReport));
    }

    private IEnumerator InitSessionRoutine()
    {
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

            StartCoroutine(GetNextQuestionRoutine());
        });
    }

    private IEnumerator ReplyAndGetNextRoutine(string userText, string voiceReport)
    {
        InterviewReplyRequest replyReq = new InterviewReplyRequest
        {
            session_id = currentSessionId,
            user_text = userText,
            voice_data = voiceReport
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
        if (ttsSpeaker == null) return;

        // use regexp to split
        string[] sentences = Regex.Split(text, @"(?<=[.!?;])");

        foreach (var sentence in sentences)
        {
            if (!string.IsNullOrWhiteSpace(sentence))
            {
                _speechQueue.Enqueue(sentence.Trim());
            }
        }

        if (!_isSpeaking)
        {
            StartCoroutine(ProcessSpeechQueue());
        }
    }

    private IEnumerator ProcessSpeechQueue()
    {
        _isSpeaking = true;

        while (_speechQueue.Count > 0)
        {
            string phrase = _speechQueue.Dequeue();
            _currentClipFinished = false;

            ttsSpeaker.Speak(phrase);

            float timer = 0f;
            while (!_currentClipFinished && timer < 15f)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(0.1f);
        }

        _isSpeaking = false;
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
        public string voice_data;
    }

    [Serializable]
    public class InterviewReplyResponse
    {
        public string session_id;
        public string feedback;
    }
}