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
            SaveState data = new SaveState();

            Debug.Log($"{GameState.Instance}");

            // Get data from GameState
            if (GameState.Instance != null)
            {
                data.money = GameState.Instance.money;
                data.happiness = GameState.Instance.happiness;
                data.emissions = GameState.Instance.TotalEmissions;
                data.maxPopulation = GameState.Instance.GetMaxPopulation();
            }

            // Get player identity and name from Main if available
            if (Main.Instance != null)
            {
                data.playerIdentity = Main.Instance.GetCurrentPlayerIdentity();
                data.playerName = Main.Instance.GetCurrentPlayerDisplayName();
            }

            data.tiles = new List<TileSaveData>();

            // Get tile data from GridManager
            if (GridManager.Instance != null)
            {
                foreach (var tileObj in GridManager.Instance.GetTileObjects())
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
            }

            string json = JsonUtility.ToJson(data, true);

            if (IsWebGL)
            {
                SaveToPlayerPrefs(json);
                Debug.Log($"[SaveManager] Game saved to PlayerPrefs (WebGL) with identity: {data.playerIdentity}, name: {data.playerName}");
            }
            else
            {
                File.WriteAllText(path, json);
                Debug.Log($"[SaveManager] Game saved to {path} with identity: {data.playerIdentity}, name: {data.playerName}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to save game: {e.Message}");
        }
    }

    public static SaveState Load()
    {
        try
        {
            string json;

            if (IsWebGL)
            {
                json = LoadFromPlayerPrefs();
                if (string.IsNullOrEmpty(json))
                {
                    Debug.LogWarning("[SaveManager] No save data found in PlayerPrefs (WebGL)");
                    return null;
                }
                Debug.Log("[SaveManager] Game loaded from PlayerPrefs (WebGL)");
            }
            else
            {
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[SaveManager] No save file found at {path}");
                    return null;
                }

                json = File.ReadAllText(path);
                Debug.Log($"[SaveManager] Game loaded from {path}");
            }

            SaveState data = JsonUtility.FromJson<SaveState>(json);
            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to load game: {e.Message}");
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
            if (IsWebGL)
            {
                return PlayerPrefs.HasKey(SAVE_KEY) || PlayerPrefs.HasKey(SAVE_CHUNK_PREFIX + "0");
            }
            else
            {
                return File.Exists(path);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Error checking for save data: {e.Message}");
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
            if (IsWebGL)
            {
                // Delete main key
                if (PlayerPrefs.HasKey(SAVE_KEY))
                {
                    PlayerPrefs.DeleteKey(SAVE_KEY);
                }

                // Delete all chunks
                int chunkIndex = 0;
                while (PlayerPrefs.HasKey(SAVE_CHUNK_PREFIX + chunkIndex))
                {
                    PlayerPrefs.DeleteKey(SAVE_CHUNK_PREFIX + chunkIndex);
                    chunkIndex++;
                }

                PlayerPrefs.Save();
                Debug.Log("[SaveManager] Save data deleted from PlayerPrefs (WebGL)");
            }
            else
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    Debug.Log("[SaveManager] Save file deleted");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to delete save data: {e.Message}");
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
            // Clear existing chunks first
            int chunkIndex = 0;
            while (PlayerPrefs.HasKey(SAVE_CHUNK_PREFIX + chunkIndex))
            {
                PlayerPrefs.DeleteKey(SAVE_CHUNK_PREFIX + chunkIndex);
                chunkIndex++;
            }

            // If data is small enough, save in single key
            if (json.Length <= MAX_PLAYERPREFS_SIZE)
            {
                PlayerPrefs.SetString(SAVE_KEY, json);
                PlayerPrefs.Save();
            }
            else
            {
                // Split into chunks
                chunkIndex = 0;
                int offset = 0;

                while (offset < json.Length)
                {
                    int chunkSize = Mathf.Min(MAX_PLAYERPREFS_SIZE, json.Length - offset);
                    string chunk = json.Substring(offset, chunkSize);
                    PlayerPrefs.SetString(SAVE_CHUNK_PREFIX + chunkIndex, chunk);
                    
                    offset += chunkSize;
                    chunkIndex++;
                }

                // Remove the single key if it exists
                if (PlayerPrefs.HasKey(SAVE_KEY))
                {
                    PlayerPrefs.DeleteKey(SAVE_KEY);
                }

                PlayerPrefs.Save();
                Debug.Log($"[SaveManager] Large save data split into {chunkIndex} chunks");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to save to PlayerPrefs: {e.Message}");
        }
    }

    /// <summary>
    /// Load data from PlayerPrefs, combining chunks if necessary
    /// </summary>
    private static string LoadFromPlayerPrefs()
    {
        try
        {
            // Check if data is stored in single key
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                return PlayerPrefs.GetString(SAVE_KEY);
            }

            // Check for chunked data
            if (PlayerPrefs.HasKey(SAVE_CHUNK_PREFIX + "0"))
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                int chunkIndex = 0;

                while (PlayerPrefs.HasKey(SAVE_CHUNK_PREFIX + chunkIndex))
                {
                    string chunk = PlayerPrefs.GetString(SAVE_CHUNK_PREFIX + chunkIndex);
                    sb.Append(chunk);
                    chunkIndex++;
                }

                Debug.Log($"[SaveManager] Loaded data from {chunkIndex} chunks");
                return sb.ToString();
            }

            return null;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to load from PlayerPrefs: {e.Message}");
            return null;
        }
    }

    #endregion
}