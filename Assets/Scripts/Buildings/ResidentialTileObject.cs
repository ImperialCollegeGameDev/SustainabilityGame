using System;
using UnityEngine;

public class ResidentialTileObject : TileObject
{
    [NonSerialized] public float occupancy = 0; // The occupancy at this current moment for this particular TileObject, definition has the max occupancy
    [NonSerialized] public bool canAccessPower = false;
    private void OnDestroy()
    {
        GameState.Instance.population -= Mathf.FloorToInt(occupancy);
    }

    public override void GridUpdate()
    {
        UpdateCanAccessPower();
    }

    private void UpdateCanAccessPower()
    {
        canAccessPower = GridManager.Instance
            .GetWithinRadius(this, GameState.Instance.Settings.RequiredProximityToPower,
            tileObj => tileObj.Definition.CountsAsPowerSource)
            .Count > 0;
        if (!canAccessPower && Status != BuildingStatus.NeedsPower)
        {
            SetStatus(BuildingStatus.NeedsPower);
        }
        else if (canAccessPower && Status == BuildingStatus.NeedsPower)
        {
            SetStatus(BuildingStatus.None);
        }
    }
}