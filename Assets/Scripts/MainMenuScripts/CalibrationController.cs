using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CalibrationController : MonoBehaviour
{
    [Header("References")]
    public AudioAnalyzer audioAnalyzer;
    public Button calibrateButton;
    public Button stopButton;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI promptText;

    [Header("Settings")]
    [TextArea] public string calibrationSentence = "The quick brown fox jumps over the lazy dog";

    private void Start()
    {
        ResetUI();

        calibrateButton.onClick.RemoveAllListeners();
        stopButton.onClick.RemoveAllListeners();
        calibrateButton.onClick.AddListener(StartCalibration);
        stopButton.onClick.AddListener(StopCalibration);
    }

    void ResetUI()
    {
        calibrateButton.gameObject.SetActive(true);
        stopButton.gameObject.SetActive(false);
        stopButton.interactable = true;
        promptText.text = calibrationSentence;
        titleText.text = "Press Calibrate when ready";
    }

    void StartCalibration()
    {
        calibrateButton.gameObject.SetActive(false);
        stopButton.gameObject.SetActive(true);
        titleText.text = "Recording...";
        promptText.text = calibrationSentence; // In case stat page is still there
        audioAnalyzer.StartRecording();
    }

    void StopCalibration()
    {
        stopButton.interactable = false;
        titleText.text = "Processing...";
        audioAnalyzer.OnCalibrationComplete += OnCalibrationResult;
        audioAnalyzer.StopRecordingAndAnalyze(true);
    }

    void OnCalibrationResult(bool success, string message)
    {
        audioAnalyzer.OnCalibrationComplete -= OnCalibrationResult;
        stopButton.interactable = true;
        stopButton.gameObject.SetActive(false);
        calibrateButton.gameObject.SetActive(true);

        if (success)
        {
            titleText.text = "Stats Calibrated";
            promptText.text = $"{message}";

        }
        else
        {
            titleText.text = "Calibration Failed";
            promptText.text = message;
            StartCoroutine(ResetDelay(10.0f));
        }
    }

    private System.Collections.IEnumerator ResetDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ResetUI();
    }
}