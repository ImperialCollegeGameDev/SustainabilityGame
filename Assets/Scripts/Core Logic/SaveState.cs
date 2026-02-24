using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveState
{
    public long money;
    public float happiness;
    public float emissions;

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
