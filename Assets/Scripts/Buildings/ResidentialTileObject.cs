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
    public float occupancy { get; private set; } = 0; // The occupancy at this current moment for this particular TileObject, definition has the max occupancy

    private void OnDestroy()
    {
        GameState.Instance.population -= Mathf.FloorToInt(occupancy);
    }

    public override void Tick(float delta)
    {
        if (Mathf.FloorToInt(occupancy) > Definition.Residential.MaxOccupancy) occupancy = Definition.Residential.MaxOccupancy;

        float occupancyDelta = (Definition.Residential.MaxOccupancy - occupancy) * 0.05f * delta;
        float sadPeopleLeavingLmao = (1 - (GameState.Instance.happiness / 100)) * 5 * delta;
        occupancyDelta -= sadPeopleLeavingLmao;

        if (occupancy + occupancyDelta < 0) occupancyDelta = -occupancy;

        occupancy += occupancyDelta;
        occupancy = Mathf.Min(occupancy, Definition.Residential.MaxOccupancy);

        if (Definition.Residential.MaxOccupancy - occupancy < 0.1f)
        {
            occupancy = Definition.Residential.MaxOccupancy;
        }
    }
}