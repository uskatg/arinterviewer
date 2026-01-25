using UnityEngine;

public class TalkingController : MonoBehaviour
{
    public Animator animator;      // Drag your model here
    public AudioSource audioSource; // Drag the speaker here

    void Update()
    {
        // Check if the AI is currently playing audio
        if (audioSource.isPlaying)
        {
            // Turn ON the Blender animation
            animator.SetBool("IsTalking", true);
        }
        else
        {
            // Turn OFF (Go back to Idle)
            animator.SetBool("IsTalking", false);
        }
    }
}