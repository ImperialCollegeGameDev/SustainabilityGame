using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using UnityEngine;
using TMPro;

/// <summary>
/// Standalone leaderboard component - now mostly handled by Main class
/// This can be used for UI-specific leaderboard operations
/// </summary>
public class Leaderboard : MonoBehaviour
{
    // Create a leaderboard with this ID in the Unity Dashboard
    const string LeaderboardId = "SusGameMainLeaderboard";

    string VersionId { get; set; }
    int Offset { get; set; }
    int Limit { get; set; }
    int RangeLimit { get; set; }
    List<string> FriendIds { get; set; }

    public GameObject leaderboard_entryPrefab; // Assign in inspector
    public GameObject spawnParent; // Parent object for leaderboard entries

    // Track created entries for proper cleanup
    private List<GameObject> activeLeaderboardEntries = new List<GameObject>();

    void Awake()
    {
        Debugger.Log("[Leaderboard] Awake called");
        
        // Validate required references early
        if (leaderboard_entryPrefab == null)
        {
            Debugger.LogError("[Leaderboard] Leaderboard entry prefab is not assigned! Please assign it in the inspector.");
        }
        
        if (spawnParent == null)
        {
            Debugger.LogWarning("[Leaderboard] Spawn parent is not assigned! Will use this transform as parent.");
            spawnParent = gameObject;
        }
    }

    async void Start()
    {
        Debugger.Log("[Leaderboard] Start called - beginning leaderboard initialization");
        
        // Ensure LeanTween is initialized
        try
        {
            LeanTween.init();
            Debugger.Log("[Leaderboard] LeanTween initialized successfully");
        }
        catch (System.Exception e)
        {
            Debugger.LogWarning($"[Leaderboard] LeanTween initialization failed: {e.Message}");
        }
        
        // Wait for authentication to be ready and then load leaderboard data
        await LoadLeaderboardData();
    }

    /// <summary>
    /// Load leaderboard data for the content view
    /// </summary>
    private async Task LoadLeaderboardData()
    {
        try
        {
            Debugger.Log("[Leaderboard] Starting to load leaderboard data...");
            Debugger.Log($"[Leaderboard] Main.IsAuthenticationReady: {Main.IsAuthenticationReady}");
            Debugger.Log($"[Leaderboard] Main.Instance exists: {Main.Instance != null}");

            // Wait for authentication to be ready with timeout
            float timeout = 10f; // 10 second timeout
            float elapsed = 0f;
            
            Debugger.Log($"[Leaderboard] Waiting for authentication (timeout: {timeout}s)...");
            while (!Main.IsAuthenticationReady && elapsed < timeout)
            {
                await Task.Delay(100); // Wait 100ms before checking again
                elapsed += 0.1f;
                
                if (elapsed % 1.0f < 0.1f) // Log every second
                {
                    Debugger.Log($"[Leaderboard] Still waiting for auth... ({elapsed:F1}s elapsed)");
                }
            }

            if (!Main.IsAuthenticationReady)
            {
                Debugger.LogError($"[Leaderboard] Authentication not ready after {timeout}s timeout. Cannot load leaderboard data.");
                Debugger.LogError($"[Leaderboard] Final auth status - Main.IsAuthenticationReady: {Main.IsAuthenticationReady}");
                Debugger.LogError($"[Leaderboard] AuthenticationService.Instance.IsSignedIn: {AuthenticationService.Instance?.IsSignedIn ?? false}");
                ShowErrorMessage("Authentication failed. Please try again later.");
                return;
            }

            Debugger.Log($"[Leaderboard] Authentication ready after {elapsed:F1}s");
            Debugger.Log($"[Leaderboard] Player ID: {AuthenticationService.Instance.PlayerId}");
            
            // Check leaderboard connection status
            bool leaderboardConnected = await CheckLeaderboardConnection();
            Debugger.Log($"[Leaderboard] Leaderboard connection status: {leaderboardConnected}");
            
            if (!leaderboardConnected)
            {
                ShowErrorMessage("Could not connect to leaderboard service. Please check your internet connection.");
                return;
            }

            // Fetch leaderboard scores and generate UI - use await to ensure proper sequencing
            Debugger.Log("[Leaderboard] Fetching leaderboard scores and player score...");
            await GetScoresAsync();
            await GetPlayerScoreAsync();
            
            Debugger.Log("[Leaderboard] Leaderboard data fetch completed successfully");
        }
        catch (System.Exception e)
        {
            Debugger.LogError($"[Leaderboard] Failed to load leaderboard data: {e.Message}");
            Debugger.LogError($"[Leaderboard] LoadLeaderboardData stack trace: {e.StackTrace}");
            ShowErrorMessage("Failed to load leaderboard data. Please try refreshing.");
        }
    }

    /// <summary>
    /// Show error message to user (placeholder for actual UI implementation)
    /// </summary>
    private void ShowErrorMessage(string message)
    {
        Debugger.LogWarning($"[Leaderboard] Error message for user: {message}");
        // TODO: Implement actual UI error message display
    }

    /// <summary>
    /// Check leaderboard connection
    /// </summary>
    private async Task<bool> CheckLeaderboardConnection()
    {
        try
        {
            Debugger.Log("[Leaderboard] Testing leaderboard connection...");
            var testResponse = await LeaderboardsService.Instance.GetScoresAsync(LeaderboardId, new GetScoresOptions { Limit = 1 });
            Debugger.Log($"[Leaderboard] Connection test successful. Results count: {testResponse?.Results?.Count ?? 0}");
            return true;
        }
        catch (System.Exception e)
        {
            Debugger.LogError($"[Leaderboard] Connection test failed: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Check if authentication is ready before performing leaderboard operations
    /// </summary>
    private bool IsAuthenticationReady()
    {
        bool mainReady = Main.IsAuthenticationReady;
        bool serviceSignedIn = AuthenticationService.Instance?.IsSignedIn ?? false;
        
        Debugger.Log($"[Leaderboard] Authentication check - Main.IsAuthenticationReady: {mainReady}, AuthService.IsSignedIn: {serviceSignedIn}");
        
        if (!mainReady || !serviceSignedIn)
        {
            Debugger.LogWarning("[Leaderboard] Authentication not ready. Make sure Main class has initialized Unity Services.");
            Debugger.LogWarning($"[Leaderboard] Details - Main ready: {mainReady}, Service signed in: {serviceSignedIn}");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Public method to refresh leaderboard data (can be called by refresh button)
    /// </summary>
    public async void RefreshLeaderboard()
    {
        Debugger.Log("[Leaderboard] RefreshLeaderboard called - reloading data...");
        await LoadLeaderboardData();
    }

    /// <summary>
    /// Get scores asynchronously with proper error handling - fixed for string metadata
    /// </summary>
    private async Task GetScoresAsync()
    {
        Debugger.Log("[Leaderboard] GetScoresAsync called");
        
        if (!IsAuthenticationReady()) 
        {
            Debugger.LogWarning("[Leaderboard] GetScoresAsync aborted - authentication not ready");
            return;
        }

        try
        {
            Debugger.Log($"[Leaderboard] Requesting scores from leaderboard ID: {LeaderboardId}");
            var scoresResponse = await LeaderboardsService.Instance.GetScoresAsync(LeaderboardId, new GetScoresOptions { IncludeMetadata = true });
            
            Debugger.Log($"[Leaderboard] Scores response received");
            Debugger.Log($"[Leaderboard] Results count: {scoresResponse?.Results?.Count ?? 0}");
            Debugger.Log($"[Leaderboard] Total entries: {scoresResponse?.Total ?? 0}");
            Debugger.Log($"[Leaderboard] Offset: {scoresResponse?.Offset ?? 0}");
            Debugger.Log($"[Leaderboard] Limit: {scoresResponse?.Limit ?? 0}");

            if (scoresResponse?.Results != null)
            {
                for (int i = 0; i < scoresResponse.Results.Count; i++)
                {
                    var entry = scoresResponse.Results[i];
                    Debugger.Log($"[Leaderboard] Entry {i}: Rank={entry.Rank}, Score={entry.Score}, PlayerID={entry.PlayerId}");
                    
                    // Log metadata - fixed to handle string metadata
                    if (!string.IsNullOrEmpty(entry.Metadata))
                    {
                        Debugger.Log($"[Leaderboard] Entry {i} metadata: {entry.Metadata}");
                    }
                    else
                    {
                        Debugger.Log($"[Leaderboard] Entry {i} has no metadata");
                    }
                }
            }

            generateLeaderboard(scoresResponse);
        }
        catch (System.Exception e)
        {
            Debugger.LogError($"[Leaderboard] Failed to get scores: {e.Message}");
            Debugger.LogError($"[Leaderboard] GetScoresAsync stack trace: {e.StackTrace}");
            ShowErrorMessage("Failed to load leaderboard scores.");
        }
    }

    // Keep the old public method for backward compatibility
    public async void GetScores()
    {
        await GetScoresAsync();
    }

    public async void GetPaginatedScores()
    {
        Debugger.Log($"[Leaderboard] GetPaginatedScores called with Offset={Offset}, Limit={Limit}");
        
        if (!IsAuthenticationReady()) 
        {
            Debugger.LogWarning("[Leaderboard] GetPaginatedScores aborted - authentication not ready");
            return;
        }

        try
        {
            Offset = 10;
            Limit = 10;
            Debugger.Log($"[Leaderboard] Requesting paginated scores - Offset: {Offset}, Limit: {Limit}");
            
            var scoresResponse = await LeaderboardsService.Instance.GetScoresAsync(LeaderboardId, new GetScoresOptions { Offset = Offset, Limit = Limit, IncludeMetadata = true });
            
            Debugger.Log($"[Leaderboard] Paginated scores response received - Results count: {scoresResponse?.Results?.Count ?? 0}");

            generateLeaderboard(scoresResponse);
        }
        catch (System.Exception e)
        {
            Debugger.LogError($"[Leaderboard] Failed to get paginated scores: {e.Message}");
            Debugger.LogError($"[Leaderboard] GetPaginatedScores stack trace: {e.StackTrace}");
        }
    }

    public void generateLeaderboard(LeaderboardScoresPage scoresPage)
    {
        Debugger.Log("[Leaderboard] generateLeaderboard called");
        
        if (scoresPage == null)
        {
            Debugger.LogWarning("[Leaderboard] Null scores page passed to generateLeaderboard");
            return;
        }

        if (leaderboard_entryPrefab == null)
        {
            Debugger.LogError("[Leaderboard] Leaderboard entry prefab not assigned!");
            ShowErrorMessage("Leaderboard configuration error. Please contact support.");
            return;
        }

        if (spawnParent == null)
        {
            Debugger.LogError("[Leaderboard] Spawn parent not assigned!");
            spawnParent = gameObject; // Fallback to self
        }

        Debugger.Log($"[Leaderboard] Generating leaderboard with {scoresPage.Results?.Count ?? 0} entries");

        // Clear existing entries before generating new ones
        int clearedEntries = ClearExistingEntries();
        Debugger.Log($"[Leaderboard] Cleared {clearedEntries} existing entries");
        
        if (scoresPage.Results == null || scoresPage.Results.Count == 0)
        {
            Debugger.Log("[Leaderboard] No results to display");
            ShowErrorMessage("No leaderboard entries found.");
            return;
        }
        
        int entryIndex = 0;
        foreach (var entry in scoresPage.Results)
        {
            if (entry == null)
            {
                Debugger.LogWarning($"[Leaderboard] Null entry at index {entryIndex}, skipping...");
                continue;
            }

            Debugger.Log($"[Leaderboard] Processing entry {entryIndex}: Rank={entry.Rank}, Score={entry.Score}, PlayerID={entry.PlayerId}");
            
            try
            {
                GameObject newEntry = Instantiate(leaderboard_entryPrefab, spawnParent.transform);
                if (newEntry == null)
                {
                    Debugger.LogError($"[Leaderboard] Failed to instantiate entry prefab for entry {entryIndex}");
                    continue;
                }

                // Track the created entry for cleanup
                activeLeaderboardEntries.Add(newEntry);
                
                Debugger.Log($"[Leaderboard] Instantiated entry prefab: {newEntry.name}");
                
                // Get all TextMeshPro components from the entry
                TextMeshProUGUI[] textComponents = newEntry.GetComponentsInChildren<TextMeshProUGUI>();
                Debugger.Log($"[Leaderboard] Found {textComponents.Length} TextMeshPro components in entry");
                
                if (textComponents.Length >= 3)
                {
                    // Set rank (first TextMeshPro child)
                    string rankText = $"#{entry.Rank + 1}";
                    textComponents[0].text = rankText;
                    Debugger.Log($"[Leaderboard] Set rank text: {rankText}");
                    
                    // Set score (second TextMeshPro child)
                    string scoreText = ((int)entry.Score).ToString();
                    textComponents[1].text = scoreText;
                    Debugger.Log($"[Leaderboard] Set score text: {scoreText}");
                    
                    // Set player name (third TextMeshPro child) - from metadata
                    string displayName = GetPlayerNameFromMetadata(entry);
                    textComponents[2].text = displayName;
                    Debugger.Log($"[Leaderboard] Set player name: {displayName}");
                }
                else
                {
                    Debugger.LogWarning($"[Leaderboard] Entry prefab doesn't have enough TextMeshPro children. Expected 3, found {textComponents.Length}");
                    Debugger.LogWarning($"[Leaderboard] Entry prefab structure:");
                    for (int i = 0; i < textComponents.Length; i++)
                    {
                        Debugger.LogWarning($"[Leaderboard]   TextMeshPro {i}: {textComponents[i].name} - {textComponents[i].text}");
                    }
                }
                
                // Add LeanTween animation with error handling
                Debugger.Log($"[Leaderboard] Adding animation to entry {entryIndex}");
                AnimateEntryIn(newEntry, entryIndex);
                entryIndex++;
            }
            catch (System.Exception e)
            {
                Debugger.LogError($"[Leaderboard] Failed to create entry {entryIndex}: {e.Message}");
                Debugger.LogError($"[Leaderboard] Entry creation stack trace: {e.StackTrace}");
            }
        }
        
        Debugger.Log($"[Leaderboard] Successfully processed {entryIndex} leaderboard entries");
    }

    /// <summary>
    /// Extract player name from leaderboard entry metadata - fixed for JSON string metadata
    /// </summary>
    private string GetPlayerNameFromMetadata(Unity.Services.Leaderboards.Models.LeaderboardEntry entry)
    {
        if (entry == null)
        {
            Debugger.LogWarning("[Leaderboard] GetPlayerNameFromMetadata: entry is null");
            return "Anonymous";
        }

        Debugger.Log($"[Leaderboard] GetPlayerNameFromMetadata called for player {entry.PlayerId}");
        
        // Check if entry has metadata
        if (string.IsNullOrEmpty(entry.Metadata))
        {
            Debugger.Log($"[Leaderboard] No metadata found for player {entry.PlayerId}, using fallback");
            return "Anonymous";
        }
        
        // Parse JSON metadata to extract player name
        try
        {
            Debugger.Log($"[Leaderboard] Raw metadata JSON: {entry.Metadata}");
            
            // Try to parse as a simple JSON object
            var metadataObj = JsonUtility.FromJson<MetadataWrapper>(entry.Metadata);
            
            if (metadataObj != null && !string.IsNullOrEmpty(metadataObj.playerName))
            {
                Debugger.Log($"[Leaderboard] Successfully parsed playerName: '{metadataObj.playerName}'");
                return metadataObj.playerName;
            }
            else
            {
                Debugger.LogWarning($"[Leaderboard] Parsed metadata but playerName is null/empty for player {entry.PlayerId}");
            }
        }
        catch (System.Exception e)
        {
            Debugger.LogError($"[Leaderboard] Failed to parse JSON metadata: {e.Message}");
            Debugger.LogError($"[Leaderboard] Raw metadata that failed to parse: {entry.Metadata}");
            
            // Try regex parsing as fallback
            try
            {
                if (entry.Metadata.Contains("playerName"))
                {
                    // Look for "playerName":"value" pattern
                    var match = System.Text.RegularExpressions.Regex.Match(
                        entry.Metadata, 
                        "\"playerName\"\\s*:\\s*\"([^\"]+)\""
                    );
                    
                    if (match.Success && match.Groups.Count > 1)
                    {
                        string playerName = match.Groups[1].Value;
                        Debugger.Log($"[Leaderboard] Regex extraction found playerName: '{playerName}'");
                        if (!string.IsNullOrEmpty(playerName) && playerName != "null")
                        {
                            return playerName;
                        }
                    }
                }
            }
            catch (System.Exception fallbackException)
            {
                Debugger.LogError($"[Leaderboard] Fallback metadata parsing also failed: {fallbackException.Message}");
            }
        }
        
        // Fallback to Anonymous if no valid player name found
        Debugger.Log($"[Leaderboard] Using fallback name for player {entry.PlayerId}");
        return "Anonymous";
    }

    /// <summary>
    /// Animate leaderboard entry with simple scale boing effect
    /// </summary>
    private void AnimateEntryIn(GameObject entry, int index)
    {
        if (entry == null) 
        {
            Debugger.LogWarning("[Leaderboard] AnimateEntryIn: entry is null");
            return;
        }

        Debugger.Log($"[Leaderboard] Animating entry {index}: {entry.name}");

        try
        {
            // Stagger the animation based on index
            float delay = index * 0.1f;
            Debugger.Log($"[Leaderboard] Animation delay for entry {index}: {delay}s");
            
            // Start with scale at zero for pop-in effect
            entry.transform.localScale = Vector3.zero;
            
            // Animate scale with boing effect (easeOutBack creates the "boing" effect)
            LeanTween.scale(entry, Vector3.one, 0.5f)
                .setDelay(delay)
                .setEase(LeanTweenType.easeOutBack)
                .setOnComplete(() => {
                    Debugger.Log($"[Leaderboard] Scale animation completed for entry {index}");
                });
                
            Debugger.Log($"[Leaderboard] Simple scale animation started for entry {index}");
        }
        catch (System.Exception e)
        {
            Debugger.LogError($"[Leaderboard] Failed to animate entry {index}: {e.Message}");
            // Reset to visible state if animation fails
            entry.transform.localScale = Vector3.one;
        }
    }

    /// <summary>
    /// Clear existing leaderboard entries before generating new ones
    /// </summary>
    private int ClearExistingEntries()
    {
        Debugger.Log("[Leaderboard] ClearExistingEntries called");
        int clearedCount = 0;
        
        // First, clear tracked entries
        foreach (var entry in activeLeaderboardEntries)
        {
            if (entry != null)
            {
                try
                {
                    // Cancel any active tweens on this object before destroying
                    LeanTween.cancel(entry);
                    Destroy(entry); // Use Destroy instead of DestroyImmediate for better performance
                    clearedCount++;
                }
                catch (System.Exception e)
                {
                    Debugger.LogWarning($"[Leaderboard] Failed to destroy tracked entry: {e.Message}");
                }
            }
        }
        activeLeaderboardEntries.Clear();
        
        // Fallback: clear any remaining children with TextMeshPro components
        Transform parentToCheck = spawnParent != null ? spawnParent.transform : transform;
        Debugger.Log($"[Leaderboard] Clearing entries from parent: {parentToCheck.name}");
        
        // Create a list to avoid modifying collection during iteration
        List<Transform> childrenToDestroy = new List<Transform>();
        
        for (int i = 0; i < parentToCheck.childCount; i++)
        {
            Transform child = parentToCheck.GetChild(i);
            TextMeshProUGUI[] childTextComponents = child.GetComponentsInChildren<TextMeshProUGUI>();
            
            if (childTextComponents.Length > 0)
            {
                childrenToDestroy.Add(child);
            }
        }
        
        foreach (var child in childrenToDestroy)
        {
            if (child != null)
            {
                try
                {
                    Debugger.Log($"[Leaderboard] Clearing fallback child: {child.name} (has TextMeshPro components)");
                    // Cancel any active tweens on this object before destroying
                    LeanTween.cancel(child.gameObject);
                    Destroy(child.gameObject);
                    clearedCount++;
                }
                catch (System.Exception e)
                {
                    Debugger.LogWarning($"[Leaderboard] Failed to destroy fallback entry: {e.Message}");
                }
            }
        }
        
        Debugger.Log($"[Leaderboard] Cleared {clearedCount} existing entries");
        return clearedCount;
    }

    /// <summary>
    /// Get player score asynchronously with proper error handling - fixed for string metadata
    /// </summary>
    private async Task GetPlayerScoreAsync()
    {
        Debugger.Log("[Leaderboard] GetPlayerScoreAsync called");
        
        if (!IsAuthenticationReady()) 
        {
            Debugger.LogWarning("[Leaderboard] GetPlayerScoreAsync aborted - authentication not ready");
            return;
        }

        try
        {
            Debugger.Log($"[Leaderboard] Requesting player score for ID: {AuthenticationService.Instance.PlayerId}");
            var scoreResponse = await LeaderboardsService.Instance.GetPlayerScoreAsync(LeaderboardId, new GetPlayerScoreOptions { IncludeMetadata = true });
            
            Debugger.Log($"[Leaderboard] Player score response received");
            Debugger.Log($"[Leaderboard] Player ID: {scoreResponse?.PlayerId ?? "null"}");
            Debugger.Log($"[Leaderboard] Player Score: {scoreResponse?.Score ?? 0}");
            Debugger.Log($"[Leaderboard] Player Rank: {scoreResponse?.Rank ?? -1}");
            
            // Log player metadata - fixed to handle string metadata
            if (!string.IsNullOrEmpty(scoreResponse?.Metadata))
            {
                Debugger.Log($"[Leaderboard] Player score metadata: {scoreResponse.Metadata}");
            }
            else
            {
                Debugger.Log("[Leaderboard] Player score has no metadata");
            }
        }
        catch (System.Exception e)
        {
            Debugger.LogError($"[Leaderboard] Failed to get player score: {e.Message}");
            Debugger.LogError($"[Leaderboard] GetPlayerScoreAsync stack trace: {e.StackTrace}");
        }
    }

    // Keep the old public method for backward compatibility
    public async void GetPlayerScore()
    {
        await GetPlayerScoreAsync();
    }

    public async void GetVersionScores()
    {
        Debugger.Log($"[Leaderboard] GetVersionScores called with VersionId: {VersionId ?? "null"}");
        
        if (!IsAuthenticationReady()) 
        {
            Debugger.LogWarning("[Leaderboard] GetVersionScores aborted - authentication not ready");
            return;
        }

        try
        {
            var versionScoresResponse = await LeaderboardsService.Instance.GetVersionScoresAsync(LeaderboardId, VersionId, new GetVersionScoresOptions { IncludeMetadata = true });
            
            Debugger.Log($"[Leaderboard] Version scores response received - Results count: {versionScoresResponse?.Results?.Count ?? 0}");
        }
        catch (System.Exception e)
        {
            Debugger.LogError($"[Leaderboard] Failed to get version scores: {e.Message}");
            Debugger.LogError($"[Leaderboard] GetVersionScores stack trace: {e.StackTrace}");
        }
    }

    void OnDestroy()
    {
        Debugger.Log("[Leaderboard] OnDestroy called - cancelling any active tweens");
        
        try
        {
            activeLeaderboardEntries.Clear();
        }
        catch (System.Exception e)
        {
            Debugger.LogWarning($"[Leaderboard] Error during cleanup: {e.Message}");
        }
    }

    public void ReturnHome()
    {
        Main.Instance.ReturnHome();
    }
}

// Create a serializable class for JSON parsing
[System.Serializable]
public class MetadataWrapper
{
    public string playerName;
}