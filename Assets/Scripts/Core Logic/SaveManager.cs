using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveManager
{
    private static string path => Application.persistentDataPath + "/save.json";
    private const string SAVE_KEY = "GameSaveData";
    private const string SAVE_CHUNK_PREFIX = "GameSaveChunk_";
    private const int MAX_PLAYERPREFS_SIZE = 1024 * 8; // 8KB chunks to be safe

    /// <summary>
    /// Check if we're running on WebGL platform
    /// </summary>
    private static bool IsWebGL => Application.platform == RuntimePlatform.WebGLPlayer;

    public static void Save()
    {
        try
        {
            Debugger.Log($"[SaveManager] ========== SAVE START ==========");
            Debugger.Log($"[SaveManager] Platform: {Application.platform}");
            Debugger.Log($"[SaveManager] IsWebGL: {IsWebGL}");
            
            SaveState data = new SaveState();

            // Get data from GameState
            if (GameState.Instance != null)
            {
                data.money = GameState.Instance.money;
                data.happiness = GameState.Instance.happiness;
                data.emissions = GameState.Instance.TotalEmissions;
                data.maxPopulation = GameState.Instance.GetMaxPopulation();
                Debugger.Log($"[SaveManager] GameState data: Money={data.money}, Happiness={data.happiness:F2}, Emissions={data.emissions:F2}, MaxPop={data.maxPopulation}");
            }
            else
            {
                Debugger.LogWarning("[SaveManager] GameState.Instance is null!");
            }

            // Get player identity and name from Main if available
            if (Main.Instance != null)
            {
                data.playerIdentity = Main.Instance.GetCurrentPlayerIdentity();
                data.playerName = Main.Instance.GetCurrentPlayerDisplayName();
                Debugger.Log($"[SaveManager] Player data: Identity={data.playerIdentity ?? "null"}, Name={data.playerName ?? "null"}");
            }
            else
            {
                Debugger.LogWarning("[SaveManager] Main.Instance is null!");
            }

            data.tiles = new List<TileSaveData>();

            // Get tile data from GameState (authoritative source)
            if (GameState.Instance != null)
            {
                foreach (var tileObj in GameState.Instance.GetBuildings())
                {
                    TileSaveData tileData = new TileSaveData();
                    tileData.gridPosition = tileObj.Origin;
                    tileData.def = tileObj.Definition;

                    if (tileObj is ResidentialTileObject res)
                    {
                        tileData.occupancy = res.occupancy;
                    }

                    data.tiles.Add(tileData);
                }
                Debugger.Log($"[SaveManager] Saved {data.tiles.Count} tiles");
            }

            string json = JsonUtility.ToJson(data, true);
            Debugger.Log($"[SaveManager] JSON size: {json.Length} characters");
            Debugger.Log($"[SaveManager] JSON preview (first 500 chars): {json.Substring(0, Mathf.Min(500, json.Length))}");

            if (IsWebGL)
            {
                Debugger.Log("[SaveManager] Using WebGL save method (PlayerPrefs)");
                SaveToPlayerPrefs(json);
                Debugger.Log($"[SaveManager] ✓ Game saved to PlayerPrefs (WebGL)");
            }
            else
            {
                Debugger.Log($"[SaveManager] Using File save method: {path}");
                File.WriteAllText(path, json);
                Debugger.Log($"[SaveManager] ✓ Game saved to {path}");
            }
            
            Debugger.Log($"[SaveManager] ========== SAVE COMPLETE ==========");
        }
        catch (System.Exception e)
        {
            Debugger.LogError($"[SaveManager] ✗ Failed to save game: {e.Message}");
            Debugger.LogError($"[SaveManager] Stack trace: {e.StackTrace}");
        }
    }

    public static SaveState Load()
    {
        try
        {
            Debugger.Log($"[SaveManager] ========== LOAD START ==========");
            Debugger.Log($"[SaveManager] Platform: {Application.platform}");
            Debugger.Log($"[SaveManager] IsWebGL: {IsWebGL}");
            
            string json;

            if (IsWebGL)
            {
                Debugger.Log("[SaveManager] Using WebGL load method (PlayerPrefs)");
                json = LoadFromPlayerPrefs();
                if (string.IsNullOrEmpty(json))
                {
                    Debugger.LogWarning("[SaveManager] ✗ No save data found in PlayerPrefs (WebGL)");
                    Debugger.Log($"[SaveManager] Checking keys - SAVE_KEY exists: {PlayerPrefs.HasKey(SAVE_KEY)}");
                    Debugger.Log($"[SaveManager] Checking keys - SAVE_CHUNK_0 exists: {PlayerPrefs.HasKey(SAVE_CHUNK_PREFIX + "0")}");
                    return null;
                }
                Debugger.Log($"[SaveManager] Loaded JSON from PlayerPrefs, size: {json.Length} characters");
            }
            else
            {
                Debugger.Log($"[SaveManager] Using File load method: {path}");
                if (!File.Exists(path))
                {
                    Debugger.LogWarning($"[SaveManager] ✗ No save file found at {path}");
                    return null;
                }

                json = File.ReadAllText(path);
                Debugger.Log($"[SaveManager] Loaded JSON from file, size: {json.Length} characters");
            }

            Debugger.Log($"[SaveManager] JSON preview (first 500 chars): {json.Substring(0, Mathf.Min(500, json.Length))}");
            
            SaveState data = JsonUtility.FromJson<SaveState>(json);
            
            if (data == null)
            {
                Debugger.LogError("[SaveManager] ✗ Failed to deserialize JSON - data is null");
                return null;
            }
            
            Debugger.Log($"[SaveManager] Deserialized data: Money={data.money}, Happiness={data.happiness:F2}, Emissions={data.emissions:F2}, MaxPop={data.maxPopulation}");
            Debugger.Log($"[SaveManager] Player data: Identity={data.playerIdentity ?? "null"}, Name={data.playerName ?? "null"}");
            Debugger.Log($"[SaveManager] Tiles count: {(data.tiles != null ? data.tiles.Count : 0)}");
            Debugger.Log($"[SaveManager] ✓ Game loaded successfully");
            Debugger.Log($"[SaveManager] ========== LOAD COMPLETE ==========");
            
            return data;
        }
        catch (System.Exception e)
        {
            Debugger.LogError($"[SaveManager] ✗ Failed to load game: {e.Message}");
            Debugger.LogError($"[SaveManager] Stack trace: {e.StackTrace}");
            return null;
        }
    }

    /// <summary>
    /// Checks if save data exists
    /// </summary>
    public static bool HasSaveData()
    {
        try
        {
            bool exists;
            if (IsWebGL)
            {
                bool hasSingleKey = PlayerPrefs.HasKey(SAVE_KEY);
                bool hasChunkedKey = PlayerPrefs.HasKey(SAVE_CHUNK_PREFIX + "0");
                exists = hasSingleKey || hasChunkedKey;
                Debugger.Log($"[SaveManager] HasSaveData check (WebGL): SingleKey={hasSingleKey}, ChunkedKey={hasChunkedKey}, Result={exists}");
            }
            else
            {
                exists = File.Exists(path);
                Debugger.Log($"[SaveManager] HasSaveData check (File): {exists} at {path}");
            }
            return exists;
        }
        catch (System.Exception e)
        {
            Debugger.LogError($"[SaveManager] Error checking for save data: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Deletes the save file
    /// </summary>
    public static void DeleteSave()
    {
        try
        {
            Debugger.Log($"[SaveManager] ========== DELETE SAVE ==========");
            
            if (IsWebGL)
            {
                int deletedKeys = 0;
                
                // Delete main key
                if (PlayerPrefs.HasKey(SAVE_KEY))
                {
                    PlayerPrefs.DeleteKey(SAVE_KEY);
                    deletedKeys++;
                    Debugger.Log("[SaveManager] Deleted main save key");
                }

                // Delete all chunks
                int chunkIndex = 0;
                while (PlayerPrefs.HasKey(SAVE_CHUNK_PREFIX + chunkIndex))
                {
                    PlayerPrefs.DeleteKey(SAVE_CHUNK_PREFIX + chunkIndex);
                    deletedKeys++;
                    chunkIndex++;
                }
                
                if (chunkIndex > 0)
                {
                    Debugger.Log($"[SaveManager] Deleted {chunkIndex} chunk(s)");
                }

                PlayerPrefs.Save();
                Debugger.Log($"[SaveManager] ✓ Save data deleted from PlayerPrefs (WebGL) - Total keys deleted: {deletedKeys}");
            }
            else
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    Debugger.Log($"[SaveManager] ✓ Save file deleted: {path}");
                }
                else
                {
                    Debugger.Log($"[SaveManager] No save file to delete at: {path}");
                }
            }
        }
        catch (System.Exception e)
        {
            Debugger.LogError($"[SaveManager] ✗ Failed to delete save data: {e.Message}");
        }
    }

    #region WebGL PlayerPrefs Methods

    /// <summary>
    /// Save data to PlayerPrefs, splitting into chunks if necessary
    /// </summary>
    private static void SaveToPlayerPrefs(string json)
    {
        try
        {
            Debugger.Log($"[SaveManager] SaveToPlayerPrefs - JSON length: {json.Length}, Chunk size limit: {MAX_PLAYERPREFS_SIZE}");
            
            // Clear existing chunks first
            int clearedChunks = 0;
            int chunkIndex = 0;
            while (PlayerPrefs.HasKey(SAVE_CHUNK_PREFIX + chunkIndex))
            {
                PlayerPrefs.DeleteKey(SAVE_CHUNK_PREFIX + chunkIndex);
                clearedChunks++;
                chunkIndex++;
            }
            
            if (clearedChunks > 0)
            {
                Debugger.Log($"[SaveManager] Cleared {clearedChunks} existing chunk(s)");
            }
            
            // Clear single key if it exists
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                PlayerPrefs.DeleteKey(SAVE_KEY);
                Debugger.Log($"[SaveManager] Cleared existing single save key");
            }

            // If data is small enough, save in single key
            if (json.Length <= MAX_PLAYERPREFS_SIZE)
            {
                Debugger.Log($"[SaveManager] Saving as single key (data fits in one chunk)");
                PlayerPrefs.SetString(SAVE_KEY, json);
                PlayerPrefs.Save();
                Debugger.Log($"[SaveManager] ✓ Saved to single key '{SAVE_KEY}'");
                
                // Verify it was saved
                if (PlayerPrefs.HasKey(SAVE_KEY))
                {
                    string verification = PlayerPrefs.GetString(SAVE_KEY);
                    Debugger.Log($"[SaveManager] ✓ Verification: Key exists, length={verification.Length}");
                }
                else
                {
                    Debugger.LogError($"[SaveManager] ✗ Verification failed: Key does not exist after save!");
                }
            }
            else
            {
                // Split into chunks
                Debugger.Log($"[SaveManager] Data too large, splitting into chunks");
                chunkIndex = 0;
                int offset = 0;
                int totalChunks = Mathf.CeilToInt((float)json.Length / MAX_PLAYERPREFS_SIZE);
                
                Debugger.Log($"[SaveManager] Will create {totalChunks} chunk(s)");

                while (offset < json.Length)
                {
                    int chunkSize = Mathf.Min(MAX_PLAYERPREFS_SIZE, json.Length - offset);
                    string chunk = json.Substring(offset, chunkSize);
                    string chunkKey = SAVE_CHUNK_PREFIX + chunkIndex;
                    
                    PlayerPrefs.SetString(chunkKey, chunk);
                    Debugger.Log($"[SaveManager] Saved chunk {chunkIndex}/{totalChunks - 1}: key='{chunkKey}', size={chunkSize} chars, offset={offset}");
                    
                    offset += chunkSize;
                    chunkIndex++;
                }

                PlayerPrefs.Save();
                Debugger.Log($"[SaveManager] ✓ Saved {chunkIndex} chunk(s), total size: {json.Length} chars");
                
                // Verify chunks were saved
                for (int i = 0; i < chunkIndex; i++)
                {
                    string chunkKey = SAVE_CHUNK_PREFIX + i;
                    if (!PlayerPrefs.HasKey(chunkKey))
                    {
                        Debugger.LogError($"[SaveManager] ✗ Verification failed: Chunk {i} ('{chunkKey}') does not exist after save!");
                    }
                }
                Debugger.Log($"[SaveManager] ✓ All {chunkIndex} chunks verified");
            }
        }
        catch (System.Exception e)
        {
            Debugger.LogError($"[SaveManager] ✗ Failed to save to PlayerPrefs: {e.Message}");
            Debugger.LogError($"[SaveManager] Stack trace: {e.StackTrace}");
        }
    }

    /// <summary>
    /// Load data from PlayerPrefs, combining chunks if necessary
    /// </summary>
    private static string LoadFromPlayerPrefs()
    {
        try
        {
            Debugger.Log($"[SaveManager] LoadFromPlayerPrefs - Checking for save data");
            
            // Check if data is stored in single key
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                Debugger.Log($"[SaveManager] Found single-key save data");
                string data = PlayerPrefs.GetString(SAVE_KEY);
                Debugger.Log($"[SaveManager] Loaded {data.Length} characters from single key");
                return data;
            }

            // Check for chunked data
            if (PlayerPrefs.HasKey(SAVE_CHUNK_PREFIX + "0"))
            {
                Debugger.Log($"[SaveManager] Found chunked save data");
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                int chunkIndex = 0;

                while (PlayerPrefs.HasKey(SAVE_CHUNK_PREFIX + chunkIndex))
                {
                    string chunkKey = SAVE_CHUNK_PREFIX + chunkIndex;
                    string chunk = PlayerPrefs.GetString(chunkKey);
                    sb.Append(chunk);
                    Debugger.Log($"[SaveManager] Loaded chunk {chunkIndex}: key='{chunkKey}', size={chunk.Length} chars");
                    chunkIndex++;
                }

                string result = sb.ToString();
                Debugger.Log($"[SaveManager] ✓ Combined {chunkIndex} chunk(s) into {result.Length} total characters");
                return result;
            }

            Debugger.LogWarning($"[SaveManager] No save data found - neither single key nor chunks exist");
            return null;
        }
        catch (System.Exception e)
        {
            Debugger.LogError($"[SaveManager] ✗ Failed to load from PlayerPrefs: {e.Message}");
            Debugger.LogError($"[SaveManager] Stack trace: {e.StackTrace}");
            return null;
        }
    }

    /// <summary>
    /// Run diagnostics to test WebGL save/load functionality
    /// </summary>
    public static void RunDiagnostics()
    {
        Debugger.Log($"[SaveManager] ========== DIAGNOSTICS START ==========");
        Debugger.Log($"[SaveManager] Platform: {Application.platform}");
        Debugger.Log($"[SaveManager] IsWebGL: {IsWebGL}");
        Debugger.Log($"[SaveManager] Persistent data path: {Application.persistentDataPath}");
        
        if (IsWebGL)
        {
            Debugger.Log($"[SaveManager] Testing PlayerPrefs write/read...");
            
            // Test 1: Simple write/read
            string testKey = "SaveManager_Test";
            string testValue = "Test123";
            PlayerPrefs.SetString(testKey, testValue);
            PlayerPrefs.Save();
            
            if (PlayerPrefs.HasKey(testKey))
            {
                string readValue = PlayerPrefs.GetString(testKey);
                if (readValue == testValue)
                {
                    Debugger.Log($"[SaveManager] ✓ PlayerPrefs basic test PASSED");
                }
                else
                {
                    Debugger.LogError($"[SaveManager] ✗ PlayerPrefs basic test FAILED - wrote '{testValue}', read '{readValue}'");
                }
            }
            else
            {
                Debugger.LogError($"[SaveManager] ✗ PlayerPrefs basic test FAILED - key not found after save");
            }
            
            PlayerPrefs.DeleteKey(testKey);
            
            // Test 2: Check save data status
            bool hasSingleKey = PlayerPrefs.HasKey(SAVE_KEY);
            bool hasChunkedKey = PlayerPrefs.HasKey(SAVE_CHUNK_PREFIX + "0");
            
            Debugger.Log($"[SaveManager] Save data status:");
            Debugger.Log($"[SaveManager]   - Single key ({SAVE_KEY}): {hasSingleKey}");
            Debugger.Log($"[SaveManager]   - Chunked key ({SAVE_CHUNK_PREFIX}0): {hasChunkedKey}");
            
            if (hasSingleKey)
            {
                string data = PlayerPrefs.GetString(SAVE_KEY);
                Debugger.Log($"[SaveManager]   - Single key size: {data.Length} characters");
            }
            
            if (hasChunkedKey)
            {
                int chunkCount = 0;
                int totalSize = 0;
                while (PlayerPrefs.HasKey(SAVE_CHUNK_PREFIX + chunkCount))
                {
                    string chunk = PlayerPrefs.GetString(SAVE_CHUNK_PREFIX + chunkCount);
                    totalSize += chunk.Length;
                    chunkCount++;
                }
                Debugger.Log($"[SaveManager]   - Chunk count: {chunkCount}");
                Debugger.Log($"[SaveManager]   - Total size: {totalSize} characters");
            }
        }
        else
        {
            Debugger.Log($"[SaveManager] File system diagnostics:");
            Debugger.Log($"[SaveManager]   - Save path: {path}");
            Debugger.Log($"[SaveManager]   - File exists: {File.Exists(path)}");
            if (File.Exists(path))
            {
                FileInfo info = new FileInfo(path);
                Debugger.Log($"[SaveManager]   - File size: {info.Length} bytes");
            }
        }
        
        Debugger.Log($"[SaveManager] ========== DIAGNOSTICS END ==========");
    }

    #endregion
}