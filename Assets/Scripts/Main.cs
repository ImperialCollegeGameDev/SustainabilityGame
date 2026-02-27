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
    private int anonAuth = -1;
    
    // Identity management
    public static bool IsAuthenticationReady { get; private set; } = false;
    private string savedPlayerIdentity = null;
    private string playerDisplayName = null;
    
    // Player name cache for leaderboard display
    private Dictionary<string, string> playerNameCache = new Dictionary<string, string>();

    //// Events for game flow management
    //public System.Action OnGameStarted;
    //public System.Action OnGameLoaded;
    //public System.Action OnGameSaved;

    void Awake()
    {
        // Singleton setup with cross-scene persistence
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject.transform.parent.gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            Debug.Log("[Main] Main instance created and set to DontDestroyOnLoad");
        }
        else
        {
            Debug.Log("[Main] Duplicate Main instance destroyed");
            Destroy(gameObject);
        }
    }

    async void Start()
    {
        Debug.Log("[Main] Main.Start() called - Initializing Unity Services...");
        await InitializeUnityServices();
    }

    void Update()
    {
        
    }

    #region Unity Services & Authentication

    /// <summary>
    /// Initialize Unity Services and handle authentication
    /// </summary>
    private async Task InitializeUnityServices()
    {
        try
        {
            Debug.Log("[Main] Checking Unity Services state...");
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                Debug.Log("[Main] Unity Services not initialized, initializing now...");
                await UnityServices.InitializeAsync();
                Debug.Log("[Main] Unity Services initialized successfully");
            }
            else
            {
                Debug.Log("[Main] Unity Services already initialized");
            }

            await HandleAuthentication();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Main] Failed to initialize Unity Services: {e.Message}");
            Debug.LogError($"[Main] Stack trace: {e.StackTrace}");
        }
    }

    /// <summary>
    /// Handle authentication - either restore saved identity or create new one
    /// </summary>
    private async Task HandleAuthentication()
    {
        try
        {
            Debug.Log("[Main] Starting authentication process...");
            Debug.Log($"[Main] Saved player identity: {savedPlayerIdentity ?? "null"}");
            Debug.Log($"[Main] AuthenticationService.Instance.IsSignedIn: {AuthenticationService.Instance.IsSignedIn}");
            
            // Check if we have a saved identity to restore
            if (!string.IsNullOrEmpty(savedPlayerIdentity))
            {
                Debug.Log($"[Main] Attempting to restore saved identity: {savedPlayerIdentity}");
                await RestorePlayerIdentity(savedPlayerIdentity);
            }
            else if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("[Main] No saved identity and not signed in, creating new identity...");
                await CreateNewIdentity();
            }
            else
            {
                Debug.Log("[Main] Already signed in with existing identity");
            }

            // Generate player display name if not already set
            if (string.IsNullOrEmpty(playerDisplayName))
            {
                Debug.Log("[Main] No display name set, generating new one...");
                GeneratePlayerDisplayName();
            }
            else
            {
                Debug.Log($"[Main] Using existing display name: {playerDisplayName}");
            }

            IsAuthenticationReady = true;
            Debug.Log($"[Main] Authentication completed successfully!");
            Debug.Log($"[Main] Player ID: {AuthenticationService.Instance.PlayerId}");
            Debug.Log($"[Main] Display Name: {playerDisplayName}");
            Debug.Log($"[Main] IsAuthenticationReady: {IsAuthenticationReady}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Main] Authentication failed: {e.Message}");
            Debug.LogError($"[Main] Authentication stack trace: {e.StackTrace}");
            IsAuthenticationReady = false;
        }
    }

    /// <summary>
    /// Create a new anonymous identity
    /// </summary>
    private async Task CreateNewIdentity()
    {
        Debug.Log("[Main] Creating new anonymous identity...");
        
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("[Main] SignedIn event: " + AuthenticationService.Instance.PlayerId);
        };
        
        AuthenticationService.Instance.SignInFailed += s =>
        {
            Debug.LogError($"[Main] SignInFailed event: {s}");
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        Debug.Log($"[Main] Anonymous sign-in completed. Player ID: {AuthenticationService.Instance.PlayerId}");
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
            Debug.Log($"[Main] Attempting to restore identity: {playerId}");
            Debug.LogWarning("[Main] Cannot restore anonymous identity, creating new one instead");
            await CreateNewIdentity();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Main] Failed to restore identity {playerId}: {e.Message}");
            Debug.Log("[Main] Falling back to creating new identity");
            await CreateNewIdentity(); // Fallback to new identity
        }
    }

    /// <summary>
    /// Get the current player identity for saving
    /// </summary>
    public string GetCurrentPlayerIdentity()
    {
        string identity = AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId : null;
        Debug.Log($"[Main] GetCurrentPlayerIdentity: {identity ?? "null"}");
        return identity;
    }

    /// <summary>
    /// Set the saved player identity (called during load)
    /// </summary>
    public void SetSavedPlayerIdentity(string playerId)
    {
        Debug.Log($"[Main] Setting saved player identity: {playerId ?? "null"}");
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
        Debug.Log($"[Main] Generated new display name: {playerDisplayName}");
    }

    /// <summary>
    /// Get the current player's display name
    /// </summary>
    public string GetCurrentPlayerDisplayName()
    {
        string displayName = playerDisplayName ?? "Anonymous";
        Debug.Log($"[Main] GetCurrentPlayerDisplayName: {displayName}");
        return displayName;
    }

    /// <summary>
    /// Get display name for any player ID (with caching)
    /// </summary>
    public string GetPlayerDisplayName(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.Log("[Main] GetPlayerDisplayName: playerId is null/empty, returning Anonymous");
            return "Anonymous";
        }
        
        // If it's the current player, return their display name
        if (playerId == GetCurrentPlayerIdentity())
        {
            string currentName = GetCurrentPlayerDisplayName();
            Debug.Log($"[Main] GetPlayerDisplayName: Current player, returning {currentName}");
            return currentName;
        }
        
        // Check cache first
        if (playerNameCache.TryGetValue(playerId, out string cachedName))
        {
            Debug.Log($"[Main] GetPlayerDisplayName: Found cached name for {playerId}: {cachedName}");
            return cachedName;
        }
        
        // For other players, we'd need to implement a proper lookup system
        // For now, just return a shortened version of their ID
        string displayName = $"Player_{playerId.Substring(0, Mathf.Min(6, playerId.Length))}";
        playerNameCache[playerId] = displayName;
        Debug.Log($"[Main] GetPlayerDisplayName: Generated name for {playerId}: {displayName}");
        return displayName;
    }

    /// <summary>
    /// Set the player's display name
    /// </summary>
    public void SetPlayerDisplayName(string name)
    {
        Debug.Log($"[Main] Setting player display name to: {name ?? "null"}");
        playerDisplayName = name;
    }

    /// <summary>
    /// Set saved player display name (called during load)
    /// </summary>
    public void SetSavedPlayerDisplayName(string name)
    {
        Debug.Log($"[Main] Setting saved player display name to: {name ?? "null"}");
        playerDisplayName = name;
    }

    #endregion

    #region Game Flow Management

    /// <summary>
    /// Starts a new game session
    /// </summary>
    public void StartNewGame()
    {
        Debug.Log("[Main] Starting new game...");
        
        // Reset game state if it exists
        if (GameState.Instance != null)
        {
            Debug.Log("[Main] Resetting GameState score for new game");
            GameState.Instance.ResetScore();
        }
        
        // Clear any saved identity for fresh start
        savedPlayerIdentity = null;
        Debug.Log("[Main] Cleared saved identity for fresh start");
        
        SceneTransition.i.SendToScene("Main");
    }

    /// <summary>
    /// Visit leaderboard scene
    /// </summary>
    public void VisitLeaderboard()
    {
        Debug.Log("[Main] Attempting to visit leaderboard...");
        
        // ensure user has an identity before visiting leaderboard
        if (GameState.Instance != null)
        {
            string playerId = GetCurrentPlayerIdentity();
            if (string.IsNullOrEmpty(playerId))
            {
                Debug.LogWarning("[Main] Cannot visit leaderboard - no player identity found");
                return;
            }
            Debug.Log($"[Main] Player identity verified: {playerId}");
        }
        
        Debug.Log($"[Main] IsAuthenticationReady: {IsAuthenticationReady}");
        SceneTransition.i.SendToScene("Leaderboard");
    }

    /// <summary>
    /// Return to home scene
    /// </summary>
    public void ReturnHome()
    {
        Debug.Log("[Main] Returning to home scene");
        SceneTransition.i.SendToScene("Home");
    }

    /// <summary>
    /// Loads an existing game session
    /// </summary>
    public void LoadGame()
    {
        Debug.Log("[Main] Loading existing game session");
        loadGame = true;
        SceneTransition.i.SendToScene("Main");
    }

    /// <summary>
    /// Saves the current game session and submits high score to leaderboard
    /// </summary>
    public async void SaveGame()
    {
        Debug.Log("[Main] SaveGame called");
        
        if (GameState.Instance == null)
        {
            Debug.LogWarning("[Main] Cannot save game - GameState not found");
            return;
        }

        Debug.Log($"[Main] Current game stats - Score: {GetScore()}, Max Population: {GetMaxPopulation()}");

        // Save game data first
        SaveManager.Save();
        Debug.Log("[Main] Game saved successfully to file/PlayerPrefs");

        // Submit high score (max population) to leaderboard (don't block save if this fails)
        await SubmitHighScoreToLeaderboard();
    }

    /// <summary>
    /// Submit high score (max population) to leaderboard without blocking save operations
    /// </summary>
    private async Task SubmitHighScoreToLeaderboard()
    {
        Debug.Log("[Main] SubmitHighScoreToLeaderboard called");
        Debug.Log($"[Main] Authentication status - IsAuthenticationReady: {IsAuthenticationReady}, IsSignedIn: {AuthenticationService.Instance.IsSignedIn}");
        
        try
        {
            if (IsAuthenticationReady && AuthenticationService.Instance.IsSignedIn)
            {
                int highScore = GetMaxPopulation(); // Submit high score (max population) instead of current score
                Debug.Log($"[Main] Preparing to submit high score: {highScore}");
                
                if (highScore > 0)
                {
                    await AddScoreToLeaderboard(highScore);
                    Debug.Log($"[Main] High score {highScore} submitted to leaderboard successfully");
                }
                else
                {
                    Debug.Log("[Main] No high score to submit to leaderboard (score is 0)");
                }
            }
            else
            {
                Debug.LogWarning($"[Main] Cannot submit score - authentication not ready. IsAuthenticationReady: {IsAuthenticationReady}, IsSignedIn: {AuthenticationService.Instance.IsSignedIn}");
            }
        }
        catch (System.Exception e)
        {
            // Don't let leaderboard errors break the save process
            Debug.LogWarning($"[Main] Failed to submit high score to leaderboard: {e.Message}");
            Debug.LogWarning($"[Main] Leaderboard submission stack trace: {e.StackTrace}");
        }
    }

    #endregion

    #region Leaderboard Integration

    /// <summary>
    /// Check leaderboard connection status
    /// </summary>
    public async Task<bool> CheckLeaderboardStatus()
    {
        Debug.Log("[Main] Checking leaderboard connection status...");
        
        if (!IsAuthenticationReady || !AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning("[Main] Cannot check leaderboard - authentication not ready");
            return false;
        }

        try
        {
            // Try to get a minimal leaderboard response to test connectivity
            var testResponse = await LeaderboardsService.Instance.GetScoresAsync(LeaderboardId, new GetScoresOptions { Limit = 1 });
            Debug.Log($"[Main] Leaderboard connection successful. Response entries: {testResponse?.Results?.Count ?? 0}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Main] Leaderboard connection failed: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Add score to leaderboard with player name stored as metadata - fixed for string metadata
    /// </summary>
    public async Task AddScoreToLeaderboard(int score)
    {
        Debug.Log($"[Main] AddScoreToLeaderboard called with score: {score}");
        Debug.Log($"[Main] Authentication check - IsAuthenticationReady: {IsAuthenticationReady}, IsSignedIn: {AuthenticationService.Instance.IsSignedIn}");
        
        if (!IsAuthenticationReady || !AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning("[Main] Cannot add score to leaderboard - authentication not ready");
            return;
        }

        try
        {
            Debug.Log($"[Main] Submitting score to leaderboard ID: {LeaderboardId}");
            
            // Get current player display name for metadata with validation
            string playerName = GetCurrentPlayerDisplayName();
            Debug.Log($"[Main] Retrieved player name for metadata: '{playerName}'");
            
            // Validate player name before using it
            if (string.IsNullOrEmpty(playerName) || playerName == "null")
            {
                Debug.LogWarning("[Main] Player name is null or empty, using fallback");
                playerName = "Anonymous";
            }
            
            Debug.Log($"[Main] Final player name for metadata: '{playerName}'");
            
            // Create metadata dictionary - Unity Leaderboards expects Dictionary<string, object> for submission
            Dictionary<string, object> metadata = null;
            
            try
            {
                metadata = new Dictionary<string, object>
                {
                    { "playerName", playerName }
                };
                
                Debug.Log($"[Main] Created metadata dictionary with {metadata.Count} entries");
                Debug.Log($"[Main] Metadata playerName value: '{metadata["playerName"]}'");
                Debug.Log($"[Main] Metadata playerName type: {metadata["playerName"]?.GetType().Name ?? "null"}");
            }
            catch (System.Exception metadataException)
            {
                Debug.LogError($"[Main] Failed to create metadata dictionary: {metadataException.Message}");
                // Fallback to simple metadata creation
                metadata = new Dictionary<string, object> { { "playerName", "Anonymous" } };
            }
            
            // Submit score with metadata containing player name
            Debug.Log($"[Main] Submitting to leaderboard with metadata: playerName='{metadata["playerName"]}'");
            
            var scoreResponse = await LeaderboardsService.Instance.AddPlayerScoreAsync(
                LeaderboardId, 
                score, 
                new AddPlayerScoreOptions 
                { 
                    Metadata = metadata 
                }
            );
            
            Debug.Log("[Main] Score submission with metadata successful!");
            Debug.Log($"[Main] Response PlayerId: {scoreResponse?.PlayerId ?? "null"}");
            Debug.Log($"[Main] Response Score: {scoreResponse?.Score ?? 0}");
            Debug.Log($"[Main] Response Rank: {scoreResponse?.Rank ?? -1}");
            
            // Log metadata from response - fixed to handle string metadata
            if (!string.IsNullOrEmpty(scoreResponse?.Metadata))
            {
                Debug.Log($"[Main] Response metadata: {scoreResponse.Metadata}");
            }
            else
            {
                Debug.LogWarning("[Main] Response metadata is null or empty");
            }
            
            // Attempt to log full response with better error handling
            try
            {
                string jsonResponse = JsonUtility.ToJson(scoreResponse);
                Debug.Log($"[Main] Full response JSON: {jsonResponse}");
            }
            catch (System.ArgumentException jsonException)
            {
                Debug.LogWarning($"[Main] Failed to serialize response to JSON: {jsonException.Message}");
                Debug.Log($"[Main] Score response (ToString): {scoreResponse?.ToString() ?? "null"}");
            }
            catch (System.Exception jsonException)
            {
                Debug.LogWarning($"[Main] Unexpected error serializing response: {jsonException.Message}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Main] Failed to add score to leaderboard: {e.Message}");
            Debug.LogError($"[Main] Leaderboard error stack trace: {e.StackTrace}");
            
            // Log additional context for debugging
            Debug.LogError($"[Main] Score being submitted: {score}");
            Debug.LogError($"[Main] Player ID: {AuthenticationService.Instance?.PlayerId ?? "null"}");
            Debug.LogError($"[Main] Player Display Name: {GetCurrentPlayerDisplayName()}");
            
            throw; // Re-throw to allow caller to handle
        }
    }

    /// <summary>
    /// Add current game score to leaderboard
    /// </summary>
    public async Task AddCurrentScoreToLeaderboard()
    {
        int currentScore = GetScore();
        Debug.Log($"[Main] AddCurrentScoreToLeaderboard - submitting current score: {currentScore}");
        await AddScoreToLeaderboard(currentScore);
    }

    /// <summary>
    /// Add high score (max population) to leaderboard
    /// </summary>
    public async Task AddHighScoreToLeaderboard()
    {
        int highScore = GetMaxPopulation();
        Debug.Log($"[Main] AddHighScoreToLeaderboard - submitting high score: {highScore}");
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
        Debug.Log($"[Main] GetScore: {score}");
        return score;
    }

    /// <summary>
    /// Gets the maximum population reached from GameState
    /// </summary>
    public int GetMaxPopulation()
    {
        int maxPop = GameState.Instance?.GetMaxPopulation() ?? 0;
        Debug.Log($"[Main] GetMaxPopulation: {maxPop}");
        return maxPop;
    }

    #endregion

    #region Cross-Scene Utilities

    /// <summary>
    /// Called when a new scene loads
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[Main] Scene loaded: {scene.name}, LoadSceneMode: {mode}");

        // Initialize game state if we're in the game scene
        if (scene.name == "Main" && GameState.Instance != null)
        {
            Debug.Log($"[Main] In Main scene, loadGame flag: {loadGame}");
            
            if (loadGame)
            {
                Debug.Log("[Main] Loading saved game data...");
                SaveState data = SaveManager.Load();
                if (data == null)
                {
                    Debug.LogWarning("[Main] No save data found");
                } 
                else if (GameState.Instance != null)
                {
                    Debug.Log($"[Main] Save data found - Identity: {data.playerIdentity ?? "null"}, Name: {data.playerName ?? "null"}");
                    
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
                    Debug.Log($"[Main] Loaded data: {JsonUtility.ToJson(data, true)}");
                    GameState.Instance.ApplyLoadedData(data);
                    Debug.Log("[Main] Save data applied to GameState");
                }
                
                loadGame = false; // Reset flag
                Debug.Log("[Main] LoadGame flag reset");
            }

            GameState.Instance.PAUSED = false;
            Debug.Log("[Main] GameState.PAUSED set to false");
        }
    }

    void OnDestroy()
    {
        Debug.Log("[Main] OnDestroy called");
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Debug.Log("[Main] Unsubscribed from SceneManager.sceneLoaded");
            SaveGame();  
        }
    }


    public void onExitGame()
    {
        Debug.Log("[Main] onExitGame called - saving and exiting...");

        // save game before exiting
        SaveGame();

        // then quit application
        Application.Quit();

        // if the application is running in the editor, stop play mode instead of quitting
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        Debug.Log("[Main] Editor play mode stopped");
        #endif
    }

    // TODO a proper fix for the loadGame below:

    void OnApplicationPause(bool pauseStatus)
    {
        Debug.Log($"[Main] OnApplicationPause: {pauseStatus}, loadGame: {loadGame}");

        if (!loadGame) return;

        if (pauseStatus) 
        {
            Debug.Log("[Main] App paused, saving game");
            SaveGame(); // Save when app loses focus
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        Debug.Log($"[Main] OnApplicationFocus: {hasFocus}, loadGame: {loadGame}");
        
        if (!loadGame) return;

        if (!hasFocus) 
        {
            Debug.Log("[Main] App lost focus, saving game");
            SaveGame(); // Save when app loses focus
        }
    }

    #endregion

}
