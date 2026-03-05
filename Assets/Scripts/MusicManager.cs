using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    [Header("Music Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource uiSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Volume Settings")]
    [SerializeField, Range(0f, 1f)] private float musicVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

    [Header("Fade Settings")]
    [SerializeField] private float fadeTime = 1f;

    [Header("UI Sound Effects")]
    [SerializeField] private AudioClip uiClickSound;
    [SerializeField] private AudioClip uiBuySound;
    [SerializeField] private AudioClip uiBuildSound;
    [SerializeField] private AudioClip uiLeavesSound;
    [SerializeField] private AudioClip uiFailSound;

    [Header("Game Sound Effects")]
    [SerializeField] private AudioClip gameLungCancer;
    [SerializeField] private AudioClip repairSound;
    [SerializeField] private AudioClip gameLSDSound;

    [Header("Main Tracks")]
    [SerializeField] private AudioClip mainMainAndCredits;
    [SerializeField] private AudioClip mainLeaderboard;
    [SerializeField] private AudioClip mainGame;

    // Track management
    private AudioClip currentTrack;
    private bool isPaused;
    private Coroutine fadeCoroutine;
    
    // SFX tracking to prevent overlapping sounds
    private AudioClip currentSFXClip;
    private float sfxStartTime;

    // Events for other systems to listen to
    public static event Action<AudioClip> OnTrackChanged;
    public static event Action OnMusicPaused;
    public static event Action OnMusicResumed;

    // Singleton pattern for easy access
    public static MusicManager Instance { get; private set; }

    // UI Sound Effects enum
    public enum UISoundType
    {
        Click,
        Buy,
        Build,
        Leaves,
        Fail,
    }

    // Game SFX enum
    public enum SFXSoundType
    {
        LungCancer,
        Repair,
        LSD,
    }

    // Main Track enum
    public enum MainTrackType
    {
        MainAndCredits,
        Leaderboard,
        Game,
    }

    // Properties for external access
    public AudioSource MusicSource => musicSource;
    public float MusicPosition => musicSource != null ? (float)musicSource.timeSamples / musicSource.clip.frequency : 0f;
    public float TrackLength => musicSource?.clip?.length ?? 0f;
    public bool IsPlaying => musicSource != null && musicSource.isPlaying && !isPaused;
    public bool IsPaused => isPaused;

    /// <summary>
    /// Public access to music volume (0-1 range)
    /// </summary>
    public float MusicVolumePublic
    {
        get => musicVolume;
        set
        {
            musicVolume = Mathf.Clamp01(value);
            ApplyVolumeSettings();
        }
    }

    /// <summary>
    /// Public access to SFX volume (0-1 range)
    /// </summary>
    public float SFXVolumePublic
    {
        get => sfxVolume;
        set
        {
            sfxVolume = Mathf.Clamp01(value);
            ApplyVolumeSettings();
        }
    }

    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        ApplyVolumeSettings();
    }

    void Update()
    {
        // Handle SFX cleanup when finished playing
        if (currentSFXClip != null && !sfxSource.isPlaying)
        {
            currentSFXClip = null;
            sfxStartTime = 0f;
        }
    }

    private void Initialize()
    {
        // Create AudioSources if not assigned
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        if (uiSource == null)
        {
            uiSource = gameObject.AddComponent<AudioSource>();
            uiSource.loop = false;
            uiSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
    }

    #region Music Control
    /// <summary>
    /// Plays a main track by type with automatic fade and prevents restarting current track
    /// </summary>
    public void PlayMainTrack(MainTrackType trackType)
    {
        AudioClip clipToPlay = trackType switch
        {
            MainTrackType.MainAndCredits => mainMainAndCredits,
            MainTrackType.Leaderboard => mainLeaderboard,
            MainTrackType.Game => mainGame,
            _ => null
        };
        
        if (clipToPlay == null)
        {
            return;
        }

        // Check if the requested track is already playing
        if (currentTrack == clipToPlay && musicSource.isPlaying && !isPaused)
        {
            return;
        }

        // If the same track is paused, just resume it
        if (currentTrack == clipToPlay && isPaused)
        {
            ResumeMusic();
            return;
        }

        StopFade();

        if (musicSource.isPlaying)
        {
            fadeCoroutine = StartCoroutine(CrossFade(clipToPlay));
        }
        else
        {
            musicSource.clip = clipToPlay;
            musicSource.Play();
            currentTrack = clipToPlay;
            isPaused = false;
            OnTrackChanged?.Invoke(clipToPlay);
        }
    }

    /// <summary>
    /// Stops the current music track
    /// </summary>
    public void StopMusic(bool fadeOut = false)
    {
        if (fadeOut)
        {
            fadeCoroutine = StartCoroutine(FadeOut());
        }
        else
        {
            musicSource.Stop();
            currentTrack = null;
            isPaused = false;
        }
    }

    /// <summary>
    /// Pauses the current music
    /// </summary>
    public void PauseMusic()
    {
        if (musicSource.isPlaying)
        {
            musicSource.Pause();
            isPaused = true;
            OnMusicPaused?.Invoke();
        }
    }

    /// <summary>
    /// Resumes paused music
    /// </summary>
    public void ResumeMusic()
    {
        if (isPaused)
        {
            musicSource.UnPause();
            isPaused = false;
            OnMusicResumed?.Invoke();
        }
    }

    /// <summary>
    /// Toggles play/pause state
    /// </summary>
    public void ToggleMusic()
    {
        if (isPaused)
            ResumeMusic();
        else
            PauseMusic();
    }

    /// <summary>
    /// Sets the music playback position in seconds
    /// </summary>
    public void SetMusicPosition(float seconds)
    {
        if (musicSource.clip != null)
        {
            musicSource.time = Mathf.Clamp(seconds, 0f, musicSource.clip.length);
        }
    }

    /// <summary>
    /// Sets the music playback position as a 0-1 fraction
    /// </summary>
    public void SetMusicPositionNormalized(float fraction01)
    {
        if (musicSource.clip != null)
        {
            SetMusicPosition(fraction01 * musicSource.clip.length);
        }
    }
    #endregion

    #region Sound Effects
    /// <summary>
    /// Plays a one-shot sound effect
    /// </summary>
    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip != null)
        {
            // Check if the same SFX is already playing
            if (currentSFXClip == clip && sfxSource.isPlaying)
            {
                return; // Skip playing if same clip is already active
            }
            
            // Update tracking variables
            currentSFXClip = clip;
            sfxStartTime = Time.time;
            
            sfxSource.PlayOneShot(clip, volumeScale * sfxVolume * 0.5f);
        }
    }

    /// <summary>
    /// Plays a sound effect at a specific world position (3D audio)
    /// </summary>
    public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volumeScale = 1f)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, position, volumeScale * sfxVolume * 0.5f);
        }
    }

    /// <summary>
    /// Plays a UI sound effect by type
    /// </summary>
    public void PlayUISound(UISoundType soundType, float volumeScale = 1f)
    {

        AudioClip clipToPlay = soundType switch
        {
            UISoundType.Click => uiClickSound,
            UISoundType.Buy => uiBuySound,
            UISoundType.Build => uiBuildSound,
            UISoundType.Leaves => uiLeavesSound,
            UISoundType.Fail => uiFailSound,
            _ => null
        };
            
        if (clipToPlay != null)
        {
            uiSource.PlayOneShot(clipToPlay, volumeScale * sfxVolume);
        }
    }

    /// <summary>
    /// Plays a game sound effect by type
    /// </summary>
    public void PlayGameSFX(SFXSoundType soundType, float volumeScale = 1f)
    {
        AudioClip clipToPlay = soundType switch
        {
            SFXSoundType.LungCancer => gameLungCancer,
            SFXSoundType.Repair => repairSound,
            SFXSoundType.LSD => gameLSDSound,
            _ => null
        };
        
        if (clipToPlay != null)
        {
            // Check if the same game SFX is already playing
            if (currentSFXClip == clipToPlay && sfxSource.isPlaying)
            {
                return; // Skip playing if same clip is already active
            }
            
            PlaySFX(clipToPlay, volumeScale);
        }
    }
    #endregion

    #region Volume Control
    /// <summary>
    /// Sets the music volume
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
    }

    /// <summary>
    /// Sets the sound effects volume
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
    }

    /// <summary>
    /// Gets the current music volume
    /// </summary>
    public float GetMusicVolume() => musicVolume;

    /// <summary>
    /// Gets the current SFX volume
    /// </summary>
    public float GetSFXVolume() => sfxVolume;

    /// <summary>
    /// Sets the music volume (0-1 range)
    /// </summary>
    public void SetMusicVolumeToggle(float volume)
    {
        SetMusicVolume(volume);
    }

    /// <summary>
    /// Sets both SFX and UI volume (0-1 range)
    /// </summary>
    public void SetSFXUIVolumeToggle(float volume)
    {
        SetSFXVolume(volume);
    }

    private void ApplyVolumeSettings()
    {
        if (musicSource != null)
            musicSource.volume = musicVolume * 0.3f;

        if (uiSource != null)
            uiSource.volume = sfxVolume * 0.5f;

        if (sfxSource != null)
            sfxSource.volume = sfxVolume * 0.5f;
    }
    #endregion

    #region Fade Effects
    private void StopFade()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
    }

    private IEnumerator FadeOut()
    {
        float startVolume = musicSource.volume;
        float timer = 0f;

        while (timer < fadeTime)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / fadeTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        musicSource.Stop();
        musicSource.volume = startVolume;
        fadeCoroutine = null;
    }

    private IEnumerator FadeIn(float targetVolume)
    {
        musicSource.volume = 0f;
        float timer = 0f;

        while (timer < fadeTime)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / fadeTime;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, t);
            yield return null;
        }

        musicSource.volume = targetVolume;
        fadeCoroutine = null;
    }

    private IEnumerator CrossFade(AudioClip newTrack)
    {
        float startVolume = musicSource.volume;
        float timer = 0f;

        // Fade out current track
        while (timer < fadeTime * 0.5f)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / (fadeTime * 0.5f);
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        // Switch tracks
        musicSource.clip = newTrack;
        musicSource.Play();
        currentTrack = newTrack;
        OnTrackChanged?.Invoke(newTrack);

        timer = 0f;

        // Fade in new track
        while (timer < fadeTime * 0.5f)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / (fadeTime * 0.5f);
            musicSource.volume = Mathf.Lerp(0f, startVolume, t);
            yield return null;
        }

        musicSource.volume = startVolume;
        fadeCoroutine = null;
    }
    #endregion

    #region Utility
    // Utility methods can be added here if needed in the future
    #endregion
}