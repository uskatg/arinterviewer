using UnityEngine;
public class SimpleLoader : MonoBehaviour
{
    void Start()
    {
        // Force load immediately without waiting for UI or events
        GetComponent<OVRSceneManager>().LoadSceneModel();
    }
}