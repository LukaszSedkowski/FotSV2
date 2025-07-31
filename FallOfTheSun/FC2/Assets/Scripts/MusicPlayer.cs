using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicPlayer : MonoBehaviour
{
    public static MusicPlayer Instance { get; private set; }
    public AudioClip musicClip;

    private AudioSource _audioSource;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _audioSource = GetComponent<AudioSource>();
        _audioSource.clip = musicClip;
        _audioSource.loop = true;
        _audioSource.playOnAwake = false;    // wy³¹cz „Play On Awake”
        _audioSource.volume = 1f;
        _audioSource.Play();                 // uruchom w Awake
    }

    /// <summary>
    /// Ustawia g³oœnoœæ od 0 (wyciszone) do 1 (maksymalnie g³oœno)
    /// </summary>
    public void SetVolume(float vol)
    {
        if (_audioSource != null)
            _audioSource.volume = Mathf.Clamp01(vol);
    }
}
