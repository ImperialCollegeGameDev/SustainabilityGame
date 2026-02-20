using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class ResidentialTileObject : TileObject
{
    public float occupancy = 0; // The occupancy at this current moment for this particular TileObject, definition has the max occupancy

    private void OnDestroy()
    {
        GameState.Instance.population -= Mathf.FloorToInt(occupancy);
    }
}