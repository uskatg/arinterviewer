using UnityEngine;

public class AnalysisMenuController : MonoBehaviour
{
    [Header("UI References")]
    // Drag the 'AnalysisMenu' GameObject from your Hierarchy into this slot
    [SerializeField] private GameObject analysisMenuRoot;

    // ---------- BUTTON ACTIONS ----------

    public void SaveReport()
    {
        Debug.Log("[Analysis] Save Report clicked");
        // Logic for saving data goes here
    }

    public void NewSession()
    {
        Debug.Log("[Analysis] Resetting interview and closing panel");
        
        // Add logic here to reset your interview variables (e.g., score = 0)
        
        Close();
    }

    public void Close()
    {
        if (analysisMenuRoot != null)
        {
            analysisMenuRoot.SetActive(false);
            Debug.Log("[Analysis] Menu Closed");
        }
    }

    public void Open()
    {
        if (analysisMenuRoot != null)
        {
            analysisMenuRoot.SetActive(true);
            Debug.Log("[Analysis] Menu Opened");
        }
    }
}