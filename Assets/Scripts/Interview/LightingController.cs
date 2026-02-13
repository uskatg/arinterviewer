using UnityEngine;

public class ToonLightBinder : MonoBehaviour
{
    public Light mainDirectional;
    public Renderer[] renderers; // or leave empty and FindObjectsOfType<Renderer>()

    static readonly int ToonLightDir = Shader.PropertyToID("_ToonLightDir");

    void LateUpdate()
    {
        if (!mainDirectional) return;

        // Directional light points *from* the light toward the scene
        Vector3 dir = -mainDirectional.transform.forward; 
        Shader.SetGlobalVector(ToonLightDir, dir);
    }
}