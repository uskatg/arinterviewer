using Meta.WitAi;
using Meta.WitAi.Json;
using Oculus.Voice;
using UnityEngine;
using UnityEngine.Events;
using Meta.WitAi.TTS.Utilities;
using UnityEngine.InputSystem;

public class InterviewManager : MonoBehaviour
{
    [Header("Component Reference")]
    public AppVoiceExperience voiceExperience; 
    public GroqApiClient groqApiClient;
    public TTSSpeaker ttsSpeaker;

    [Header("UI Feedback")]
    public bool isListening = false;

    void Start()
    {
        groqApiClient.OnAIResponseReceived += SpeakResponse;
    }

    void Update()
    {
        // Press space to talk
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            StartListening();
        }
        else if (Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            StopListening();
        }
    }

    void SpeakResponse(string text)
    {
        if (ttsSpeaker != null)
        {
            Debug.Log("Generating speech...");
            ttsSpeaker.Speak(text);
        }
    }

    void StartListening()
    {
        if (voiceExperience == null) return;
        Debug.Log("Start recording...");
        isListening = true;
        voiceExperience.Activate();
    }

    void StopListening()
    {
        if (voiceExperience == null) return;
        Debug.Log("Stop recording, sending now...");
        isListening = false;
        voiceExperience.Deactivate();
    }


    // Callback method when voice turned to text
    public void OnWitResponse(WitResponseNode response)
    {
        string transcription = response["text"].Value;

        if (!string.IsNullOrEmpty(transcription))
        {
            Debug.Log($"<color=blue>Heard: {transcription}</color>");

            // Send to Groq
            groqApiClient.Chat(transcription);
        }
    }

}