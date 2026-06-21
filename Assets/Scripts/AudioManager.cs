using UnityEngine;

/// <summary>
/// Faz 3: Oyundaki tum ses efektlerini tek bir merkezden yonetir.
/// Singleton yapisi: her sahnede yalnizca bir instances bulunur.
/// Kullanim: AudioManager.Instance.PlaySFX(AudioManager.SFX.CardDraw);
/// </summary>
public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;
    public static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<AudioManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("AudioManager");
                    instance = go.AddComponent<AudioManager>();
                    Debug.Log("[AudioManager] Instance was null. Automatically created AudioManager GameObject.");
                }
            }
            return instance;
        }
    }

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
        ScanReveal,
        
        // New sound effects
        MainMenuMusic,
        StartGame,
        BardakSecme,
        KartSecim,
        CardOpening,
        NegativeCard,
        NotrCard,
        PositiveCard,
        Poison,
        Antidote,
        EndGame1,
        EndGame2,
        Pause,
        CardWaiting
    }

    private System.Collections.Generic.Dictionary<SFX, AudioClip> dynamicClips = new System.Collections.Generic.Dictionary<SFX, AudioClip>();
    private Coroutine endGameCoroutine;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        // Sahne yuklemelerinde yok olmasini engelle (opsiyonel)
        DontDestroyOnLoad(gameObject);

        EnsureAudioSources();
        LoadDynamicClips();
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

    void LoadDynamicClips()
    {
        LoadAndCheckClip(SFX.MainMenuMusic, "Sounds/mainmenu_sound");
        LoadAndCheckClip(SFX.StartGame,     "Sounds/startgame_sound");
        LoadAndCheckClip(SFX.BardakSecme,   "Sounds/bardak_secme");
        LoadAndCheckClip(SFX.KartSecim,     "Sounds/kart_secim");
        LoadAndCheckClip(SFX.CardOpening,   "Sounds/card_opening");
        LoadAndCheckClip(SFX.NegativeCard,  "Sounds/negative_card");
        LoadAndCheckClip(SFX.NotrCard,      "Sounds/notr_card");
        LoadAndCheckClip(SFX.PositiveCard,  "Sounds/positive_card");
        LoadAndCheckClip(SFX.Poison,        "Sounds/poison");
        LoadAndCheckClip(SFX.Antidote,      "Sounds/antidote");
        LoadAndCheckClip(SFX.EndGame1,      "Sounds/endgame_sound1");
        LoadAndCheckClip(SFX.EndGame2,      "Sounds/endgame_sound2");
        LoadAndCheckClip(SFX.Pause,         "Sounds/pause");
        LoadAndCheckClip(SFX.ButtonClick,   "Sounds/click");
        LoadAndCheckClip(SFX.CardWaiting,   "Sounds/card_waiting");
    }

    void LoadAndCheckClip(SFX type, string path)
    {
        AudioClip clip = Resources.Load<AudioClip>(path);
        if (clip == null)
        {
            Debug.LogError($"[AudioManager] Failed to load clip: {path} from Resources! Make sure it is inside Assets/Resources/Sounds/");
        }
        else
        {
            dynamicClips[type] = clip;
            Debug.Log($"[AudioManager] Successfully loaded: {path}");
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
        musicSource.loop = true;
        musicSource.Play();
    }

    public void PlayMainMenuMusic()
    {
        AudioClip clip = GetClip(SFX.MainMenuMusic);
        if (clip != null)
        {
            PlayMusic(clip, 0.8f);
        }
    }

    public void PlayEndGameSequence()
    {
        StopMusic();
        endGameCoroutine = StartCoroutine(EndGameSequenceCoroutine());
    }

    private System.Collections.IEnumerator EndGameSequenceCoroutine()
    {
        if (musicSource == null) yield break;

        AudioClip clip1 = GetClip(SFX.EndGame1);
        AudioClip clip2 = GetClip(SFX.EndGame2);

        if (clip1 != null)
        {
            musicSource.loop = false;
            musicSource.clip = clip1;
            musicSource.volume = 0.5f;
            musicSource.Play();
            yield return new WaitForSeconds(clip1.length);
        }

        if (clip2 != null)
        {
            musicSource.loop = false;
            musicSource.clip = clip2;
            musicSource.volume = 0.5f;
            musicSource.Play();
        }
    }

    public void StopMusic()
    {
        if (endGameCoroutine != null)
        {
            StopCoroutine(endGameCoroutine);
            endGameCoroutine = null;
        }
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

    public AudioClip GetClip(SFX type)
    {
        if (dynamicClips.TryGetValue(type, out var dynamicClip) && dynamicClip != null)
        {
            return dynamicClip;
        }

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
