using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
// using UGS services
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Main game controller that manages overall game flow, cross-scene utilities, and persistent data
/// </summary>
public class Main : MonoBehaviour
{
    /// <summary>
    /// Singleton instance of the Main class for cross-scene access
    /// </summary>
    public static Main Instance { get; private set; }

    // Leaderboard configuration
    private const string LeaderboardId = "SusGameMainLeaderboard";
    private bool loadGame = false;

    // Identity management
    public static bool IsAuthenticationReady { get; private set; } = false;
    public GameObject PauseUI; // Reference to the pause UI to manage its state across scenes
    private string savedPlayerIdentity = null; // Store the player's identity for saving/loading purposes
    private string playerDisplayName = null;
    
    /// <summary>
    /// Public property to access the current player's display name for UI
    /// </summary>
    public string CurrentPlayerDisplayName => GetCurrentPlayerDisplayName();
    
    // Player name cache for leaderboard display
    private Dictionary<string, string> playerNameCache = new Dictionary<string, string>();
    
    // Graphics settings persistence (survives PauseUI destruction)
    private bool graphicsSettingsInitialized = false;
    private float originalShadowDistance;
    private int originalCascadeCount;
    private Dictionary<Light, LightShadows> originalLightSettings = new Dictionary<Light, LightShadows>();
    private Dictionary<Light, float> originalLightIntensities = new Dictionary<Light, float>();
    private UniversalRenderPipelineAsset urpAsset;
    private bool shadowsEnabled = true;
    private bool renderScaleEnabled = true;
    private bool lightingEnabled = true;


    void Awake()
    {
        // Singleton setup with cross-scene persistence
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            Debugger.Log("[Main] Main instance created and set to DontDestroyOnLoad");
        }
        else
        {
            Debugger.Log("[Main] Duplicate Main instance destroyed");
            Destroy(gameObject);
        }
    }

    async void Start()
    {
        Debugger.Log("[Main] Main.Start() called - Initializing Unity Services...");
        await InitializeUnityServices();
        MusicManager.Instance.PlayMainTrack(MusicManager.MainTrackType.MainAndCredits);
    }

    public void OpenSettings()
    {
        GameObject p = Instantiate(PauseUI);
        p.GetComponent<Canvas>().sortingOrder = 100;
        p.GetComponent<PauseUI>().Load("GAME SETTINGS", false);
    }

    public void PauseGame()
    {
        GameObject p = Instantiate(PauseUI);
        p.GetComponent<Canvas>().sortingOrder = 100;
        GameState.Instance.PAUSED = true;

        System.Action unpauseAction = () => { GameState.Instance.PAUSED = false; };
        p.GetComponent<PauseUI>().Load("PAUSED GAME", true, unpauseAction);
    }

    


    #region Unity Services & Authentication

    /// <summary>
    /// Initialize Unity Services and handle authentication
    /// </summary>
    private async Task InitializeUnityServices()
    {
        try
        {
            // WebGL-safe: wait if initialization is already in progress to avoid concurrent calls
            if (UnityServices.State == ServicesInitializationState.Initializing)
            {
                Debugger.Log("[Main] Unity Services initializing, waiting...");
                while (UnityServices.State == ServicesInitializationState.Initializing)
                    await Task.Yield();
            }

            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                Debugger.Log("[Main] Initializing Unity Services...");
                var initTask = UnityServices.InitializeAsync();
                var timeoutTask = Task.Delay(10000);
                if (await Task.WhenAny(initTask, timeoutTask) == timeoutTask)
                    throw new System.TimeoutException("Unity Services initialization timed out after 10 seconds");
                await initTask;
                Debugger.Log("[Main] Unity Services initialized successfully");
            }

            await HandleAuthentication();
        }
        catch (System.Exception e)
        {
            Debugger.LogError($"[Main] Failed to initialize Unity Services: {e.Message}");
        }
    }

    /// <summary>
    /// Handle authentication - either restore saved identity or create new one
    /// </summary>
    private async Task HandleAuthentication()
    {
        try
        {
            Debugger.Log("[Main] Starting authentication process...");
            Debugger.Log($"[Main] Saved player identity: {savedPlayerIdentity ?? "null"}");
            Debugger.Log($"[Main] AuthenticationService.Instance.IsSignedIn: {AuthenticationService.Instance.IsSignedIn}");
            
            // Check if we have a saved identity to restore
            if (!string.IsNullOrEmpty(savedPlayerIdentity))
            {
                Debugger.Log($"[Main] Attempting to restore saved identity: {savedPlayerIdentity}");
                await RestorePlayerIdentity(savedPlayerIdentity);
            }
            else if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debugger.Log("[Main] No saved identity and not signed in, creating new identity...");
                await CreateNewIdentity();
            }
            else
            {
                Debugger.Log("[Main] Already signed in with existing identity");
            }

            // Generate player display name if not already set
            if (string.IsNullOrEmpty(playerDisplayName))
            {
                Debugger.Log("[Main] No display name set, generating new one...");
                GeneratePlayerDisplayName();
            }
            else
            {
                Debugger.Log($"[Main] Using existing display name: {playerDisplayName}");
            }

            IsAuthenticationReady = true;
            Debugger.Log($"[Main] Authentication completed successfully!");
            Debugger.Log($"[Main] Player ID: {AuthenticationService.Instance.PlayerId}");
            Debugger.Log($"[Main] Display Name: {playerDisplayName}");
            Debugger.Log($"[Main] IsAuthenticationReady: {IsAuthenticationReady}");
        }
        catch (System.Exception e)
        {
            Debugger.LogError($"[Main] Authentication failed: {e.Message}");
            Debugger.LogError($"[Main] Authentication stack trace: {e.StackTrace}");
            IsAuthenticationReady = false;
        }
    }

    /// <summary>
    /// Create a new anonymous identity and generate a display name
    /// </summary>
    private async Task CreateNewIdentity()
    {
        AuthenticationService.Instance.SignedIn -= OnAuthSignedIn;
        AuthenticationService.Instance.SignInFailed -= OnAuthSignInFailed;
        AuthenticationService.Instance.SignedIn += OnAuthSignedIn;
        AuthenticationService.Instance.SignInFailed += OnAuthSignInFailed;

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        Debugger.Log($"[Main] Signed in anonymously - ID: {AuthenticationService.Instance.PlayerId}");

        GeneratePlayerDisplayName();
    }

    /// <summary>
    /// Event handler for successful sign-in
    /// </summary>
    private void OnAuthSignedIn()
    {
        Debugger.Log("[Main] SignedIn event: " + AuthenticationService.Instance.PlayerId);
    }

    /// <summary>
    /// Event handler for sign-in failure
    /// </summary>
    private void OnAuthSignInFailed(RequestFailedException exception)
    {
        Debugger.LogError($"[Main] SignInFailed event: {exception.Message}");
        Debugger.LogError($"[Main] Error code: {exception.ErrorCode}");
    }

    /// <summary>
    /// Restore a previously saved player identity
    /// </summary>
    private async Task RestorePlayerIdentity(string playerId)
    {
        // Anonymous auth doesn't support identity restoration; create a fresh one
        Debugger.Log($"[Main] Cannot restore anonymous identity (was: {playerId}), creating new one");
        try
        {
            var signInTask = CreateNewIdentity();
            var timeoutTask = Task.Delay(8000);
            if (await Task.WhenAny(signInTask, timeoutTask) == timeoutTask)
                throw new System.TimeoutException("Sign-in timed out during identity restore");
            await signInTask;
        }
        catch (System.Exception e)
        {
            Debugger.LogError($"[Main] Failed to create identity during restore: {e.Message}");
            await CreateNewIdentity();
        }
    }

    /// <summary>
    /// Get the current player identity for saving
    /// </summary>
    public string GetCurrentPlayerIdentity()
    {
        string identity = AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId : null;
        Debugger.Log($"[Main] GetCurrentPlayerIdentity: {identity ?? "null"}");
        return identity;
    }

    /// <summary>
    /// Set the saved player identity (called during load)
    /// </summary>
    public void SetSavedPlayerIdentity(string playerId)
    {
        Debugger.Log($"[Main] Setting saved player identity: {playerId ?? "null"}");
        savedPlayerIdentity = playerId;
    }

    #endregion

    #region Player Name Management

    /// <summary>
    /// Generate a display name for the player
    /// </summary>
    private void GeneratePlayerDisplayName()
    {
        string[] adjectives = { "Swift", "Brave", "Clever", "Bold", "Quick", "Smart", "Fast", "Strong", "Wise", "Cool" };
        string[] nouns = { "Builder", "Mayor", "Planner", "Leader", "Hero", "Chief", "Boss", "Guide", "Pro", "Star" };
        
        string adjective = adjectives[Random.Range(0, adjectives.Length)];
        string noun = nouns[Random.Range(0, nouns.Length)];
        int number = Random.Range(10, 999);
        
        playerDisplayName = $"{adjective}{noun}{number}";
    }

    /// <summary>
    /// Get the current player's display name
    /// </summary>
    public string GetCurrentPlayerDisplayName()
    {
        return playerDisplayName ?? "Anonymous";
    }

    /// <summary>
    /// Get display name for any player ID (with caching)
    /// </summary>
    public string GetPlayerDisplayName(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
            return "Anonymous";

        if (playerId == GetCurrentPlayerIdentity())
            return GetCurrentPlayerDisplayName();

        if (playerNameCache.TryGetValue(playerId, out string cachedName))
            return cachedName;

        string displayName = $"Player_{playerId.Substring(0, Mathf.Min(6, playerId.Length))}";
        playerNameCache[playerId] = displayName;
        return displayName;
    }

    /// <summary>
    /// Set the player's display name
    /// </summary>
    public void SetPlayerDisplayName(string name)
    {
        playerDisplayName = name;
    }

    /// <summary>
    /// Set saved player display name (called during load)
    /// </summary>
    public void SetSavedPlayerDisplayName(string name)
    {
        playerDisplayName = name;
    }

    #endregion

    #region Game Flow Management

    /// <summary>
    /// Starts a new game session with a new anonymous account
    /// </summary>
    public void StartNewGame()
    {
        StartCoroutine(StartNewGameCoroutine());
    }

    /// <summary>
    /// Coroutine to handle new game start with identity creation
    /// </summary>
    private IEnumerator StartNewGameCoroutine()
    {
        if (GameState.Instance != null)
            GameState.Instance.ResetScore();

        var identityTask = CreateNewIdentityForNewGame();

        float timeoutTimer = 0f;
        float timeout = 5f;

        while (!identityTask.IsCompleted && timeoutTimer < timeout)
        {
            timeoutTimer += Time.deltaTime;
            yield return null;
        }

        if (timeoutTimer >= timeout)
            Debugger.LogWarning("[Main] Identity creation timed out - continuing without leaderboard");
        else if (identityTask.IsFaulted)
            Debugger.LogWarning($"[Main] Identity creation failed: {identityTask.Exception?.GetBaseException().Message}");

        if (MusicManager.Instance != null)
            MusicManager.Instance.PlayMainTrack(MusicManager.MainTrackType.Game);

        SceneTransition.i.SendToScene("Main");
    }

    /// <summary>
    /// Creates a new anonymous identity for a new game session
    /// </summary>
    private async Task CreateNewIdentityForNewGame()
    {
        Debugger.Log("[Main] Creating new identity for new game...");

        try
        {
            savedPlayerIdentity = null;
            playerDisplayName = null;
            playerNameCache.Clear();

            if (AuthenticationService.Instance.IsSignedIn)
            {
                try
                {
                    AuthenticationService.Instance.SignOut();
                    await Task.Yield(); // let browser process sign-out before proceeding
                }
                catch (System.Exception signOutEx)
                {
                    Debugger.LogWarning($"[Main] Sign out failed: {signOutEx.Message}");
                }
            }

            #if !UNITY_WEBGL || UNITY_EDITOR
            try
            {
                AuthenticationService.Instance.ClearSessionToken();
                await Task.Yield();
            }
            catch (System.Exception clearEx)
            {
                Debugger.LogWarning($"[Main] ClearSessionToken failed: {clearEx.Message}");
            }
            #endif

            var identityTask = CreateNewIdentity();
            var timeoutTask = Task.Delay(4000);

            if (await Task.WhenAny(identityTask, timeoutTask) == timeoutTask)
                throw new System.TimeoutException("Identity creation timed out after 4 seconds");

            await identityTask;
            Debugger.Log($"[Main] New identity ready - ID: {AuthenticationService.Instance.PlayerId}, Name: {playerDisplayName}");
        }
        catch (System.TimeoutException te)
        {
            Debugger.LogWarning($"[Main] Identity creation timed out: {te.Message}");
        }
        catch (System.Exception e)
        {
            Debugger.LogWarning($"[Main] Failed to create new identity: {e.Message}");
        }
    }

    /// <summary>
    /// Visit leaderboard scene
    /// </summary>
    public void VisitLeaderboard()
    {
        if (GameState.Instance != null && string.IsNullOrEmpty(GetCurrentPlayerIdentity()))
        {
            Debugger.LogWarning("[Main] No player identity, attempting to create one before leaderboard...");
            EnsureIdentityForLeaderboard();
        }

        if (MusicManager.Instance != null)
            MusicManager.Instance.PlayMainTrack(MusicManager.MainTrackType.Leaderboard);

        SceneTransition.i.SendToScene("Leaderboard");
    }

    // Replaces the .ContinueWith() pattern - async void is safe for fire-and-forget in Unity
    private async void EnsureIdentityForLeaderboard()
    {
        try
        {
            await CreateNewIdentityForNewGame();
            Debugger.Log($"[Main] Identity ready for leaderboard - ID: {GetCurrentPlayerIdentity()}, Name: {GetCurrentPlayerDisplayName()}");
        }
        catch (System.Exception e)
        {
            Debugger.LogWarning($"[Main] Failed to create identity for leaderboard: {e.Message}");
        }
    }


    public void ViewCredits()
    {
        if (MusicManager.Instance != null)
            MusicManager.Instance.PlayMainTrack(MusicManager.MainTrackType.MainAndCredits);

        SceneTransition.i.SendToScene("Credits");
    }

    /// <summary>
    /// Return to home scene
    /// </summary>
    public void ReturnHome()
    {
        if (MusicManager.Instance != null)
            MusicManager.Instance.PlayMainTrack(MusicManager.MainTrackType.MainAndCredits);

        SceneTransition.i.SendToScene("Home");
    }

    /// <summary>
    /// Loads an existing game session
    /// </summary>
    public void LoadGame()
    {
        loadGame = true;
        SceneTransition.i.SendToScene("Main");
    }

    /// <summary>
    /// Saves the current game session and submits high score to leaderboard
    /// </summary>
    public async void SaveGame()
    {
        if (GameState.Instance == null)
        {
            Debugger.LogWarning("[Main] Cannot save game - GameState not found");
            return;
        }

        SaveManager.Save();
        Debugger.Log("[Main] Game saved");

        await SubmitHighScoreToLeaderboard();
    }

    /// <summary>
    /// Submit high score (max population) to leaderboard without blocking save operations
    /// </summary>
    private async Task SubmitHighScoreToLeaderboard()
    {
        if (!IsAuthenticationReady || !AuthenticationService.Instance.IsSignedIn)
        {
            Debugger.LogWarning("[Main] Cannot submit score - not authenticated");
            return;
        }

        try
        {
            int highScore = GetMaxPopulation();
            if (highScore > 0)
                await AddScoreToLeaderboard(highScore);
        }
        catch (System.Exception e)
        {
            Debugger.LogWarning($"[Main] Failed to submit high score: {e.Message}");
        }
    }

    #endregion

    #region Leaderboard Integration

    /// <summary>
    /// Check leaderboard connection status
    /// </summary>
    public async Task<bool> CheckLeaderboardStatus()
    {
        if (!IsAuthenticationReady || !AuthenticationService.Instance.IsSignedIn)
        {
            Debugger.LogWarning("[Main] Cannot check leaderboard - authentication not ready");
            return false;
        }

        try
        {
            var checkTask = LeaderboardsService.Instance.GetScoresAsync(LeaderboardId, new GetScoresOptions { Limit = 1 });
            var timeoutTask = Task.Delay(8000);
            if (await Task.WhenAny(checkTask, timeoutTask) == timeoutTask)
            {
                Debugger.LogError("[Main] Leaderboard connection timed out");
                return false;
            }
            var testResponse = await checkTask;
            Debugger.Log($"[Main] Leaderboard connection OK. Entries: {testResponse?.Results?.Count ?? 0}");
            return true;
        }
        catch (System.Exception e)
        {
            Debugger.LogError($"[Main] Leaderboard connection failed: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Add score to leaderboard with player name stored as metadata - fixed for string metadata
    /// </summary>
    public async Task AddScoreToLeaderboard(int score)
    {
        if (!IsAuthenticationReady || !AuthenticationService.Instance.IsSignedIn)
        {
            Debugger.LogWarning("[Main] Cannot add score to leaderboard - authentication not ready");
            return;
        }

        try
        {
            string playerName = GetCurrentPlayerDisplayName();
            if (string.IsNullOrEmpty(playerName) || playerName == "null")
                playerName = "Anonymous";

            var metadata = new Dictionary<string, object> { { "playerName", playerName } };

            Debugger.Log($"[Main] Submitting score {score} to leaderboard as '{playerName}'...");

            var scoreTask = LeaderboardsService.Instance.AddPlayerScoreAsync(
                LeaderboardId,
                score,
                new AddPlayerScoreOptions { Metadata = metadata }
            );
            var timeoutTask = Task.Delay(8000);
            if (await Task.WhenAny(scoreTask, timeoutTask) == timeoutTask)
                throw new System.TimeoutException("Leaderboard score submission timed out after 8 seconds");

            var scoreResponse = await scoreTask;
            Debugger.Log($"[Main] Score submitted - Rank: {scoreResponse?.Rank ?? -1}, Score: {scoreResponse?.Score ?? 0}");
        }
        catch (System.Exception e)
        {
            Debugger.LogError($"[Main] Failed to add score to leaderboard: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// Add current game score to leaderboard
    /// </summary>
    public async Task AddCurrentScoreToLeaderboard()
    {
        await AddScoreToLeaderboard(GetScore());
    }

    /// <summary>
    /// Add high score (max population) to leaderboard
    /// </summary>
    public async Task AddHighScoreToLeaderboard()
    {
        await AddScoreToLeaderboard(GetMaxPopulation());
    }

    #endregion

    #region Score Management Delegation

    /// <summary>
    /// Gets the current game score from GameState
    /// </summary>
    public int GetScore()
    {
        return GameState.Instance?.GetScore() ?? 0;
    }

    /// <summary>
    /// Gets the maximum population reached from GameState
    /// </summary>
    public int GetMaxPopulation()
    {
        return GameState.Instance?.GetMaxPopulation() ?? 0;
    }

    #endregion

    #region Cross-Scene Utilities

    /// <summary>
    /// Called when a new scene loads
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debugger.Log($"[Main] Scene loaded: {scene.name}, LoadSceneMode: {mode}");

        // Initialize game state if we're in the game scene
        if (scene.name == "Main" && GameState.Instance != null)
        {
            Debugger.Log($"[Main] In Main scene, loadGame flag: {loadGame}");
            
            if (loadGame)
            {
                Debugger.Log("[Main] Loading saved game data...");
                SaveState data = SaveManager.Load();
                if (data == null)
                {
                    Debugger.LogWarning("[Main] No save data found");
                } 
                else if (GameState.Instance != null)
                {
                    Debugger.Log($"[Main] Save data found - Identity: {data.playerIdentity ?? "null"}, Name: {data.playerName ?? "null"}");
                    
                    // Set the saved identity before applying data
                    if (!string.IsNullOrEmpty(data.playerIdentity))
                    {
                        SetSavedPlayerIdentity(data.playerIdentity);
                    }
                    
                    // Set the saved player name
                    if (!string.IsNullOrEmpty(data.playerName))
                    {
                        SetSavedPlayerDisplayName(data.playerName);
                    }
                    
                    // print data as json again
                    Debugger.Log($"[Main] Loaded data: {JsonUtility.ToJson(data, true)}");
                    GameState.Instance.ApplyLoadedData(data);
                    Debugger.Log("[Main] Save data applied to GameState");
                }
                
                loadGame = false; // Reset flag
                Debugger.Log("[Main] LoadGame flag reset");
            }

            GameState.Instance.PAUSED = false;
            Debugger.Log("[Main] GameState.PAUSED set to false");

        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }


    public void onExitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    // TODO a proper fix for the loadGame below:

    void OnApplicationPause(bool pauseStatus)
    {
        if (!loadGame) return;
        // TODO: implement auto-save on pause
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!loadGame) return;
        // TODO: implement auto-save on focus loss
    }

    #endregion

    #region Graphics Settings Management

    /// <summary>
    /// Initialize graphics settings - captures original values
    /// </summary>
    public void InitializeGraphicsSettings()
    {
        if (graphicsSettingsInitialized)
        {
            Debugger.Log("[Main] Graphics settings already initialized");
            return;
        }

        Debugger.Log("[Main] Initializing graphics settings...");
        
        urpAsset = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
        
        if (urpAsset != null)
        {
            // Record URP Asset defaults
            originalShadowDistance = urpAsset.shadowDistance;
            originalCascadeCount = urpAsset.shadowCascadeCount;
            renderScaleEnabled = urpAsset.renderScale >= 0.99f;
            
            Debugger.Log($"[Main] Captured URP settings - ShadowDistance: {originalShadowDistance}, CascadeCount: {originalCascadeCount}, RenderScale: {urpAsset.renderScale}");
        }
        else
        {
            Debugger.LogWarning("[Main] URP Asset not found!");
        }

        // Record individual light defaults
        originalLightSettings.Clear();
        originalLightIntensities.Clear();
        
        Light[] allLights = GameObject.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light light in allLights)
        {
            originalLightSettings[light] = light.shadows;
            originalLightIntensities[light] = light.intensity;
        }
        
        Debugger.Log($"[Main] Captured {allLights.Length} light settings");
        graphicsSettingsInitialized = true;
    }

    /// <summary>
    /// Toggle shadows on/off
    /// </summary>
    public void ToggleShadows(bool enabled)
    {
        if (!graphicsSettingsInitialized)
        {
            InitializeGraphicsSettings();
        }

        shadowsEnabled = enabled;
        Debugger.Log($"[Main] ToggleShadows: {enabled}");

        if (urpAsset == null)
        {
            Debugger.LogWarning("[Main] Cannot toggle shadows - URP Asset is null");
            return;
        }

        // Toggle URP Asset shadow settings
        urpAsset.shadowDistance = enabled ? originalShadowDistance : 0f;
        urpAsset.shadowCascadeCount = enabled ? originalCascadeCount : 1;

        // Toggle individual light shadow settings
        foreach (var entry in originalLightSettings)
        {
            if (entry.Key != null)
            {
                entry.Key.shadows = enabled ? entry.Value : LightShadows.None;
            }
        }

        Debugger.Log($"[Main] Shadows {(enabled ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// Toggle render scale
    /// </summary>
    public void ToggleRenderScale(bool enabled)
    {
        if (!graphicsSettingsInitialized)
        {
            InitializeGraphicsSettings();
        }

        renderScaleEnabled = enabled;
        Debugger.Log($"[Main] ToggleRenderScale: {enabled}");

        if (urpAsset == null)
        {
            Debugger.LogWarning("[Main] Cannot toggle render scale - URP Asset is null");
            return;
        }

        urpAsset.renderScale = enabled ? 1.0f : 0.5f;
        Debugger.Log($"[Main] Render scale set to {urpAsset.renderScale}");
    }

    /// <summary>
    /// Toggle lighting on/off (dims lights significantly)
    /// </summary>
    public void ToggleLighting(bool enabled)
    {
        if (!graphicsSettingsInitialized)
        {
            InitializeGraphicsSettings();
        }

        lightingEnabled = enabled;
        Debugger.Log($"[Main] ToggleLighting: {enabled}");

        // Toggle light intensities
        foreach (var entry in originalLightIntensities)
        {
            if (entry.Key != null)
            {
                // When disabled, reduce to 10% of original intensity
                entry.Key.intensity = enabled ? entry.Value : entry.Value * 0.1f;
            }
        }

        Debugger.Log($"[Main] Lighting {(enabled ? "enabled" : "dimmed to 10%")}");
    }

    /// <summary>
    /// Get current shadow state
    /// </summary>
    public bool AreShadowsEnabled()
    {
        if (!graphicsSettingsInitialized)
        {
            return true; // Default enabled
        }
        return shadowsEnabled;
    }

    /// <summary>
    /// Get current render scale state
    /// </summary>
    public bool IsRenderScaleEnabled()
    {
        if (!graphicsSettingsInitialized)
        {
            return true; // Default enabled
        }
        return renderScaleEnabled;
    }

    /// <summary>
    /// Get current lighting state
    /// </summary>
    public bool IsLightingEnabled()
    {
        if (!graphicsSettingsInitialized)
        {
            return true; // Default enabled
        }
        return lightingEnabled;
    }

    #endregion

}
