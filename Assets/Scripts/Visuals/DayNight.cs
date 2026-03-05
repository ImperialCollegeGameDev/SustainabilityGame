using System;
using UnityEngine;

/// <summary>
/// Syncs a looping day-night cycle to an audio track (e.g., theme music).
/// - Uses audio playback time as the "clock"
/// - Rotates a directional light for sun/moon travel
/// - Gradually changes light colour through the day using keyframes
/// - Exposes current time-of-day + day fraction for other systems (e.g., solar parks)
/// </summary>

public class DayNight : MonoBehaviour
{
    public static DayNight Instance { get; private set; }

    [Header("Audio (clock source)")]
    [Tooltip("If set, overrides AudioClip length as the day duration (seconds).")]
    public float manualDayLengthSeconds = 0f;

    [Header("Sun / Moon Light")]
    [Tooltip("Directional light that represents the sun/moon.")]
    public Light directionalLight;

    [Tooltip("Yaw (degrees) of the light path. Adjust to match your world orientation.")]
    public float azimuthDegrees = 0f;

    [Tooltip("Max elevation (degrees) at noon. Typical: 45–75.")]
    [Range(0f, 90f)]
    public float maxElevationDegrees = 65f;

    [Tooltip("If true, light intensity is faded at night; otherwise only colour/rotation is changed.")]
    public bool animateIntensity = true;

    [Tooltip("Intensity at noon.")]
    public float dayIntensity = 1.2f;

    [Tooltip("Intensity at midnight.")]
    public float nightIntensity = 0.05f;

    [Header("Colour Keyframes (day fraction)")]
    [Tooltip("Colour at 0.00 (start of loop): morning pink.")]
    public Color morningPink = new Color(1.0f, 0.65f, 0.85f);

    [Tooltip("Colour at 0.25: noon white (255,255,255).")]
    public Color noonWhite = Color.white;

    [Tooltip("Colour at 0.45: late afternoon warm.")]
    public Color lateAfternoonWarm = new Color(1.0f, 0.86f, 0.65f);

    [Tooltip("Colour at 0.55: golden hour.")]
    public Color goldenHour = new Color(1.0f, 0.70f, 0.40f);

    [Tooltip("Colour at 0.65: sunset orange-purple.")]
    public Color sunsetOrangePurple = new Color(0.95f, 0.45f, 0.70f);

    [Tooltip("Colour at 0.80: night dark blue.")]
    public Color nightDarkBlue = new Color(0.10f, 0.16f, 0.35f);

    [Tooltip("Colour at 1.00: end of loop (wrap to morning). Usually same as morningPink.")]
    public Color endWrap = new Color(1.0f, 0.65f, 0.85f);

    [Header("Time-of-day definition")]
    [Tooltip("Hour at which the loop starts (morning). Commonly 6 for 06:00.")]
    [Range(0f, 23.999f)]
    public float startHour = 6f;

    [Tooltip("Hours represented by one full music loop. Usually 24.")]
    public float hoursPerLoop = 24f;

    [Header("Debug")]
    public bool logOnStart = false;

    [SerializeField] private float NightStart = 18f;
    [SerializeField] private float DayStart = 6f;

    private Gradient _colourGradient;

    /// <summary>0..1 fraction through the loop (music position / loop length).</summary>
    public float DayFraction01 { get; private set; }

    /// <summary>Current time of day in hours [0..24).</summary>
    public float TimeOfDayHours { get; private set; }

    /// <summary>Current time of day in minutes [0..1440).</summary>
    public float TimeOfDayMinutes => TimeOfDayHours * 60f;

    public static event Action OnNightStarted;
    public static event Action OnDayStarted;

    /// <summary>Convenience: is it daytime? (customisable threshold).</summary>
    public bool IsDaytime
    {
        get
        {
            // Treat "daytime" as sun above horizon: fraction in (0, 0.5) if noon is at 0.25 and midnight at 0.75.
            // With the elevation model below, this aligns well with light elevation.
            float elevation = GetSunElevationDegrees(DayFraction01);
            return elevation > 0f;
        }
    }

    /// <summary>Convenience: 0 at midnight, 1 at noon, back to 0 at next midnight (clamped).</summary>
    public float DaylightStrength01
    {
        get
        {
            float elev = GetSunElevationDegrees(DayFraction01);
            // Elevation goes [-maxElevation..+maxElevation]. Map to [0..1].
            return Mathf.Clamp01((elev / maxElevationDegrees + 1f) * 0.5f);
        }
    }

    void Awake()
    {
        BuildColourGradient();
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple DayNight instances detected. There should only be one.");
            Destroy(this);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (logOnStart)
        {
            Debug.Log($"[MusicSyncedDayNightCycle] StartHour={startHour}, HoursPerLoop={hoursPerLoop}, DayLengthSeconds={GetDayLengthSeconds():0.00}");
        }

        // Basic safety checks
        if (MusicManager.Instance == null) Debug.LogWarning("[MusicSyncedDayNightCycle] MusicManager not found.");
        if (directionalLight == null) Debug.LogWarning("[MusicSyncedDayNightCycle] directionalLight not assigned.");
    }

    void Update()
    {

        if (GameState.Instance.PAUSED) return; // Don't advance time when paused.

        float dayLength = GetDayLengthSeconds();
        if (dayLength <= 0.01f) return;
        if (MusicManager.Instance == null || directionalLight == null) return;

        // Use the music playback position as the clock. This stays in sync even if timeScale changes.
        float t = GetMusicPositionSeconds();
        DayFraction01 = Mathf.Repeat(t / dayLength, 1f);

        // Convert to time-of-day (0..24)
        float previousTime = TimeOfDayHours;
        TimeOfDayHours = Mathf.Repeat(startHour + DayFraction01 * hoursPerLoop, 24f);

        if (CrossedThreshold(previousTime, TimeOfDayHours, NightStart))
            OnNightStarted?.Invoke();

        if (CrossedThreshold(previousTime, TimeOfDayHours, DayStart))
            OnDayStarted?.Invoke();

        // Apply light rotation and colour
        ApplyLightRotation(DayFraction01);
        ApplyLightColour(DayFraction01);

        if (animateIntensity)
            ApplyLightIntensity(DayFraction01);
    }

    /// <summary>Returns time of day as "HH:MM".</summary>
    public string GetTimeString()
    {
        int totalMinutes = Mathf.FloorToInt(TimeOfDayMinutes) % (24 * 60);
        int hh = totalMinutes / 60;
        int mm = totalMinutes % 60;
        return $"{hh:00}:{mm:00}";
    }

    /// <summary>
    /// For other systems: returns a normalised solar output factor (0 at night, 1 near noon).
    /// You can multiply solar park max output by this.
    /// </summary>
    public float GetSolarProductionFactor01()
    {
        // Simple physically-inspired factor: based on sun elevation above horizon.
        float elev = GetSunElevationDegrees(DayFraction01);
        if (elev <= 0f) return 0f;

        // Map elevation [0..maxElevation] to [0..1] with a gentle curve.
        float x = Mathf.Clamp01(elev / maxElevationDegrees);
        return Mathf.SmoothStep(0f, 1f, x);
    }

    private float GetDayLengthSeconds()
    {
        if (manualDayLengthSeconds > 0.01f) return manualDayLengthSeconds;
        if (MusicManager.Instance != null && MusicManager.Instance.MusicSource.clip != null) 
            return MusicManager.Instance.TrackLength;
        return 0f;
    }

    private float GetMusicPositionSeconds()
    {
        // Get music position from MusicManager
        if (MusicManager.Instance == null) return 0f;
        return MusicManager.Instance.MusicPosition;
    }

    private void ApplyLightRotation(float day01)
    {
        // Define sun "phase" where:
        // - day01 = 0.00 -> morning (near sunrise)
        // - day01 = 0.25 -> noon (max elevation)
        // - day01 = 0.50 -> evening (near sunset)
        // - day01 = 0.75 -> midnight (min elevation; below horizon)
        //
        // Use a sinusoid for elevation.
        float elevation = GetSunElevationDegrees(day01);

        // Azimuth: one full rotation per cycle.
        float azimuth = azimuthDegrees + day01 * 360f;

        // Construct rotation:
        // - Pitch controls elevation (around X)
        // - Yaw controls direction (around Y)
        // Unity directional light points *forward* (its -Z is the direction it shines),
        // so we rotate the transform itself accordingly.
        Quaternion rot = Quaternion.Euler(elevation, azimuth, 0f);
        directionalLight.transform.rotation = rot;
    }

    private float GetSunElevationDegrees(float day01)
    {
        // Sinusoid that peaks at day01=0.25 and troughs at 0.75.
        // sin(2pi*(day01 - 0.0)) peaks at 0.25 because sin(pi/2)=1.
        float s = Mathf.Sin(2f * Mathf.PI * day01);
        return s * maxElevationDegrees;
    }

    private void ApplyLightColour(float day01)
    {
        Color c = _colourGradient.Evaluate(day01);
        directionalLight.color = c;
    }

    private void ApplyLightIntensity(float day01)
    {
        // Tie intensity to elevation: bright when sun is high, dim at night.
        float elev = GetSunElevationDegrees(day01);

        float target;
        if (elev <= 0f)
        {
            target = nightIntensity;
        }
        else
        {
            float x = Mathf.Clamp01(elev / maxElevationDegrees);
            target = Mathf.Lerp(nightIntensity, dayIntensity, Mathf.SmoothStep(0f, 1f, x));
        }

        directionalLight.intensity = target;
    }

    private void BuildColourGradient()
    {
        // Key times are fractions through the loop.
        // Adjust these if your music sections differ.
        // 0.00 morning pink
        // 0.25 noon white
        // 0.45 late afternoon warm
        // 0.55 golden hour
        // 0.65 sunset orange-purple
        // 0.80 night dark blue
        // 1.00 wrap back to morning
        _colourGradient = new Gradient();

        GradientColorKey[] colorKeys = new GradientColorKey[]
        {
            new GradientColorKey(morningPink, 0.00f),
            new GradientColorKey(noonWhite, 0.25f),
            new GradientColorKey(lateAfternoonWarm, 0.45f),
            new GradientColorKey(goldenHour, 0.55f),
            new GradientColorKey(sunsetOrangePurple, 0.65f),
            new GradientColorKey(nightDarkBlue, 0.80f),
            new GradientColorKey(endWrap, 1.00f),
        };

        // Alpha stays at 1; you can animate it if you want fades.
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[]
        {
            new GradientAlphaKey(1f, 0f),
            new GradientAlphaKey(1f, 1f)
        };

        _colourGradient.SetKeys(colorKeys, alphaKeys);
    }

    bool CrossedThreshold(float previous, float current, float threshold)
    {
        if (previous <= current)
        {
            return previous < threshold && current >= threshold;
        }
        else
        {
            // Wrapped past midnight
            return previous < threshold || current >= threshold;
        }
    }
}