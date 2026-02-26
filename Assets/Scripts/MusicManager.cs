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
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

    [Header("Fade Settings")]
    [SerializeField] private float fadeTime = 1f;

    [Header("UI Sound Effects")]
    [SerializeField] private AudioClip uiClickSound;
    [SerializeField] private AudioClip gameLungCancer;
    [SerializeField] private AudioClip uiHoverSound;
    [SerializeField] private AudioClip uiSuccessSound;
    [SerializeField] private AudioClip uiErrorSound;
    [SerializeField] private AudioClip uiOpenSound;
    [SerializeField] private AudioClip uiCloseSound;
    [SerializeField] private AudioClip repairSound;

    [SerializeField] private AudioClip mainMainAndCredits;
    [SerializeField] private AudioClip mainLeaderboard;
    [SerializeField] private AudioClip mainGame;

    // Track management
    private AudioClip currentTrack;
    private bool isPaused;
    private Coroutine fadeCoroutine;

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
        Hover,
        Success,
        Error,
        Open,
        Close,
        Repair,
        LungCancer
    }

    // Properties for external access
    public AudioSource MusicSource => musicSource;
    public float MusicPosition => musicSource != null ? (float)musicSource.timeSamples / musicSource.clip.frequency : 0f;
    public float TrackLength => musicSource?.clip?.length ?? 0f;
    public bool IsPlaying => musicSource != null && musicSource.isPlaying && !isPaused;
    public bool IsPaused => isPaused;

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
        // Handle any real-time audio updates if needed
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

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
    }

    #region Music Control
    /// <summary>
    /// Plays a music track with optional fade-in
    /// </summary>
    public void PlayTrack(AudioClip track, bool fadeIn = false)
    {
        if (track == null)
        {
            Debug.LogWarning("[MusicManager] Cannot play null track");
            return;
        }

        StopFade();

        if (fadeIn && musicSource.isPlaying)
        {
            fadeCoroutine = StartCoroutine(CrossFade(track));
        }
        else
        {
            musicSource.clip = track;
            musicSource.Play();
            currentTrack = track;
            isPaused = false;
            OnTrackChanged?.Invoke(track);
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
            sfxSource.PlayOneShot(clip, volumeScale * sfxVolume * masterVolume);
        }
    }

    /// <summary>
    /// Plays a sound effect at a specific world position (3D audio)
    /// </summary>
    public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volumeScale = 1f)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, position, volumeScale * sfxVolume * masterVolume);
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
            UISoundType.LungCancer => gameLungCancer,
            UISoundType.Hover => uiHoverSound,
            UISoundType.Success => uiSuccessSound,
            UISoundType.Error => uiErrorSound,
            UISoundType.Open => uiOpenSound,
            UISoundType.Close => uiCloseSound,
            UISoundType.Repair => repairSound,
            _ => null
        };
        // do not replay if already playing
        if (sfxSource.isPlaying)
            return;
        if (clipToPlay != null)
        {
            PlaySFX(clipToPlay, volumeScale);
        }
    }
    #endregion

    #region Volume Control
    /// <summary>
    /// Sets the master volume (affects all audio)
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
    }

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
    /// Gets the current master volume
    /// </summary>
    public float GetMasterVolume() => masterVolume;

    /// <summary>
    /// Gets the current music volume
    /// </summary>
    public float GetMusicVolume() => musicVolume;

    /// <summary>
    /// Gets the current SFX volume
    /// </summary>
    public float GetSFXVolume() => sfxVolume;

    private void ApplyVolumeSettings()
    {
        if (musicSource != null)
            musicSource.volume = musicVolume * masterVolume;

        if (sfxSource != null)
            sfxSource.volume = sfxVolume * masterVolume;
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
    /// <summary>
    /// Mutes all audio
    /// </summary>
    public void MuteAll()
    {
        SetMasterVolume(0f);
    }

    /// <summary>
    /// Unmutes all audio to previous volume levels
    /// </summary>
    public void UnmuteAll()
    {
        SetMasterVolume(1f);
    }
    #endregion
}