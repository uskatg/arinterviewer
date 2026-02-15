using UnityEngine;

public class SimpleInterviewerPlacer : MonoBehaviour
{
    [SerializeField] private Transform head;            // CenterEyeAnchor
    [SerializeField] private Transform interviewerRoot; // Avatar root
    [SerializeField] private float distance = 1.2f;
    [SerializeField] private float heightOffset = -0.2f;

    private void Start()
    {
        Place();
    }

    public void Place()
    {
        if (!head || !interviewerRoot) return;

        Vector3 pos = head.position + head.forward * distance;
        pos.y = head.position.y + heightOffset;
        interviewerRoot.position = pos;

        Vector3 look = head.position - interviewerRoot.position;
        look.y = 0f;
        if (look.sqrMagnitude > 0.001f)
            interviewerRoot.rotation = Quaternion.LookRotation(look);
    }
}