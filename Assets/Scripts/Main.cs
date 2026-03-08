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
        // set camera to main camera
        // p.GetComponent<Canvas>().worldCamera = Camera.main;
        p.GetComponent<Canvas>().sortingOrder = 100; // Ensure it appears above other UI

        // call the setup function
        Debugger.Log("[Main] Opening settings menu...", p.GetComponent<PauseUI>());
        p.GetComponent<PauseUI>().Load("GAME SETTINGS", false);
    }

    public void PauseGame()
    {
        GameObject p = Instantiate(PauseUI);
        // set camera to main camera
        p.GetComponent<Canvas>().sortingOrder = 100; // Ensure it appears above other UI

        // call the setup function
        Debugger.Log("[Main] Opening pause menu...", p.GetComponent<PauseUI>());
        GameState.Instance.PAUSED = true;

        // create unpause game action callback
        System.Action unpauseAction = () =>
        {
            Debugger.Log("[Main] Unpausing game from pause menu callback");
            GameState.Instance.PAUSED = false;
        };

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
            Debugger.Log("[Main] Checking Unity Services state...");
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                Debugger.Log("[Main] Unity Services not initialized, initializing now...");
                await UnityServices.InitializeAsync();
                Debugger.Log("[Main] Unity Services initialized successfully");
            }
            else
            {
                Debugger.Log("[Main] Unity Services already initialized");
            }

            await HandleAuthentication();
        }
        catch (System.Exception e)
        {
            Debugger.LogError($"[Main] Failed to initialize Unity Services: {e.Message}");
            Debugger.LogError($"[Main] Stack trace: {e.StackTrace}");
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
        Debugger.Log("[Main] Creating new anonymous identity...");
        
        // Remove existing event handlers to prevent duplicates
        AuthenticationService.Instance.SignedIn -= OnAuthSignedIn;
        AuthenticationService.Instance.SignInFailed -= OnAuthSignInFailed;
        
        // Add event handlers
        AuthenticationService.Instance.SignedIn += OnAuthSignedIn;
        AuthenticationService.Instance.SignInFailed += OnAuthSignInFailed;

        Debugger.Log("[Main] Calling SignInAnonymouslyAsync...");
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        Debugger.Log($"[Main] Anonymous sign-in completed. Player ID: {AuthenticationService.Instance.PlayerId}");
        
        // Generate display name for this new identity
        GeneratePlayerDisplayName();
        Debugger.Log($"[Main] Generated display name for new identity: {playerDisplayName}");
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
        try
        {
            // Note: With anonymous authentication, we can't directly restore a specific identity
            // This would require switching to a different authentication method
            // For now, we'll create a new identity but log the attempt
            Debugger.Log($"[Main] Attempting to restore identity: {playerId}");
            Debugger.LogWarning("[Main] Cannot restore anonymous identity, creating new one instead");
            await CreateNewIdentity();
        }
        catch (System.Exception e)
        {
            Debugger.LogError($"[Main] Failed to restore identity {playerId}: {e.Message}");
            Debugger.Log("[Main] Falling back to creating new identity");
            await CreateNewIdentity(); // Fallback to new identity
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
        Debugger.Log($"[Main] Generated new display name: {playerDisplayName}");
    }

    /// <summary>
    /// Get the current player's display name
    /// </summary>
    public string GetCurrentPlayerDisplayName()
    {
        string displayName = playerDisplayName ?? "Anonymous";
        Debugger.Log($"[Main] GetCurrentPlayerDisplayName: {displayName}");
        return displayName;
    }

    /// <summary>
    /// Get display name for any player ID (with caching)
    /// </summary>
    public string GetPlayerDisplayName(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debugger.Log("[Main] GetPlayerDisplayName: playerId is null/empty, returning Anonymous");
            return "Anonymous";
        }
        
        // If it's the current player, return their display name
        if (playerId == GetCurrentPlayerIdentity())
        {
            string currentName = GetCurrentPlayerDisplayName();
            Debugger.Log($"[Main] GetPlayerDisplayName: Current player, returning {currentName}");
            return currentName;
        }
        
        // Check cache first
        if (playerNameCache.TryGetValue(playerId, out string cachedName))
        {
            Debugger.Log($"[Main] GetPlayerDisplayName: Found cached name for {playerId}: {cachedName}");
            return cachedName;
        }
        
        // For other players, we'd need to implement a proper lookup system
        // For now, just return a shortened version of their ID
        string displayName = $"Player_{playerId.Substring(0, Mathf.Min(6, playerId.Length))}";
        playerNameCache[playerId] = displayName;
        Debugger.Log($"[Main] GetPlayerDisplayName: Generated name for {playerId}: {displayName}");
        return displayName;
    }

    /// <summary>
    /// Set the player's display name
    /// </summary>
    public void SetPlayerDisplayName(string name)
    {
        Debugger.Log($"[Main] Setting player display name to: {name ?? "null"}");
        playerDisplayName = name;
    }

    /// <summary>
    /// Set saved player display name (called during load)
    /// </summary>
    public void SetSavedPlayerDisplayName(string name)
    {
        Debugger.Log($"[Main] Setting saved player display name to: {name ?? "null"}");
        playerDisplayName = name;
    }

    #endregion

    #region Game Flow Management

    /// <summary>
    /// Starts a new game session with a new anonymous account
    /// </summary>
    public void StartNewGame()
    {
        Debugger.Log("[Main] Starting new game...");
        StartCoroutine(StartNewGameCoroutine());
    }

    /// <summary>
    /// Coroutine to handle new game start with identity creation
    /// </summary>
    private IEnumerator StartNewGameCoroutine()
    {
        // Reset game state if it exists
        if (GameState.Instance != null)
        {
            Debugger.Log("[Main] Resetting GameState score for new game");
            GameState.Instance.ResetScore();
        }
        
        // Sign out and create new identity for fresh leaderboard entry
        var identityTask = CreateNewIdentityForNewGame();
        
        // Wait for completion with short timeout for WebGL
        float timeoutTimer = 0f;
        float timeout = 5f; // Reduced from 20s - don't block game start
        
        while (!identityTask.IsCompleted && timeoutTimer < timeout)
        {
            timeoutTimer += Time.deltaTime;
            yield return null;
        }
        
        if (timeoutTimer >= timeout)
        {
            Debugger.LogWarning("[Main] Identity creation timed out - continuing without leaderboard");
            Debugger.LogWarning("[Main] You can still play the game. Leaderboard features will be unavailable.");
        }
        else if (identityTask.IsFaulted)
        {
            Debugger.LogWarning($"[Main] Identity creation failed: {identityTask.Exception?.GetBaseException().Message}");
            Debugger.LogWarning("[Main] Game will continue without leaderboard features");
        }
        else
        {
            Debugger.Log($"[Main] Identity ready - ID: {GetCurrentPlayerIdentity()}, Name: {GetCurrentPlayerDisplayName()}");
        }
        
        // Play game music track
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayMainTrack(MusicManager.MainTrackType.Game);
        }
        
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
            // Clear saved identity and display name first
            savedPlayerIdentity = null;
            playerDisplayName = null;
            playerNameCache.Clear();
            Debugger.Log("[Main] Cleared saved identity, display name, and name cache");
            
            // Sign out if already signed in
            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debugger.Log("[Main] Signing out from current account");
                try
                {
                    AuthenticationService.Instance.SignOut();
                    await Task.Delay(200); // Short delay for sign out to complete
                    Debugger.Log("[Main] Sign out completed");
                }
                catch (System.Exception signOutEx)
                {
                    Debugger.LogWarning($"[Main] Sign out failed: {signOutEx.Message}");
                }
            }
            
            // Clear the session token - skip on WebGL to prevent freezing
            #if !UNITY_WEBGL || UNITY_EDITOR
            try
            {
                Debugger.Log("[Main] Clearing session token (Editor/Standalone only)");
                AuthenticationService.Instance.ClearSessionToken();
                await Task.Delay(200);
            }
            catch (System.Exception clearEx)
            {
                Debugger.LogWarning($"[Main] ClearSessionToken failed: {clearEx.Message}");
            }
            #else
            Debugger.Log("[Main] Skipping ClearSessionToken on WebGL");
            #endif
            
            // Create new anonymous account (this will also generate a display name)
            Debugger.Log("[Main] Creating new anonymous account");
            
            // Add timeout protection for WebGL
            var identityTask = CreateNewIdentity();
            var timeoutTask = Task.Delay(4000); // 4 second timeout - fast fail for better UX
            
            var completedTask = await Task.WhenAny(identityTask, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                throw new System.TimeoutException("Identity creation timed out after 4 seconds");
            }
            
            // Check for exceptions
            await identityTask; // This will throw if the task faulted
            
            Debugger.Log($"[Main] New identity created - Player ID: {AuthenticationService.Instance.PlayerId}, Display Name: {playerDisplayName}");
        }
        catch (System.TimeoutException te)
        {
            Debugger.LogWarning($"[Main] Identity creation timed out: {te.Message}");
            Debugger.LogWarning("[Main] This may indicate network issues - game will continue without leaderboard");
        }
        catch (System.Exception e)
        {
            Debugger.LogWarning($"[Main] Failed to create new identity: {e.Message}");
            Debugger.LogWarning("[Main] Game will continue without leaderboard features");
            
            // Log details for debugging
            if (e.InnerException != null)
            {
                Debugger.Log($"[Main] Inner exception: {e.InnerException.Message}");
            }
        }
    }

    /// <summary>
    /// Visit leaderboard scene
    /// </summary>
    public void VisitLeaderboard()
    {
        Debugger.Log("[Main] Attempting to visit leaderboard...");
        
        // ensure user has an identity before visiting leaderboard
        if (GameState.Instance != null)
        {
            string playerId = GetCurrentPlayerIdentity();
            if (string.IsNullOrEmpty(playerId))
            {
                Debugger.LogWarning("[Main] Cannot visit leaderboard - no player identity found");
                return;
            }
            Debugger.Log($"[Main] Player identity verified: {playerId}");
        }
        
        // Play leaderboard music track
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayMainTrack(MusicManager.MainTrackType.Leaderboard);
        }
        
        Debugger.Log($"[Main] IsAuthenticationReady: {IsAuthenticationReady}");
        SceneTransition.i.SendToScene("Leaderboard");
    }


    public void ViewCredits()
    {
        Debugger.Log("[Main] Attempting to visit credits...");
        
        // Play main/credits music track
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayMainTrack(MusicManager.MainTrackType.MainAndCredits);
        }
        
        SceneTransition.i.SendToScene("Credits");
    }

    /// <summary>
    /// Return to home scene
    /// </summary>
    public void ReturnHome()
    {
        Debugger.Log("[Main] Returning to home scene");
        
        // Play main/credits music track for home
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayMainTrack(MusicManager.MainTrackType.MainAndCredits);
        }
        
        SceneTransition.i.SendToScene("Home");
    }

    /// <summary>
    /// Loads an existing game session
    /// </summary>
    public void LoadGame()
    {
        Debugger.Log("[Main] Loading existing game session");
        loadGame = true;
        SceneTransition.i.SendToScene("Main");
    }

    /// <summary>
    /// Saves the current game session and submits high score to leaderboard
    /// </summary>
    public async void SaveGame()
    {
        Debugger.Log("[Main] SaveGame called");
        
        if (GameState.Instance == null)
        {
            Debugger.LogWarning("[Main] Cannot save game - GameState not found");
            return;
        }

        Debugger.Log($"[Main] Current game stats - Score: {GetScore()}, Max Population: {GetMaxPopulation()}");

        // Save game data first
        SaveManager.Save();
        Debugger.Log("[Main] Game saved successfully to file/PlayerPrefs");

        // Submit high score (max population) to leaderboard (don't block save if this fails)
        await SubmitHighScoreToLeaderboard();
    }

    /// <summary>
    /// Submit high score (max population) to leaderboard without blocking save operations
    /// </summary>
    private async Task SubmitHighScoreToLeaderboard()
    {
        Debugger.Log("[Main] SubmitHighScoreToLeaderboard called");
        Debugger.Log($"[Main] Authentication status - IsAuthenticationReady: {IsAuthenticationReady}, IsSignedIn: {AuthenticationService.Instance.IsSignedIn}");
        
        try
        {
            if (IsAuthenticationReady && AuthenticationService.Instance.IsSignedIn)
            {
                int highScore = GetMaxPopulation(); // Submit high score (max population) instead of current score
                Debugger.Log($"[Main] Preparing to submit high score: {highScore}");
                
                if (highScore > 0)
                {
                    await AddScoreToLeaderboard(highScore);
                    Debugger.Log($"[Main] High score {highScore} submitted to leaderboard successfully");
                }
                else
                {
                    Debugger.Log("[Main] No high score to submit to leaderboard (score is 0)");
                }
            }
            else
            {
                Debugger.LogWarning($"[Main] Cannot submit score - authentication not ready. IsAuthenticationReady: {IsAuthenticationReady}, IsSignedIn: {AuthenticationService.Instance.IsSignedIn}");
            }
        }
        catch (System.Exception e)
        {
            // Don't let leaderboard errors break the save process
            Debugger.LogWarning($"[Main] Failed to submit high score to leaderboard: {e.Message}");
            Debugger.LogWarning($"[Main] Leaderboard submission stack trace: {e.StackTrace}");
        }
    }

    #endregion

    #region Leaderboard Integration

    /// <summary>
    /// Check leaderboard connection status
    /// </summary>
    public async Task<bool> CheckLeaderboardStatus()
    {
        Debugger.Log("[Main] Checking leaderboard connection status...");
        
        if (!IsAuthenticationReady || !AuthenticationService.Instance.IsSignedIn)
        {
            Debugger.LogWarning("[Main] Cannot check leaderboard - authentication not ready");
            return false;
        }

        try
        {
            // Try to get a minimal leaderboard response to test connectivity
            var testResponse = await LeaderboardsService.Instance.GetScoresAsync(LeaderboardId, new GetScoresOptions { Limit = 1 });
            Debugger.Log($"[Main] Leaderboard connection successful. Response entries: {testResponse?.Results?.Count ?? 0}");
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
        Debugger.Log($"[Main] AddScoreToLeaderboard called with score: {score}");
        Debugger.Log($"[Main] Authentication check - IsAuthenticationReady: {IsAuthenticationReady}, IsSignedIn: {AuthenticationService.Instance.IsSignedIn}");
        
        if (!IsAuthenticationReady || !AuthenticationService.Instance.IsSignedIn)
        {
            Debugger.LogWarning("[Main] Cannot add score to leaderboard - authentication not ready");
            return;
        }

        try
        {
            Debugger.Log($"[Main] Submitting score to leaderboard ID: {LeaderboardId}");
            
            // Get current player display name for metadata with validation
            string playerName = GetCurrentPlayerDisplayName();
            Debugger.Log($"[Main] Retrieved player name for metadata: '{playerName}'");
            
            // Validate player name before using it
            if (string.IsNullOrEmpty(playerName) || playerName == "null")
            {
                Debugger.LogWarning("[Main] Player name is null or empty, using fallback");
                playerName = "Anonymous";
            }
            
            Debugger.Log($"[Main] Final player name for metadata: '{playerName}'");
            
            // Create metadata dictionary - Unity Leaderboards expects Dictionary<string, object> for submission
            Dictionary<string, object> metadata = null;
            
            try
            {
                metadata = new Dictionary<string, object>
                {
                    { "playerName", playerName }
                };
                
                Debugger.Log($"[Main] Created metadata dictionary with {metadata.Count} entries");
                Debugger.Log($"[Main] Metadata playerName value: '{metadata["playerName"]}'");
                Debugger.Log($"[Main] Metadata playerName type: {metadata["playerName"]?.GetType().Name ?? "null"}");
            }
            catch (System.Exception metadataException)
            {
                Debugger.LogError($"[Main] Failed to create metadata dictionary: {metadataException.Message}");
                // Fallback to simple metadata creation
                metadata = new Dictionary<string, object> { { "playerName", "Anonymous" } };
            }
            
            // Submit score with metadata containing player name
            Debugger.Log($"[Main] Submitting to leaderboard with metadata: playerName='{metadata["playerName"]}'");
            
            var scoreResponse = await LeaderboardsService.Instance.AddPlayerScoreAsync(
                LeaderboardId, 
                score, 
                new AddPlayerScoreOptions 
                { 
                    Metadata = metadata 
                }
            );
            
            Debugger.Log("[Main] Score submission with metadata successful!");
            Debugger.Log($"[Main] Response PlayerId: {scoreResponse?.PlayerId ?? "null"}");
            Debugger.Log($"[Main] Response Score: {scoreResponse?.Score ?? 0}");
            Debugger.Log($"[Main] Response Rank: {scoreResponse?.Rank ?? -1}");
            
            // Log metadata from response - fixed to handle string metadata
            if (!string.IsNullOrEmpty(scoreResponse?.Metadata))
            {
                Debugger.Log($"[Main] Response metadata: {scoreResponse.Metadata}");
            }
            else
            {
                Debugger.LogWarning("[Main] Response metadata is null or empty");
            }
            
            // Attempt to log full response with better error handling
            try
            {
                string jsonResponse = JsonUtility.ToJson(scoreResponse);
                Debugger.Log($"[Main] Full response JSON: {jsonResponse}");
            }
            catch (System.ArgumentException jsonException)
            {
                Debugger.LogWarning($"[Main] Failed to serialize response to JSON: {jsonException.Message}");
                Debugger.Log($"[Main] Score response (ToString): {scoreResponse?.ToString() ?? "null"}");
            }
            catch (System.Exception jsonException)
            {
                Debugger.LogWarning($"[Main] Unexpected error serializing response: {jsonException.Message}");
            }
        }
        catch (System.Exception e)
        {
            Debugger.LogError($"[Main] Failed to add score to leaderboard: {e.Message}");
            Debugger.LogError($"[Main] Leaderboard error stack trace: {e.StackTrace}");
            
            // Log additional context for debugging
            Debugger.LogError($"[Main] Score being submitted: {score}");
            Debugger.LogError($"[Main] Player ID: {AuthenticationService.Instance?.PlayerId ?? "null"}");
            Debugger.LogError($"[Main] Player Display Name: {GetCurrentPlayerDisplayName()}");
            
            throw; // Re-throw to allow caller to handle
        }
    }

    /// <summary>
    /// Add current game score to leaderboard
    /// </summary>
    public async Task AddCurrentScoreToLeaderboard()
    {
        int currentScore = GetScore();
        Debugger.Log($"[Main] AddCurrentScoreToLeaderboard - submitting current score: {currentScore}");
        await AddScoreToLeaderboard(currentScore);
    }

    /// <summary>
    /// Add high score (max population) to leaderboard
    /// </summary>
    public async Task AddHighScoreToLeaderboard()
    {
        int highScore = GetMaxPopulation();
        Debugger.Log($"[Main] AddHighScoreToLeaderboard - submitting high score: {highScore}");
        await AddScoreToLeaderboard(highScore);
    }

    #endregion

    #region Score Management Delegation

    /// <summary>
    /// Gets the current game score from GameState
    /// </summary>
    public int GetScore()
    {
        int score = GameState.Instance?.GetScore() ?? 0;
        Debugger.Log($"[Main] GetScore: {score}");
        return score;
    }

    /// <summary>
    /// Gets the maximum population reached from GameState
    /// </summary>
    public int GetMaxPopulation()
    {
        int maxPop = GameState.Instance?.GetMaxPopulation() ?? 0;
        Debugger.Log($"[Main] GetMaxPopulation: {maxPop}");
        return maxPop;
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
        Debugger.Log("[Main] OnDestroy called");
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Debugger.Log("[Main] Unsubscribed from SceneManager.sceneLoaded");
        }
    }


    public void onExitGame()
    {
        Debugger.Log("[Main] onExitGame called - saving and exiting...");

        // then quit application
        Application.Quit();

        // if the application is running in the editor, stop play mode instead of quitting
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        Debugger.Log("[Main] Editor play mode stopped");
        #endif
    }

    // TODO a proper fix for the loadGame below:

    void OnApplicationPause(bool pauseStatus)
    {
        Debugger.Log($"[Main] OnApplicationPause: {pauseStatus}, loadGame: {loadGame}");

        if (!loadGame) return;

        if (pauseStatus) 
        {
            Debugger.Log("[Main] App paused, saving game");
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        Debugger.Log($"[Main] OnApplicationFocus: {hasFocus}, loadGame: {loadGame}");
        
        if (!loadGame) return;

        if (!hasFocus) 
        {
            Debugger.Log("[Main] App lost focus, saving game");
        }
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
