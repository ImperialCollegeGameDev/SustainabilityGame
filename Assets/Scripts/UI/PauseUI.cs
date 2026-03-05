using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PauseUI : MonoBehaviour
{
    public GameObject background;
    public TMPro.TextMeshProUGUI titleText;
    public Button saveButton;

    public List<GameObject> objsToAnimateInOrder;

    public Toggle shadowsToggle;
    public Toggle scaleToggle;

    public Slider mainMusic;
    public Slider sfxMusic;

    private bool shadowsOn = true;
    private bool scaleOn = true;

    // Scale tracking for proper restoration
    private Dictionary<GameObject, Vector3> originalScales = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, Vector3> originalPositions = new Dictionary<GameObject, Vector3>();
    private List<int> activeTweenIds = new List<int>();

    // Music state tracking for pause/resume
    private AudioClip savedMusicTrack;
    private float savedMusicPosition;
    private bool musicWasPausedBefore;
    private bool isInLiveGame; // Track if this is a live game pause vs settings menu

    // Callback storage
    private System.Action onCloseCallback;

    public void ToggleShadows()
    {
        MusicManager.Instance.PlayUISound(MusicManager.UISoundType.Click);
        shadowsOn = shadowsToggle.isOn;
        
        if (Main.Instance != null)
        {
            Main.Instance.ToggleShadows(shadowsOn);
            // Also toggle lighting when shadows are toggled
            Main.Instance.ToggleLighting(shadowsOn);
        }
        else
        {
            Debug.LogWarning("[PauseUI] Main.Instance is null, cannot toggle shadows");
        }

        Debug.Log($"[PauseUI] Shadows and lighting {(shadowsOn ? "enabled" : "disabled")}");
    }

    public void SetRenderScale()
    {
        MusicManager.Instance.PlayUISound(MusicManager.UISoundType.Click);
        scaleOn = scaleToggle.isOn;
        
        if (Main.Instance != null)
        {
            Main.Instance.ToggleRenderScale(scaleOn);
        }
        else
        {
            Debug.LogWarning("[PauseUI] Main.Instance is null, cannot toggle render scale");
        }
        
        Debug.Log($"[PauseUI] Render scale {(scaleOn ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// Called when the music volume slider value changes
    /// </summary>
    public void OnMusicVolumeChanged(float value)
    {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetMusicVolume(value);
        }
    }

    /// <summary>
    /// Called when the SFX volume slider value changes
    /// </summary>
    public void OnSFXVolumeChanged(float value)
    {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetSFXVolume(value);
        }
    }

    /// <summary>
    /// Public method to close the pause popup with animated sequence
    /// Call this from external buttons or scripts
    /// </summary>
    public void ClosePause()
    {
        Debug.Log("[PauseUI] ClosePause called - starting close sequence");
        
        // Play close sound
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayUISound(MusicManager.UISoundType.Click);
            Debug.Log("[PauseUI] Played close UI sound");
        }

        // Cancel any existing animations
        CleanupAnimations();
        Debug.Log("[PauseUI] Cleaned up existing animations");

        // Restore music state before closing (only if in live game)
        if (isInLiveGame)
        {
            RestoreMusicState();
        }

        // Start close animation sequence
        StartCloseAnimation();
    }

    /// <summary>
    /// Animate the closing of the pause UI with reverse sequence
    /// </summary>
    private void StartCloseAnimation()
    {
        Debug.Log("[PauseUI] Starting close animation sequence");
        
        // Animate UI elements closing in reverse order
        for (int i = objsToAnimateInOrder.Count - 1; i >= 0; i--)
        {
            GameObject obj = objsToAnimateInOrder[i];
            if (obj != null)
            {
                float delay = (objsToAnimateInOrder.Count - 1 - i) * 0.08f; // Faster reverse timing
                AnimateElementClose(obj, delay);
            }
        }

        // Animate background closing after all elements
        float backgroundDelay = objsToAnimateInOrder.Count * 0.08f + 0.2f;
        AnimateBackgroundClose(backgroundDelay);

        // Calculate total animation time and destroy after completion
        float totalAnimationTime = backgroundDelay + 0.4f;
        Debug.Log($"[PauseUI] Close animation will complete in {totalAnimationTime} seconds, then destroy GameObject");
        
        LeanTween.delayedCall(totalAnimationTime, () => {
            Debug.Log("[PauseUI] Close animation completed");
            
            // Invoke callback before destroying
            if (onCloseCallback != null)
            {
                Debug.Log("[PauseUI] Invoking onClose callback");
                onCloseCallback.Invoke();
            }
            else
            {
                Debug.Log("[PauseUI] No onClose callback to invoke");
            }
            
            Debug.Log("[PauseUI] Destroying GameObject");
            Destroy(gameObject);
        });
    }

    /// <summary>
    /// Animate individual element closing
    /// </summary>
    private void AnimateElementClose(GameObject obj, float delay)
    {
        CanvasGroup canvas = obj.GetComponent<CanvasGroup>();
        RectTransform rect = obj.GetComponent<RectTransform>();

        // Fade out
        if (canvas != null)
        {
            int fadeId = LeanTween.alphaCanvas(canvas, 0f, 0.3f)
                .setEaseInQuart()
                .setDelay(delay).id;
            activeTweenIds.Add(fadeId);
        }

        // Scale down
        int scaleId = LeanTween.scale(obj, Vector3.zero, 0.4f)
            .setEaseInBack()
            .setDelay(delay).id;
        activeTweenIds.Add(scaleId);

        // Slide down
        if (rect != null && originalPositions.ContainsKey(obj))
        {
            Vector3 targetPos = originalPositions[obj];
            targetPos.y -= 30f; // Slide down effect

            int moveId = LeanTween.move(rect, targetPos, 0.3f)
                .setEaseInQuart()
                .setDelay(delay + 0.1f).id;
            activeTweenIds.Add(moveId);
        }
    }

    /// <summary>
    /// Animate background closing
    /// </summary>
    private void AnimateBackgroundClose(float delay)
    {
        if (background != null)
        {
            CanvasGroup bgCanvas = background.GetComponent<CanvasGroup>();
            
            // Fade out background
            if (bgCanvas != null)
            {
                int fadeId = LeanTween.alphaCanvas(bgCanvas, 0f, 0.3f)
                    .setEaseInQuart()
                    .setDelay(delay).id;
                activeTweenIds.Add(fadeId);
            }

            // Scale down background
            int scaleId = LeanTween.scale(background, Vector3.zero, 0.4f)
                .setEaseInBack()
                .setDelay(delay).id;
            activeTweenIds.Add(scaleId);
        }
    }

    public void Load(string TopText, bool inLiveGame, System.Action onCloseCallback = null)
    {
        Debug.Log($"[PauseUI] Load called with TopText: '{TopText}', inLiveGame: {inLiveGame}");
        
        // Store the live game flag
        isInLiveGame = inLiveGame;
        
        // Store the callback for later use
        this.onCloseCallback = onCloseCallback;
        if (onCloseCallback != null)
        {
            Debug.Log("[PauseUI] onClose callback registered");
        }
        else
        {
            Debug.Log("[PauseUI] No onClose callback provided");
        }
        
        // Ensure the GameObject is active for animations
        gameObject.SetActive(true);
        Debug.Log("[PauseUI] GameObject activated for Load animation");

        // Save current music state and switch to menu music (only if in live game)
        if (isInLiveGame)
        {
            SaveMusicState();
        }

        // Set UI properties first
        titleText.text = TopText;
        saveButton.interactable = inLiveGame;
        Debug.Log($"[PauseUI] Title set to: '{TopText}', Save button interactive: {inLiveGame}");

        SetupDefaultValues();

        // Initialize UI controls
        InitializeSliders();
        InitializeToggles();
        Debug.Log("[PauseUI] Initialized sliders and toggles");
        
        // Record original states BEFORE hiding elements
        RecordOriginalStates();
        Debug.Log($"[PauseUI] Recorded original states for {originalScales.Count} objects");
        
        // Hide all elements immediately for animation setup
        HideAllElementsForAnimation();
        Debug.Log("[PauseUI] Hidden all elements for animation setup");
        
        // Start the entrance animation sequence
        AnimateBackground();
        AnimateUIElements();
        Debug.Log("[PauseUI] Started background and UI element animations");
    }

    private void SetupDefaultValues()
    {
        // Initialize graphics settings in Main if not already done
        if (Main.Instance != null)
        {
            Main.Instance.InitializeGraphicsSettings();
            
            // Get current states from Main
            shadowsOn = Main.Instance.AreShadowsEnabled();
            scaleOn = Main.Instance.IsRenderScaleEnabled();
            
            Debug.Log($"[PauseUI] Loaded graphics settings - Shadows: {shadowsOn}, RenderScale: {scaleOn}");
        }
        else
        {
            Debug.LogWarning("[PauseUI] Main.Instance is null, cannot initialize graphics settings");
        }
    }

    /// <summary>
    /// Hide all UI elements immediately for clean animation start
    /// </summary>
    private void HideAllElementsForAnimation()
    {
        Debug.Log("[PauseUI] Hiding all elements for animation setup");

        // Hide background initially
        if (background != null)
        {
            CanvasGroup bgCanvas = background.GetComponent<CanvasGroup>();
            if (bgCanvas != null)
            {
                bgCanvas.alpha = 0f;
            }
            background.transform.localScale = Vector3.zero;
        }

        // Hide all UI elements initially
        foreach (var obj in objsToAnimateInOrder)
        {
            if (obj != null)
            {
                CanvasGroup canvas = obj.GetComponent<CanvasGroup>();
                if (canvas != null)
                {
                    canvas.alpha = 0f;
                }
                obj.transform.localScale = Vector3.zero;
                
                // Move to initial animation position
                RectTransform rect = obj.GetComponent<RectTransform>();
                if (rect != null && originalPositions.ContainsKey(obj))
                {
                    Vector3 originalPos = originalPositions[obj];
                    rect.anchoredPosition = new Vector3(originalPos.x, originalPos.y - 30f, originalPos.z);
                }
            }
        }
    }

    /// <summary>
    /// Record original scales and positions before any modifications
    /// </summary>
    private void RecordOriginalStates()
    {
        // Clear any existing records
        originalScales.Clear();
        originalPositions.Clear();

        // Record background original state
        if (background != null)
        {
            originalScales[background] = background.transform.localScale;
            RectTransform bgRect = background.GetComponent<RectTransform>();
            if (bgRect != null)
            {
                originalPositions[background] = bgRect.anchoredPosition;
            }
        }

        // Record all UI elements original states
        foreach (var obj in objsToAnimateInOrder)
        {
            if (obj != null)
            {
                originalScales[obj] = obj.transform.localScale;
                RectTransform rect = obj.GetComponent<RectTransform>();
                if (rect != null)
                {
                    originalPositions[obj] = rect.anchoredPosition;
                }
            }
        }
    }

    /// <summary>
    /// Initialize slider values from MusicManager current settings
    /// </summary>
    private void InitializeSliders()
    {
        if (MusicManager.Instance != null)
        {
            // Set slider values to current MusicManager volumes
            if (mainMusic != null)
            {
                mainMusic.value = MusicManager.Instance.GetMusicVolume();
                // Add listener for slider changes
                mainMusic.onValueChanged.RemoveAllListeners();
                mainMusic.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            if (sfxMusic != null)
            {
                sfxMusic.value = MusicManager.Instance.GetSFXVolume();
                // Add listener for slider changes
                sfxMusic.onValueChanged.RemoveAllListeners();
                sfxMusic.onValueChanged.AddListener(OnSFXVolumeChanged);
            }
        }
    }

    /// <summary>
    /// Initialize toggle states and add listeners
    /// </summary>
    private void InitializeToggles()
    {
        if (shadowsToggle != null)
        {
            shadowsToggle.isOn = shadowsOn;
            shadowsToggle.onValueChanged.RemoveAllListeners();
            shadowsToggle.onValueChanged.AddListener((bool value) => ToggleShadows());
        }

        if (scaleToggle != null)
        {
            scaleToggle.isOn = scaleOn;
            scaleToggle.onValueChanged.RemoveAllListeners();
            scaleToggle.onValueChanged.AddListener((bool value) => SetRenderScale());
        }
    }

    /// <summary>
    /// Animate background with scale and fade
    /// </summary>
    private void AnimateBackground()
    {
        if (background != null)
        {
            CanvasGroup bgCanvas = background.GetComponent<CanvasGroup>();
            if (bgCanvas != null)
            {
                // Fade in background
                int fadeId = LeanTween.alphaCanvas(bgCanvas, 0.5f, 0.4f)
                    .setEaseOutQuart()
                    .setDelay(0.1f).id;
                activeTweenIds.Add(fadeId);
            }

            // Scale in background with bounce - restore to original scale
            if (originalScales.ContainsKey(background))
            {
                int scaleId = LeanTween.scale(background, originalScales[background], 0.5f)
                    .setEaseOutBack()
                    .setDelay(0.1f).id;
                activeTweenIds.Add(scaleId);
            }
        }
    }

    /// <summary>
    /// Animate UI elements with staggered multi-effect animations
    /// </summary>
    private void AnimateUIElements()
    {
        int i = 0;
        foreach (var obj in objsToAnimateInOrder)
        {
            if (obj != null)
            {
                float delay = 0.4f + i * 0.15f; // Start after background, stagger each element
                
                CanvasGroup canvas = obj.GetComponent<CanvasGroup>();
                RectTransform rect = obj.GetComponent<RectTransform>();
                
                // Fade in with varied easing
                if (canvas != null)
                {
                    int fadeId = LeanTween.alphaCanvas(canvas, 1f, 0.6f)
                        .setEaseOutCubic()
                        .setDelay(delay).id;
                    activeTweenIds.Add(fadeId);
                }

                // Scale animation - restore to original scale
                if (originalScales.ContainsKey(obj))
                {
                    LeanTweenType scaleEase = (i % 3) switch
                    {
                        0 => LeanTweenType.easeOutElastic,
                        1 => LeanTweenType.easeOutBack,
                        _ => LeanTweenType.easeOutBounce
                    };

                    int scaleId = LeanTween.scale(obj, originalScales[obj], 0.7f)
                        .setEase(scaleEase)
                        .setDelay(delay).id;
                    activeTweenIds.Add(scaleId);
                }

                // Slide up animation - restore to original position
                if (rect != null && originalPositions.ContainsKey(obj))
                {
                    int moveId = LeanTween.move(rect, originalPositions[obj], 0.6f)
                        .setEaseOutQuart()
                        .setDelay(delay + 0.1f).id;
                    activeTweenIds.Add(moveId);
                }

                // Add subtle secondary animation after main animation
                float secondaryDelay = delay + 0.8f;
                StartSecondaryAnimation(obj, secondaryDelay, i);
            }
            i++;
        }
    }

    /// <summary>
    /// Add subtle breathing/floating effects after main animations
    /// </summary>
    private void StartSecondaryAnimation(GameObject obj, float delay, int index)
    {
        if (!originalScales.ContainsKey(obj)) return;

        Vector3 originalScale = originalScales[obj];
        
        // Gentle breathing scale effect - relative to original scale
        int breatheId = LeanTween.scale(obj, originalScale * 1.02f, 2.0f)
            .setEaseInOutSine()
            .setLoopPingPong()
            .setDelay(delay + index * 0.2f).id;
        activeTweenIds.Add(breatheId);

        // Very subtle floating effect for some elements
        if (index % 2 == 0 && originalPositions.ContainsKey(obj))
        {
            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect != null)
            {
                Vector3 originalPos = originalPositions[obj];
                int floatId = LeanTween.moveY(rect, originalPos.y + 2f, 3.0f)
                    .setEaseInOutSine()
                    .setLoopPingPong()
                    .setDelay(delay + index * 0.3f).id;
                activeTweenIds.Add(floatId);
            }
        }
    }

    public void onSaveAndExit()
    {
        Main.Instance.SaveGame();
        Main.Instance.ReturnHome();
    }

    /// <summary>
    /// Save the current music state before opening pause menu
    /// </summary>
    private void SaveMusicState()
    {
        if (MusicManager.Instance != null)
        {
            // Save current track and position
            savedMusicTrack = MusicManager.Instance.MusicSource?.clip;
            savedMusicPosition = MusicManager.Instance.MusicPosition;
            musicWasPausedBefore = MusicManager.Instance.IsPaused;
            
            Debug.Log($"[PauseUI] Saved music state - Track: {savedMusicTrack?.name ?? "none"}, Position: {savedMusicPosition:F2}s, WasPaused: {musicWasPausedBefore}");
            
            // Stop current music (don't use PauseMusic as we're switching tracks)
            if (MusicManager.Instance.IsPlaying || MusicManager.Instance.IsPaused)
            {
                MusicManager.Instance.StopMusic(fadeOut: false);
            }
            
            // Play menu music
            MusicManager.Instance.PlayMainTrack(MusicManager.MainTrackType.MainAndCredits);
            Debug.Log("[PauseUI] Switched to main/credits music");
        }
        else
        {
            Debug.LogWarning("[PauseUI] MusicManager.Instance is null, cannot save music state");
        }
    }

    /// <summary>
    /// Restore the music state when closing pause menu
    /// </summary>
    private void RestoreMusicState()
    {
        if (MusicManager.Instance != null && savedMusicTrack != null)
        {
            Debug.Log($"[PauseUI] Restoring music state - Track: {savedMusicTrack.name}, Position: {savedMusicPosition:F2}s, WasPaused: {musicWasPausedBefore}");
            
            // Stop menu music
            MusicManager.Instance.StopMusic(fadeOut: false);
            
            // Restore the saved track
            MusicManager.Instance.MusicSource.clip = savedMusicTrack;
            MusicManager.Instance.SetMusicPosition(savedMusicPosition);
            
            // Resume playback unless it was paused before
            if (!musicWasPausedBefore)
            {
                MusicManager.Instance.MusicSource.Play();
                Debug.Log("[PauseUI] Resumed music playback");
            }
            else
            {
                Debug.Log("[PauseUI] Music was paused before, leaving it paused");
            }
            
            // Clear saved state
            savedMusicTrack = null;
        }
        else if (savedMusicTrack == null)
        {
            Debug.Log("[PauseUI] No saved music track to restore");
        }
    }

    /// <summary>
    /// Clean up animations when this UI is disabled or destroyed
    /// </summary>
    void OnDisable()
    {
        CleanupAnimations();
    }

    void OnDestroy()
    {
        CleanupAnimations();
    }

    /// <summary>
    /// Cancel all active animations and restore original states
    /// </summary>
    private void CleanupAnimations()
    {
        // Cancel all active tweens
        foreach (int tweenId in activeTweenIds)
        {
            LeanTween.cancel(tweenId);
        }
        activeTweenIds.Clear();

        // Restore original scales and positions
        foreach (var kvp in originalScales)
        {
            if (kvp.Key != null)
            {
                kvp.Key.transform.localScale = kvp.Value;
            }
        }

        foreach (var kvp in originalPositions)
        {
            if (kvp.Key != null)
            {
                RectTransform rect = kvp.Key.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchoredPosition = kvp.Value;
                }
            }
        }
    }
}
