using System;
using UnityEngine;

public class ResidentialTileObject : TileObject
{
    [NonSerialized] public float occupancy = 0; // The occupancy at this current moment for this particular TileObject, definition has the max occupancy

    private void OnDestroy()
    {
        GameState.Instance.population -= Mathf.FloorToInt(occupancy);
    }
}