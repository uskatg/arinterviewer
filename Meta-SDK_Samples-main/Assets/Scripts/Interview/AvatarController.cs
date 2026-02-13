using UnityEngine;

public class AvatarController : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public AudioSource ttsAudioSource;

    private const string IS_TALKING_PARAM = "IsTalking";

    private void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();

        if (ttsAudioSource == null)
        {
            ttsAudioSource = FindFirstObjectByType<AudioSource>();
        }
    }

    private void Update()
    {
        if (animator != null && ttsAudioSource != null)
        {
            bool isTalking = ttsAudioSource.isPlaying;
            animator.SetBool(IS_TALKING_PARAM, isTalking);
        }
    }
}