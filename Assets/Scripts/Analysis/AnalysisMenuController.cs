using UnityEngine;

public class AnalysisMenuController : MonoBehaviour
{
    [Header("Scene Objects")]
    [SerializeField] private GameObject menuEmpty;
    [SerializeField] private GameObject interviewEmpty;
    [SerializeField] private GameObject analysisEmpty;

    // ---------- BUTTON ACTIONS ----------

    public void SaveReport()
    {
        // Placeholder for real save logic later
        Debug.Log("[Analysis] Save Report clicked (not implemented yet)");
    }

    // NEW SESSION → Analysis → Interview
    public void NewSession()
    {
        Debug.Log("[Analysis] Starting new interview session");

        if (analysisEmpty != null)
            analysisEmpty.SetActive(false);

        if (interviewEmpty != null)
            interviewEmpty.SetActive(true);
    }

    // CLOSE → Analysis → Menu
    public void Close()
    {
        Debug.Log("[Analysis] Closing analysis and returning to Menu");

        if (analysisEmpty != null)
            analysisEmpty.SetActive(false);

        if (menuEmpty != null)
            menuEmpty.SetActive(true);
    }
}