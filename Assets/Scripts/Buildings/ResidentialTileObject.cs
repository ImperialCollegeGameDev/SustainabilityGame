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
    private float occupancy = 0; // The occupancy at this current moment for this particular TileObject, definition has the max occupancy

    private void OnDestroy()
    {
        GameState.Instance.population -= Mathf.FloorToInt(occupancy);
    }

    public override void Tick(float delta)
    {
        if (Mathf.FloorToInt(occupancy) >= Definition.Residential.MaxOccupancy) return;

        int before = Mathf.FloorToInt(occupancy);

        float occupancyDelta = (Definition.Residential.MaxOccupancy - occupancy) * 0.05f * delta;
        occupancy += occupancyDelta;
        occupancy = Mathf.Min(occupancy, Definition.Residential.MaxOccupancy);

        if (Definition.Residential.MaxOccupancy - occupancy < 0.1f)
        {
            occupancy = Definition.Residential.MaxOccupancy;
        }

        int after = Mathf.FloorToInt(occupancy);
        int deltaInt = after - before;

        if (deltaInt > 0) GameState.Instance.population += deltaInt;
    }
}