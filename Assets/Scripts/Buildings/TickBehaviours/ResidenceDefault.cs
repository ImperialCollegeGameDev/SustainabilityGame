using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CityBuilder/TickBehaviours/ResidenceDefault")]
class ResidenceDefault : TickBehaviour
{
    [Header("Location Happiness Modifiers")]
    [SerializeField] private Dictionary<string, float> locationHappinessModifiers = new Dictionary<string, float>()
    {
        { "Park", 0.05f },
        { "Coal", -0.2f },
        { "Geothermal", -0.1f },
        { "Nuclear", -0.6f },
        { "Neighbourhood", 0.03f },
        { "Neighbourhood-2", 0.02f },
        { "KP", -0.03f },
        { "central", -0.05f },
    };

    public override void Tick(TileObject tileObject, float delta)
    {
        TileObjectDefinition def = tileObject.Definition;
        if (tileObject is not ResidentialTileObject resTile)
        {
            Debug.LogError("TickBehaviour ResidenceDefault applied to non-residential tile object.");
            return;
        }

        // Calculate LocationHappiness
        float locationHappiness = CalculateLocationHappiness(resTile);

        // Calculate LocalHappiness (power access * location happiness)
        float powerFactor = resTile.canAccessPower ? 1f : 0f;
        resTile.LocalHappiness = powerFactor * locationHappiness;

        float occupancy = resTile.occupancy;

        if (Mathf.FloorToInt(occupancy) > def.Residential.MaxOccupancy) occupancy = def.Residential.MaxOccupancy;

        // Use LocalHappiness to affect occupancy growth
        float baseGrowthRate = 0.05f;
        float happinessModifiedGrowth = baseGrowthRate * resTile.LocalHappiness;
        
        float occupancyDelta = (def.Residential.MaxOccupancy - occupancy) * happinessModifiedGrowth * delta;
        float sadPeopleLeaving = (1 - (GameState.Instance.happiness / 100)) * def.Residential.MaxOccupancy * 0.05f * delta;
        
        // Additional leaving based on local unhappiness
        float localUnhappinessLeaving = (1 - resTile.LocalHappiness) * occupancy * 0.03f * delta;
        
        occupancyDelta -= sadPeopleLeaving;
        occupancyDelta -= localUnhappinessLeaving;

        if (occupancy + occupancyDelta < 0) occupancyDelta = -occupancy;

        occupancy += occupancyDelta;
        occupancy = Mathf.Min(occupancy, def.Residential.MaxOccupancy);

        if (def.Residential.MaxOccupancy - occupancy < 0.1f)
        {
            occupancy = def.Residential.MaxOccupancy;
        }

        resTile.occupancy = occupancy;

        List<StatRow> stats = new List<StatRow>();

        stats.Add(new StatRow("Current Occupancy", Mathf.FloorToInt(resTile.occupancy).ToString(), Color.green));
        stats.Add(new StatRow("Max Occupancy", def.Residential.MaxOccupancy.ToString(), Color.yellow));

        Color happinessColor = resTile.LocalHappiness >= 0.8f ? Color.green :
                               resTile.LocalHappiness >= 0.5f ? Color.yellow : Color.red;
        stats.Add(new StatRow("Local Happiness", $"{Mathf.RoundToInt(resTile.LocalHappiness * 100)}%", happinessColor));

        tileObject.Stats = stats;
    }

    private float CalculateLocationHappiness(ResidentialTileObject resTile)
    {
        float locationHappiness = 1f; // Base happiness is neutral (1.0)

        // Get adjacent tiles (radius 1 includes diagonals)
        var adjacentTiles = GridManager.Instance.GetWithinRadius(
            resTile.Origin, 
            resTile.Definition.Size, 
            1, 
            null // Get all tile objects
        );

        // Apply happiness modifiers based on adjacent buildings
        foreach (var adjacentTile in adjacentTiles)
        {
            if (adjacentTile == null || adjacentTile.Definition == null)
                continue;

            string buildingId = adjacentTile.Definition.Id;
            
            if (locationHappinessModifiers.ContainsKey(buildingId))
            {
                locationHappiness += locationHappinessModifiers[buildingId];
            }
        }

        // Clamp happiness between 0 and 2 (allows for very good or very bad locations)
        locationHappiness = Mathf.Clamp(locationHappiness, 0f, 2f);

        return locationHappiness;
    }
}