using UnityEngine;
using Oculus.Interaction;

public class UIFacePlayerTransformer : MonoBehaviour, ITransformer
{
    private IGrabbable _grabbable;
    private Vector3 _offset;

    public void Initialize(IGrabbable grabbable) => _grabbable = grabbable;

    public void BeginTransform()
    {
        // Calculate the initial distance from the grab point to the UI
        // This prevents the "snap" to your hand
        _offset = transform.position - _grabbable.GrabPoints[0].position;
    }

    public void UpdateTransform()
    {
        Pose grabPose = _grabbable.GrabPoints[0];

        // 1. Move the UI based on the ray's current position + original offset
        transform.position = grabPose.position + _offset;

        // 2. Make the UI face the camera
        Vector3 directionToPlayer = Camera.main.transform.position - transform.position;
        directionToPlayer.y = 0; // Keep the UI perfectly vertical
        if (directionToPlayer != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(-directionToPlayer);
        }
    }

    public void EndTransform() { }
}