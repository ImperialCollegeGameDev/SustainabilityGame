using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CityBuilder/TickBehaviours/ResidenceDefault")]
class ResidenceDefault : TickBehaviour
{
    public override void Tick(TileObject tileObject, float delta)
    {
        TileObjectDefinition def = tileObject.Definition;
        if (tileObject is not ResidentialTileObject resTile)
        {
            Debug.LogError("TickBehaviour ResidenceDefault applied to non-residential tile object.");
            return;
        }

        float occupancy = resTile.occupancy;

        if (Mathf.FloorToInt(occupancy) > def.Residential.MaxOccupancy) occupancy = def.Residential.MaxOccupancy;

        float occupancyDelta = (def.Residential.MaxOccupancy - occupancy) * 0.05f * delta;
        float sadPeopleLeaving = (1 - (GameState.Instance.happiness / 100)) * def.Residential.MaxOccupancy  * 0.05f * delta;
        occupancyDelta -= sadPeopleLeaving;

        if (occupancy + occupancyDelta < 0) occupancyDelta = -occupancy;

        occupancy += occupancyDelta;
        occupancy = Mathf.Min(occupancy, def.Residential.MaxOccupancy);

        if (def.Residential.MaxOccupancy - occupancy < 0.1f)
        {
            occupancy = def.Residential.MaxOccupancy;
        }

        resTile.occupancy = occupancy;
    }

    public override List<StatRow> GetStats(TileObject tileObject)
    {
        List<StatRow> stats = new List<StatRow>();
        
        if (tileObject is not ResidentialTileObject resTile)
        {
            return stats;
        }

        TileObjectDefinition def = tileObject.Definition;
        
        stats.Add(new StatRow("Current Occupancy", Mathf.FloorToInt(resTile.occupancy).ToString(), Color.green));
        stats.Add(new StatRow("Max Occupancy", def.Residential.MaxOccupancy.ToString(), Color.yellow));
        stats.Add(new StatRow("Occupancy %", $"{Mathf.RoundToInt((resTile.occupancy / def.Residential.MaxOccupancy) * 100)}%", Color.cyan));

        return stats;
    }
}