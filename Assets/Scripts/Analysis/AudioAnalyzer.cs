using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using NWaves.FeatureExtractors;
using NWaves.FeatureExtractors.Options;

public class AudioAnalyzer : MonoBehaviour
{
    [Header("Configuration")]
    public bool showDebugLogs = true;
    [Tooltip("Enter your OpenAI API Key here (sk-...)")]
    public string openAIApiKey = "";

    [Header("Analysis Thresholds")]
    public float volumeThreshold = 0.02f;
    public float pitchVarianceThreshold = 5.0f;
    public float wpmFastThreshold = 160f;
    public float wpmSlowThreshold = 110f;

    // Used by InterviewManager (transcript, voiceReport)
    public Action<string, string> OnTranscriptionComplete;

    private PitchExtractor _pitchExtractor;

    // Calibration
    private bool _hasCalibrated;
    private float _baseVolume;
    private float _basePitchSpread;

    // Timing
    private float _recordingStartTime;
    private float _speakingDuration;

    // Live tracking
    private readonly List<float> _rmsHistory = new List<float>(128);
    private readonly List<float> _pitchHistory = new List<float>(128);
    private Coroutine _liveTrackingCo;
    private bool _isRecording;

    // Mic
    private string _micDevice;
    private AudioClip _clip;
    private const int MaxRecordSeconds = 240;
    private const int SampleRate = 16000;

    private void Awake()
    {
        _pitchExtractor = new PitchExtractor(new PitchOptions
        {
            SamplingRate = SampleRate,
            LowFrequency = 80,
            HighFrequency = 400
        });
    }

    public void StartRecording()
    {
        if (Microphone.devices == null || Microphone.devices.Length == 0)
        {
            Debug.LogError("AudioAnalyzer: No microphone found.");
            return;
        }

        _micDevice = Microphone.devices[0];
        _clip = Microphone.Start(_micDevice, false, MaxRecordSeconds, SampleRate);

        _recordingStartTime = Time.time;
        _isRecording = true;
        _rmsHistory.Clear();
        _pitchHistory.Clear();

        if (_liveTrackingCo != null)
        {
            StopCoroutine(_liveTrackingCo);
        }
        _liveTrackingCo = StartCoroutine(LiveTrackingRoutine());

        if (showDebugLogs)
        {
            Debug.Log("AudioAnalyzer: Recording started on " + _micDevice);
        }
    }

    public void StopRecordingAndAnalyze(bool isCalibrationRound = false)
    {
        if (!_isRecording)
            return;

        int micPos = Microphone.GetPosition(_micDevice);
        Microphone.End(_micDevice);

        _speakingDuration = Time.time - _recordingStartTime;
        _isRecording = false;

        if (_liveTrackingCo != null)
        {
            StopCoroutine(_liveTrackingCo);
            _liveTrackingCo = null;
        }

        if (showDebugLogs)
        {
            Debug.Log($"AudioAnalyzer: Stopped. Duration {_speakingDuration:F2}s (micPos={micPos}).");
        }

        // Trim clip to actual length
        if (micPos > 0 && _clip != null)
        {
            float[] buffer = new float[micPos];
            _clip.GetData(buffer, 0);

            var trimmed = AudioClip.Create("Speech", micPos, 1, SampleRate, false);
            trimmed.SetData(buffer, 0);
            _clip = trimmed;
        }

        if (_clip == null)
        {
            if (showDebugLogs) Debug.LogWarning("AudioAnalyzer: No clip to analyze.");
            OnTranscriptionComplete?.Invoke("[No audio]", "[Audio Analysis: N/A]");
            return;
        }

        // Fire and forget
        _ = ProcessWithWhisperAsync(_clip, isCalibrationRound);
    }

    private async Task ProcessWithWhisperAsync(AudioClip clip, bool isCalibrationRound)
    {
        if (string.IsNullOrEmpty(openAIApiKey))
        {
            Debug.LogError("AudioAnalyzer: Missing OpenAI API key.");
            return;
        }

        if (clip.length < 0.5f || _speakingDuration < 0.5f)
        {
            if (showDebugLogs)
                Debug.Log("AudioAnalyzer: Audio too short, skipping cloud transcription.");

            OnTranscriptionComplete?.Invoke("[Audio too short]", "[Audio Analysis: N/A]");
            return;
        }

        var wavBytes = EncodeToWAV(clip);
        string transcript = "";

        try
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", openAIApiKey);

                using (var content = new MultipartFormDataContent())
                {
                    var audioContent = new ByteArrayContent(wavBytes);
                    audioContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");
                    content.Add(audioContent, "file", "audio.wav");
                    content.Add(new StringContent("gpt-4o-mini-transcribe"), "model");
                    content.Add(new StringContent("en"), "language");

                    var response = await client.PostAsync("https://api.openai.com/v1/audio/transcriptions", content);
                    var responseText = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var parsed = JsonUtility.FromJson<WhisperResponse>(responseText);
                        transcript = parsed != null ? parsed.text : "";
                    }
                    else
                    {
                        Debug.LogError("Whisper HTTP error: " + response.StatusCode + "\n" + responseText);
                        transcript = "[STT Error]";
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Whisper exception: " + e.Message);
            transcript = "[STT Error]";
        }

        // Word count / WPM
        int wordCount = 0;
        if (!string.IsNullOrEmpty(transcript) && transcript != "[STT Error]")
        {
            // basic split, avoid LINQ
            var parts = transcript.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            wordCount = parts.Length;
        }

        float wpm = 0f;
        if (_speakingDuration > 0.1f && wordCount > 0)
        {
            wpm = (wordCount / _speakingDuration) * 60f;
        }

        // Aggregate live data
        float avgVolume = ComputeAverage(_rmsHistory);
        float avgPitch = ComputeAverage(_pitchHistory);
        float pitchSpread = ComputeStdDev(_pitchHistory, avgPitch);

        string voiceReport;

        if (isCalibrationRound)
        {
            _baseVolume = avgVolume;
            _basePitchSpread = pitchSpread;
            _hasCalibrated = true;
            voiceReport = $"Calibration done. Baseline volume={avgVolume:F3}, pitch spread={pitchSpread:F1}";
        }
        else if (_hasCalibrated)
        {
            float volDelta = avgVolume - _baseVolume;

            string volLabel;
            if (volDelta > volumeThreshold) volLabel = "Loud/Confident";
            else if (volDelta < -volumeThreshold) volLabel = "Quiet/Shy";
            else volLabel = "Normal Volume";

            string toneLabel = pitchSpread > _basePitchSpread + pitchVarianceThreshold ? "Expressive" : "Monotone";

            string paceLabel;
            if (wpm > wpmFastThreshold) paceLabel = "Fast";
            else if (wpm < wpmSlowThreshold) paceLabel = "Slow";
            else paceLabel = "Good Pace";

            voiceReport =
                $"[Audio Analysis: Volume: {volLabel} | Tone: {toneLabel} | Pace: {wpm:F0} WPM ({paceLabel})]";
        }
        else
        {
            voiceReport =
                $"[Audio Analysis (Uncalibrated): Volume={avgVolume:F3}, PitchSpread={pitchSpread:F1}, WPM={wpm:F0}]";
        }

        OnTranscriptionComplete?.Invoke(transcript, voiceReport);
    }

    private IEnumerator LiveTrackingRoutine()
    {
        // Simple 300 ms polling of the latest chunk
        var wait = new WaitForSeconds(0.3f);

        while (_isRecording)
        {
            yield return wait;

            int micPos = Microphone.GetPosition(_micDevice);
            if (micPos <= 0 || _clip == null)
                continue;

            float[] samples = new float[micPos];
            _clip.GetData(samples, 0);

            int chunkSize = Mathf.Min(micPos, SampleRate / 3); // ~0.33s
            if (chunkSize <= 0 || micPos < chunkSize)
                continue;

            var chunk = new float[chunkSize];
            Array.Copy(samples, micPos - chunkSize, chunk, 0, chunkSize);

            // RMS (downsample a bit)
            double sum = 0;
            int count = 0;
            for (int i = 0; i < chunk.Length; i += 4)
            {
                float v = chunk[i];
                sum += v * v;
                count++;
            }

            if (count > 0)
            {
                float rms = Mathf.Sqrt((float)(sum / count));
                if (rms > 0.001f)
                {
                    _rmsHistory.Add(rms);
                }
            }

            // Pitch
            var frames = _pitchExtractor.ComputeFrom(chunk);
            foreach (var frame in frames)
            {
                if (frame != null && frame.Length > 0 && frame[0] > 0f)
                {
                    _pitchHistory.Add(frame[0]);
                }
            }
        }
    }

    private static float ComputeAverage(List<float> values)
    {
        if (values == null || values.Count == 0)
            return 0f;

        float sum = 0f;
        for (int i = 0; i < values.Count; i++)
        {
            sum += values[i];
        }

        return sum / values.Count;
    }

    private static float ComputeStdDev(List<float> values, float mean)
    {
        if (values == null || values.Count == 0)
            return 0f;

        float sumSq = 0f;
        for (int i = 0; i < values.Count; i++)
        {
            float d = values[i] - mean;
            sumSq += d * d;
        }

        return Mathf.Sqrt(sumSq / values.Count);
    }

    // Bare-bones WAV encoding so Whisper accepts it
    private byte[] EncodeToWAV(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        short[] intData = new short[samples.Length];
        byte[] bytesData = new byte[samples.Length * 2];

        const float rescaleFactor = 32767f;

        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * rescaleFactor);
            var byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        using (var memoryStream = new System.IO.MemoryStream())
        using (var writer = new System.IO.BinaryWriter(memoryStream))
        {
            // RIFF header
            writer.Write(Encoding.UTF8.GetBytes("RIFF"));
            writer.Write(36 + bytesData.Length);
            writer.Write(Encoding.UTF8.GetBytes("WAVE"));

            // fmt chunk
            writer.Write(Encoding.UTF8.GetBytes("fmt "));
            writer.Write(16); // Subchunk1Size
            writer.Write((short)1); // PCM
            writer.Write((short)clip.channels);
            writer.Write(clip.frequency);
            writer.Write(clip.frequency * clip.channels * 2);
            writer.Write((short)(clip.channels * 2));
            writer.Write((short)16); // bits per sample

            // data chunk
            writer.Write(Encoding.UTF8.GetBytes("data"));
            writer.Write(bytesData.Length);
            writer.Write(bytesData);

            writer.Flush();
            return memoryStream.ToArray();
        }
    }

    [Serializable]
    private class WhisperResponse
    {
        public string text;
    }
}