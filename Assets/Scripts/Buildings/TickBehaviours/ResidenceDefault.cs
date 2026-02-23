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
}