using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using NWaves.FeatureExtractors;
using NWaves.FeatureExtractors.Options;

public class AudioAnalyzer : MonoBehaviour
{
    public bool IsCalibrated { get; private set; }
    public float BaselineVolume { get; private set; } = 0.05f;
    public float BaselinePitchSpread { get; private set; } = 20.0f;
    public float BaselineWPM { get; private set; } = 130f;
    public float LastVolume { get; private set; }
    public float LastPitch { get; private set; }
    public float LastWPM { get; private set; }

    public Action<string, string> OnTranscriptionComplete;
    public Action<bool, string> OnCalibrationComplete;

    private string _apiKey;
    private PitchExtractor _pitchExtractor;
    private float _recordingStartTime;
    private float _speakingDuration;
    private readonly List<float> _rmsHistory = new List<float>();
    private readonly List<float> _pitchHistory = new List<float>();
    private Coroutine _liveTrackingCo;
    private bool _isRecording;
    private string _micDevice;
    private AudioClip _clip;
    private const int SampleRate = 16000;

    private void Awake()
    {
        _pitchExtractor = new PitchExtractor(new PitchOptions { SamplingRate = SampleRate, LowFrequency = 80, HighFrequency = 400 });
        LoadAPIKey();
    }

    private void LoadAPIKey()
    {
        TextAsset keyFile = Resources.Load<TextAsset>("api-key");
        if (keyFile != null) _apiKey = keyFile.text.Trim();
    }

    public void StartRecording()
    {
        if (Microphone.devices.Length == 0) return;

        _micDevice = Microphone.devices[0];
        _clip = Microphone.Start(_micDevice, false, 300, SampleRate);
        _recordingStartTime = Time.time;
        _isRecording = true;

        _rmsHistory.Clear();
        _pitchHistory.Clear();

        if (_liveTrackingCo != null) StopCoroutine(_liveTrackingCo);
        _liveTrackingCo = StartCoroutine(LiveTrackingRoutine());
    }

    public void StopRecordingAndAnalyze(bool isCalibration = false)
    {
        if (!_isRecording) return;

        int micPos = Microphone.GetPosition(_micDevice);
        Microphone.End(_micDevice);
        _isRecording = false;
        _speakingDuration = Time.time - _recordingStartTime;

        if (_liveTrackingCo != null) StopCoroutine(_liveTrackingCo);

        if (_clip == null || _speakingDuration < 0.5f)
        {
            if (isCalibration) OnCalibrationComplete?.Invoke(false, "Audio too short.");
            else OnTranscriptionComplete?.Invoke("", "[Audio too short]");
            return;
        }

        StartCoroutine(ProcessWithWhisperRoutine(_clip, isCalibration, micPos));
    }

    private IEnumerator ProcessWithWhisperRoutine(AudioClip clip, bool isCalibration, int micPos)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            Debug.LogError("[AudioAnalyzer] API Key is missing.");
            if (isCalibration) OnCalibrationComplete?.Invoke(false, "API Key Missing");
            yield break;
        }

        byte[] wavBytes = EncodeToWAV(clip, micPos);

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavBytes, "audio.wav", "audio/wav");
        form.AddField("model", "whisper-1");
        form.AddField("language", "en");

        using (UnityWebRequest req = UnityWebRequest.Post("https://api.openai.com/v1/audio/transcriptions", form))
        {
            req.SetRequestHeader("Authorization", "Bearer " + _apiKey);

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string transcript = "";
                try
                {
                    var parsed = JsonUtility.FromJson<WhisperResponse>(req.downloadHandler.text);
                    transcript = parsed?.text ?? "";
                }
                catch { /* Ignored */ }

                float avgVolume = ComputeAverage(_rmsHistory);
                float avgPitch = ComputeAverage(_pitchHistory);
                float pitchSpread = ComputeStdDev(_pitchHistory, avgPitch);

                int wordCount = transcript.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
                float wpm = (_speakingDuration > 0) ? (wordCount / _speakingDuration) * 60f : 0f;

                LastVolume = avgVolume;
                LastPitch = avgPitch; // Using Average Pitch directly
                LastWPM = wpm;

                if (isCalibration)
                {
                    BaselineVolume = avgVolume;
                    BaselinePitchSpread = pitchSpread;
                    BaselineWPM = wpm;
                    IsCalibrated = true;

                    string msg = $"Volume: {avgVolume:F3}\nPitch: {pitchSpread:F1}\nSpeed: {wpm:F0} WPM";
                    OnCalibrationComplete?.Invoke(true, msg);
                }
                else
                {
                    // Kept compatibility, but InterviewManager ignores it
                    string voiceReport = GenerateVoiceReport(avgVolume, pitchSpread, wpm);
                    OnTranscriptionComplete?.Invoke(transcript, voiceReport);
                }
            }
            else
            {
                string err = $"API Error: {req.responseCode}";
                if (isCalibration) OnCalibrationComplete?.Invoke(false, err);
                else OnTranscriptionComplete?.Invoke("[Connection Error]", "");
            }
        }
    }

    private string GenerateVoiceReport(float vol, float pitchDev, float wpm)
    {
        float baseVol = IsCalibrated ? BaselineVolume : 0.05f;
        float baseWpm = IsCalibrated ? BaselineWPM : 130f;

        string volLabel = (vol > baseVol + 0.02f) ? "Loud" : (vol < baseVol - 0.02f) ? "Quiet" : "Normal";
        string wpmLabel = (wpm > baseWpm + 20f) ? "Fast" : (wpm < baseWpm - 20f) ? "Slow" : "Good";

        return $"[Audio: {volLabel} | Speed {wpmLabel} ({wpm:F0})]";
    }

    private IEnumerator LiveTrackingRoutine()
    {
        var wait = new WaitForSeconds(0.2f);
        while (_isRecording)
        {
            yield return wait;
            if (_clip == null) continue;
            int micPos = Microphone.GetPosition(_micDevice);
            if (micPos < SampleRate / 4) continue;

            float[] chunk = new float[SampleRate / 4];
            _clip.GetData(chunk, micPos - (SampleRate / 4));

            float sum = 0;
            for (int i = 0; i < chunk.Length; i += 10) sum += chunk[i] * chunk[i];
            float rms = Mathf.Sqrt(sum / (chunk.Length / 10));
            if (rms > 0.001f) _rmsHistory.Add(rms);

            if (_pitchHistory.Count < 2048)
            {
                var frames = _pitchExtractor.ComputeFrom(chunk);
                foreach (var f in frames) if (f.Length > 0 && f[0] > 0) _pitchHistory.Add(f[0]);
            }
        }
    }

    private byte[] EncodeToWAV(AudioClip clip, int micPos)
    {
        if (micPos <= 0) micPos = clip.samples;
        float[] samples = new float[micPos * clip.channels];
        clip.GetData(samples, 0);

        short[] intData = new short[samples.Length];
        byte[] bytesData = new byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * 32767f);
            System.BitConverter.GetBytes(intData[i]).CopyTo(bytesData, i * 2);
        }

        using (var ms = new System.IO.MemoryStream())
        using (var writer = new System.IO.BinaryWriter(ms))
        {
            writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
            writer.Write(36 + bytesData.Length);
            writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVEfmt "));
            writer.Write(16); writer.Write((short)1); writer.Write((short)clip.channels);
            writer.Write(16000); writer.Write(16000 * clip.channels * 2);
            writer.Write((short)(clip.channels * 2)); writer.Write((short)16);
            writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
            writer.Write(bytesData.Length); writer.Write(bytesData);
            return ms.ToArray();
        }
    }
    private static float ComputeAverage(List<float> list) { if (list.Count == 0) return 0; float s = 0; foreach (var v in list) s += v; return s / list.Count; }
    private static float ComputeStdDev(List<float> list, float avg) { if (list.Count == 0) return 0; float s = 0; foreach (var v in list) s += (v - avg) * (v - avg); return Mathf.Sqrt(s / list.Count); }
    [Serializable] class WhisperResponse { public string text; }
}