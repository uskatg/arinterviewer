using UnityEngine;

public class GameStartManager : MonoBehaviour
{
    [Header("Drag Objects Here")]
    public GameObject menuObject;
    public GameObject interviewObject;
    public GameObject analysisObject;

    void Start()
    {
        // 1. Show the Menu
        if (menuObject != null)
        {
            menuObject.SetActive(true);
        }

        // 2. Hide the Interview
        if (interviewObject != null)
        {
            interviewObject.SetActive(false);
        }

        // 3. Hide the Analysis
        if (analysisObject != null)
        {
            analysisObject.SetActive(false);
        }
    }
}