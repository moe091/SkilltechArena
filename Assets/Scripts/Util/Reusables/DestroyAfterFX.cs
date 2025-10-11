using UnityEngine;

public class DestroyAfterFX : MonoBehaviour
{
    [SerializeField] private float extraDelay = 0.0f;
    [SerializeField] private bool waitForAnim = true;
    [SerializeField] private bool waitForSound = true;

    void Start()
    {
        float animLen = 0f;

        if (waitForAnim)
        {
            var animator = GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                var clips = animator.runtimeAnimatorController.animationClips;
                if (clips != null && clips.Length > 0)
                    animLen = clips[0].length; // single one-shot state
            }
        }

        float audioLen = 0f;
        if (waitForSound)
        {
            var src = GetComponent<AudioSource>();
            if (src != null && src.clip != null)
                audioLen = src.clip.length;
        }

        float life = Mathf.Max(animLen, audioLen) + extraDelay;
        Destroy(gameObject, life > 0f ? life : 0.1f);
    }
}
