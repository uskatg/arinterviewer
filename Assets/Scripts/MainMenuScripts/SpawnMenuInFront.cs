using UnityEngine;

public class SpawnMenuInFront : MonoBehaviour
{
    public Transform head;              // assign CenterEye / Main Camera here
    public float distance = 1.2f;        // meters in front
    public float heightOffset = -0.05f;  // small offset if needed
    public bool facePlayer = true;

    void Start()
    {
        if (!head) head = Camera.main ? Camera.main.transform : null;
        if (!head) return;

        // place in front of head (but world-locked)
        Vector3 forwardFlat = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
        if (forwardFlat.sqrMagnitude < 0.0001f) forwardFlat = head.forward;

        transform.position = head.position + forwardFlat * distance + Vector3.up * heightOffset;

        if (facePlayer)
        {
            Vector3 lookDir = transform.position - head.position;
            lookDir.y = 0f; // keep upright
            if (lookDir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }
}