using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Source")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Clips sonores")]
    [SerializeField] private AudioClip binPlacedClip;
    [SerializeField] private AudioClip wastePickupClip;
    [SerializeField] private AudioClip wasteDropClip;
    [SerializeField] private AudioClip correctTrashClip;
    [SerializeField] private AudioClip wrongTrashClip;
    [SerializeField] private AudioClip waveStartClip;
    [SerializeField] private AudioClip waveCompleteClip;
    [SerializeField] private AudioClip gameOverClip;
    [SerializeField] private AudioClip timerTickClip;
    [SerializeField] private AudioClip binHighlightClip;

    [Header("Volume")]
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 0.8f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }
    }

    private void Play(AudioClip clip, float volumeMult = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume * volumeMult);
    }

    public void PlayBinPlaced() => Play(binPlacedClip);
    public void PlayWastePickup() => Play(wastePickupClip);
    public void PlayWasteDrop() => Play(wasteDropClip);
    public void PlayCorrect() => Play(correctTrashClip);
    public void PlayWrong() => Play(wrongTrashClip);
    public void PlayWaveStart() => Play(waveStartClip);
    public void PlayWaveComplete() => Play(waveCompleteClip);
    public void PlayGameOver() => Play(gameOverClip);
    public void PlayTick() => Play(timerTickClip, 0.6f);
    public void PlayBinHighlight() => Play(binHighlightClip, 0.5f);
}