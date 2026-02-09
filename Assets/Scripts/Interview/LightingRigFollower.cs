using UnityEngine;

public class LightingRigFollower : MonoBehaviour
{
    public Transform head;                 // CenterEyeAnchor
    public Vector3 offset = new Vector3(0f, 0.6f, 0.6f);  // relative to avatar
    public Transform avatarRoot;           // your avatar root

    [Range(0f, 1f)] public float follow = 0.1f;          // smoothing
    public bool rotateWithHeadYaw = true;

    void LateUpdate()
    {
        Debug.Log("LightingRigFollower running");
        if (!head || !avatarRoot) return;

        // position rig relative to avatar
        Vector3 targetPos = avatarRoot.TransformPoint(offset);
        transform.position = Vector3.Lerp(transform.position, targetPos, 1f - Mathf.Pow(1f - follow, Time.deltaTime * 60f));

        if (rotateWithHeadYaw)
        {
            Vector3 fwd = head.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(fwd.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Pow(1f - follow, Time.deltaTime * 60f));
            }
        }
    }
}