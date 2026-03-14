using UnityEngine;

/// <summary>
/// Provides a singleton manager for playing button click sounds.
/// </summary>
public class ButtonSoundManager : MonoBehaviour
{
    // Fields
    public static ButtonSoundManager Instance { get; private set; }

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickClip;

    // Methods
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (audioSource == null)
            Debug.LogError("[ButtonSoundManager] AudioSource is not assigned.");

        if (clickClip == null)
            Debug.LogError("[ButtonSoundManager] Click clip is not assigned.");
    }

    public void PlayClick()
    {
        if (audioSource == null || clickClip == null)
            return;

        audioSource.PlayOneShot(clickClip);
    }
}