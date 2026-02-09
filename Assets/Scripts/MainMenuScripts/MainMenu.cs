using UnityEngine;

public class MenuScript : MonoBehaviour
{
    [Header("Scene Objects")]
    [SerializeField] private GameObject menuEmpty;
    [SerializeField] private GameObject interviewEmpty;

    [Header("UI Objects")]
    [SerializeField] private GameObject quickToggle;

    public void PlayGame()
    {
        // Hide menu
        if (menuEmpty != null)
            menuEmpty.SetActive(false);

        // Show interview
        if (interviewEmpty != null)
            interviewEmpty.SetActive(true);

        // ALWAYS restore QuickToggle when entering Interview
        if (quickToggle != null)
            quickToggle.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}