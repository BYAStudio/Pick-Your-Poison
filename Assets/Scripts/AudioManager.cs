using UnityEngine;

/// <summary>
/// Faz 3: Oyundaki tum ses efektlerini tek bir merkezden yonetir.
/// Singleton yapisi: her sahnede yalnizca bir instances bulunur.
/// Kullanim: AudioManager.Instance.PlaySFX(AudioManager.SFX.CardDraw);
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] AudioSource sfxSource;
    [SerializeField] AudioSource musicSource;

    [Header("Sound Effects")]
    [SerializeField] AudioClip cardDraw;
    [SerializeField] AudioClip cupDrink;
    [SerializeField] AudioClip poisonDrink;
    [SerializeField] AudioClip antidoteDrink;
    [SerializeField] AudioClip playerDeath;
    [SerializeField] AudioClip buttonClick;
    [SerializeField] AudioClip gameWin;
    [SerializeField] AudioClip gameDraw;
    [SerializeField] AudioClip turnStart;
    [SerializeField] AudioClip turnEnd;
    [SerializeField] AudioClip directionReverse;
    [SerializeField] AudioClip scanReveal;

    public enum SFX
    {
        CardDraw,
        CupDrink,
        PoisonDrink,
        AntidoteDrink,
        PlayerDeath,
        ButtonClick,
        GameWin,
        GameDraw,
        TurnStart,
        TurnEnd,
        DirectionReverse,
        ScanReveal
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Sahne yuklemelerinde yok olmasini engelle (opsiyonel)
        DontDestroyOnLoad(gameObject);

        EnsureAudioSources();
    }

    void EnsureAudioSources()
    {
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
        }

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
        }
    }

    #region Public API

    /// <summary>
    /// Belirtilen SFX tipini bir kez calar.
    /// </summary>
    public void PlaySFX(SFX type)
    {
        AudioClip clip = GetClip(type);
        if (clip == null) return;

        sfxSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Belirtilen SFX tipini belirtilen ses seviyesiyle calar.
    /// </summary>
    public void PlaySFX(SFX type, float volumeScale)
    {
        AudioClip clip = GetClip(type);
        if (clip == null) return;

        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    /// <summary>
    /// Arka plan muzigini baslatir.
    /// </summary>
    public void PlayMusic(AudioClip clip, float volume = 0.3f)
    {
        if (musicSource == null || clip == null) return;

        musicSource.clip = clip;
        musicSource.volume = Mathf.Clamp01(volume);
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource?.Stop();
    }

    public void SetSFXVolume(float volume)
    {
        if (sfxSource != null)
            sfxSource.volume = Mathf.Clamp01(volume);
    }

    public void SetMusicVolume(float volume)
    {
        if (musicSource != null)
            musicSource.volume = Mathf.Clamp01(volume);
    }

    public void MuteAll(bool mute)
    {
        if (sfxSource != null) sfxSource.mute = mute;
        if (musicSource != null) musicSource.mute = mute;
    }

    #endregion

    #region Clip Resolution

    AudioClip GetClip(SFX type)
    {
        switch (type)
        {
            case SFX.CardDraw:        return cardDraw;
            case SFX.CupDrink:        return cupDrink;
            case SFX.PoisonDrink:     return poisonDrink;
            case SFX.AntidoteDrink:   return antidoteDrink;
            case SFX.PlayerDeath:     return playerDeath;
            case SFX.ButtonClick:     return buttonClick;
            case SFX.GameWin:         return gameWin;
            case SFX.GameDraw:        return gameDraw;
            case SFX.TurnStart:       return turnStart;
            case SFX.TurnEnd:         return turnEnd;
            case SFX.DirectionReverse:return directionReverse;
            case SFX.ScanReveal:      return scanReveal;
            default:                  return null;
        }
    }

    #endregion
}
