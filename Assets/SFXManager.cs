using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance;

    [Header("Sound Effects")]
    public AudioClip[] soundEffects;
    private AudioSource audioSource;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void PlaySFX(int index)
    {
        if (soundEffects == null || soundEffects.Length == 0)
        {
            Debug.LogWarning("No sound effects assigned in SFXManager.");
            return;
        }

        if (index < 0 || index >= soundEffects.Length)
        {
            Debug.LogWarning("SFX index out of range: " + index);
            return;
        }

        audioSource.PlayOneShot(soundEffects[index]);

    }
    
    public void PlayRandomSFX()
    {
        if (soundEffects == null || soundEffects.Length == 0)
        {
            Debug.LogWarning("No sound effects assigned in SFXManager.");
            return;
        }

        int randomIndex = Random.Range(0, soundEffects.Length);
        audioSource.PlayOneShot(soundEffects[randomIndex]);
    }

}
