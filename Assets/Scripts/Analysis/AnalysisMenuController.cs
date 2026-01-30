using UnityEngine;
using UnityEngine.SceneManagement;

public class AnalysisMenuController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string interviewSceneName = "Interview";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    // ---------- BUTTON ACTIONS ----------

    public void SaveReport()
    {
        // Placeholder for real save logic later
        Debug.Log("[Analysis] Save Report clicked (not implemented yet)");
    }

    public void NewSession()
    {
        Debug.Log("[Analysis] Starting new interview session");
        SceneManager.LoadScene(interviewSceneName);
    }

    public void Close()
    {
        Debug.Log("[Analysis] Closing analysis and returning to Main Menu");
        SceneManager.LoadScene(mainMenuSceneName);
    }
}