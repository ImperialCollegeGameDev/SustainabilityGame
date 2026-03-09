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
        if (leaderboard_entryPrefab == null)
            Debugger.LogError("[Leaderboard] Entry prefab not assigned!");

        if (spawnParent == null)
            spawnParent = gameObject;
    }

    async void Start()
    {
        try { LeanTween.init(); } catch { }
        await LoadLeaderboardData();
    }

    /// <summary>
    /// Load leaderboard data for the content view
    /// </summary>
    private async Task LoadLeaderboardData()
    {
        try
        {
            float timeout = 10f;
            float elapsed = 0f;
            while (!Main.IsAuthenticationReady && elapsed < timeout)
            {
                await Task.Delay(250);
                elapsed += 0.25f;
            }

            if (!Main.IsAuthenticationReady)
            {
                Debugger.LogError("[Leaderboard] Authentication not ready (timeout)");
                ShowErrorMessage("Authentication failed. Please try again later.");
                return;
            }

            if (!await CheckLeaderboardConnection())
            {
                ShowErrorMessage("Could not connect to leaderboard service.");
                return;
            }

            await GetScoresAsync();
            await GetPlayerScoreAsync();
        }
        catch (System.Exception e)
        {
            Debugger.LogError($"[Leaderboard] Failed to load leaderboard data: {e.Message}");
            ShowErrorMessage("Failed to load leaderboard data. Please try refreshing.");
        }
    }

    /// <summary>
    /// Show error message to user (placeholder for actual UI implementation)
    /// </summary>
    private void ShowErrorMessage(string message)
    {
        Debugger.LogWarning($"[Leaderboard] Error: {message}");
        // TODO: Implement actual UI error message display
    }

    /// <summary>
    /// Check leaderboard connection
    /// </summary>
    private async Task<bool> CheckLeaderboardConnection()
    {
        try
        {
            var checkTask = LeaderboardsService.Instance.GetScoresAsync(LeaderboardId, new GetScoresOptions { Limit = 1 });
            if (await Task.WhenAny(checkTask, Task.Delay(8000)) != checkTask)
            {
                Debugger.LogError("[Leaderboard] Connection check timed out");
                return false;
            }
            await checkTask; // propagate any exception
            return true;
        }
        catch
        {
            Debugger.LogError("[Leaderboard] Leaderboard connection test failed");
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
        if (!mainReady || !serviceSignedIn) { Debugger.LogWarning("[Leaderboard] Authentication not ready"); return false; }
        return true;
    }

    /// <summary>
    /// Public method to refresh leaderboard data (can be called by refresh button)
    /// </summary>
    public async void RefreshLeaderboard()
    {
        await LoadLeaderboardData();
    }

    /// <summary>
    /// Get scores asynchronously with proper error handling
    /// </summary>
    private async Task GetScoresAsync()
    {
        if (!IsAuthenticationReady()) return;

        try
        {
            var scoresTask = LeaderboardsService.Instance.GetScoresAsync(LeaderboardId, new GetScoresOptions { IncludeMetadata = true });
            if (await Task.WhenAny(scoresTask, Task.Delay(8000)) != scoresTask)
            {
                Debugger.LogError("[Leaderboard] GetScores timed out");
                ShowErrorMessage("Failed to load leaderboard scores.");
                return;
            }
            generateLeaderboard(await scoresTask);
        }
        catch (System.Exception e)
        {
            Debugger.LogError($"[Leaderboard] Failed to get scores: {e.Message}");
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
        if (!IsAuthenticationReady()) return;

        try
        {
            Offset = 10;
            Limit = 10;
            var scoresTask = LeaderboardsService.Instance.GetScoresAsync(LeaderboardId, new GetScoresOptions { Offset = Offset, Limit = Limit, IncludeMetadata = true });
            if (await Task.WhenAny(scoresTask, Task.Delay(8000)) != scoresTask)
            {
                Debugger.LogError("[Leaderboard] GetPaginatedScores timed out");
                return;
            }
            generateLeaderboard(await scoresTask);
        }
        catch (System.Exception e)
        {
            Debugger.LogError($"[Leaderboard] Failed to get paginated scores: {e.Message}");
        }
    }

    public void generateLeaderboard(LeaderboardScoresPage scoresPage)
    {
        if (scoresPage == null)
        {
            Debugger.LogWarning("[Leaderboard] Null scores page");
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

        ClearExistingEntries();

        if (scoresPage.Results == null || scoresPage.Results.Count == 0)
        {
            ShowErrorMessage("No leaderboard entries found.");
            return;
        }

        int entryIndex = 0;
        foreach (var entry in scoresPage.Results)
        {
            if (entry == null) { continue; }

            try
            {
                GameObject newEntry = Instantiate(leaderboard_entryPrefab, spawnParent.transform);
                if (newEntry == null) { Debugger.LogError("[Leaderboard] Failed to instantiate entry prefab"); continue; }
                activeLeaderboardEntries.Add(newEntry);

                TextMeshProUGUI[] textComponents = newEntry.GetComponentsInChildren<TextMeshProUGUI>();
                if (textComponents.Length >= 3)
                {
                    textComponents[0].text = $"#{entry.Rank + 1}";
                    textComponents[1].text = ((int)entry.Score).ToString();
                    textComponents[2].text = GetPlayerNameFromMetadata(entry);

                    bool isCurrentPlayer = AuthenticationService.Instance.IsSignedIn
                        && entry.PlayerId == AuthenticationService.Instance.PlayerId;
                    if (isCurrentPlayer)
                    {
                        Color gold = new Color(1f, 0.82f, 0.16f);
                        textComponents[2].color = gold;
                        textComponents[2].fontStyle = TMPro.FontStyles.Underline;
                    }
                }
                else
                {
                    Debugger.LogWarning("[Leaderboard] Entry prefab missing Text fields");
                }

                AnimateEntryIn(newEntry, entryIndex);
                entryIndex++;
            }
            catch (System.Exception e)
            {
                Debugger.LogError($"[Leaderboard] Failed to create entry: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Extract player name from leaderboard entry metadata
    /// </summary>
    private string GetPlayerNameFromMetadata(Unity.Services.Leaderboards.Models.LeaderboardEntry entry)
    {
        if (entry == null || string.IsNullOrEmpty(entry.Metadata)) return "Anonymous";

        try
        {
            var metadataObj = JsonUtility.FromJson<MetadataWrapper>(entry.Metadata);
            if (metadataObj != null && !string.IsNullOrEmpty(metadataObj.playerName)) return metadataObj.playerName;
        }
        catch
        {
            try
            {
                var match = System.Text.RegularExpressions.Regex.Match(entry.Metadata, "\"playerName\"\\s*:\\s*\"([^\"]+)\"");
                if (match.Success && match.Groups.Count > 1) return match.Groups[1].Value;
            }
            catch { }
        }

        return "Anonymous";
    }

    /// <summary>
    /// Animate leaderboard entry with simple scale boing effect
    /// </summary>
    private void AnimateEntryIn(GameObject entry, int index)
    {
        if (entry == null) return;

        try
        {
            float delay = index * 0.1f;
            entry.transform.localScale = Vector3.zero;
            LeanTween.scale(entry, Vector3.one, 0.5f)
                .setDelay(delay)
                .setEase(LeanTweenType.easeOutBack)
                .setOnComplete(() => { });
        }
        catch
        {
            Debugger.LogError("[Leaderboard] Failed to animate entry");
            entry.transform.localScale = Vector3.one;
        }
    }

    /// <summary>
    /// Clear existing leaderboard entries before generating new ones
    /// </summary>
    private int ClearExistingEntries()
    {
        int clearedCount = 0;

        // First, clear tracked entries
        foreach (var entry in activeLeaderboardEntries)
        {
            if (entry == null) continue;
            try { LeanTween.cancel(entry); Destroy(entry); clearedCount++; } catch { }
        }
        activeLeaderboardEntries.Clear();

        // Fallback: clear any remaining children with TextMeshPro components
        Transform parentToCheck = spawnParent != null ? spawnParent.transform : transform;

        List<Transform> childrenToDestroy = new List<Transform>();
        for (int i = 0; i < parentToCheck.childCount; i++)
        {
            Transform child = parentToCheck.GetChild(i);
            TextMeshProUGUI[] childTextComponents = child.GetComponentsInChildren<TextMeshProUGUI>();
            if (childTextComponents.Length > 0) childrenToDestroy.Add(child);
        }

        foreach (var child in childrenToDestroy)
        {
            if (child == null) continue;
            try { LeanTween.cancel(child.gameObject); Destroy(child.gameObject); clearedCount++; } catch { }
        }

        return clearedCount;
    }

    /// <summary>
    /// Get player score asynchronously with proper error handling
    /// </summary>
    private async Task GetPlayerScoreAsync()
    {
        if (!IsAuthenticationReady()) return;

        try
        {
            var scoreTask = LeaderboardsService.Instance.GetPlayerScoreAsync(LeaderboardId, new GetPlayerScoreOptions { IncludeMetadata = true });
            var timeoutTask = Task.Delay(8000);

            if (await Task.WhenAny(scoreTask, timeoutTask) != scoreTask)
            {
                Debugger.Log("[Leaderboard] GetPlayerScore timed out - player likely has no score yet");
                return;
            }

            // Observe the task result without letting any exception propagate further
            if (scoreTask.IsFaulted)
            {
                HandlePlayerScoreException(scoreTask.Exception?.GetBaseException());
                return;
            }

            var scoreResponse = scoreTask.Result;
            Debugger.Log($"[Leaderboard] Player rank: #{(scoreResponse?.Rank ?? -1) + 1}, score: {scoreResponse?.Score ?? 0}");
        }
        catch (System.Exception e)
        {
            HandlePlayerScoreException(e);
        }
    }

    private void HandlePlayerScoreException(System.Exception e)
    {
        if (e == null) return;
        Debugger.LogError($"[Leaderboard] Failed to get player score: {e.Message}");
        Notifications.Instance.PostNotification("Failed to load your leaderboard score. If you've just achieved a new high score, it may not be recorded yet. Please try again later.");
    }

    // Keep the old public method for backward compatibility
    public async void GetPlayerScore()
    {
        await GetPlayerScoreAsync();
    }

    public async void GetVersionScores()
    {
        if (!IsAuthenticationReady()) return;

        try
        {
            var scoresTask = LeaderboardsService.Instance.GetVersionScoresAsync(LeaderboardId, VersionId, new GetVersionScoresOptions { IncludeMetadata = true });
            if (await Task.WhenAny(scoresTask, Task.Delay(8000)) != scoresTask)
            {
                Debugger.LogError("[Leaderboard] GetVersionScores timed out");
                return;
            }
            var versionScoresResponse = await scoresTask;
            Debugger.Log($"[Leaderboard] Version scores: {versionScoresResponse?.Results?.Count ?? 0} entries");
        }
        catch (System.Exception e)
        {
            Debugger.LogError($"[Leaderboard] Failed to get version scores: {e.Message}");
        }
    }

    void OnDestroy()
    {
        activeLeaderboardEntries.Clear();
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
