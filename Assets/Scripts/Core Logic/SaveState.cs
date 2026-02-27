using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveState
{
    public long money;
    public float happiness;
    public float emissions;
    public int maxPopulation; // Added max population to save state
    public string playerIdentity; // Unity Authentication ID to save state
    public string playerName; // Player display name to save state

    public List<TileSaveData> tiles;
}

[System.Serializable]
public class TileSaveData
{
    public Vector2Int gridPosition;
    public TileObjectDefinition def;

    // Optional live data
    public float occupancy = 0f;
}
