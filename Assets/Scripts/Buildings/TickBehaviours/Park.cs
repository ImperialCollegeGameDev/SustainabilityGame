using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CityBuilder/TickBehaviours/Park")]
class Park : TickBehaviour
{
    public override void Tick(TileObject tileObject, float delta)
    {
        TileObjectDefinition def = tileObject.Definition;
        if (tileObject is not UtilityTileObject util)
        {
            Debug.LogError("TickBehaviour UtilityDefault applied to non-utility tile object.");
            return;
        }

        float emission = def.Utility.Emission * delta;
        float mult = 50f / Mathf.Max(1f, GameState.Instance.PreviousEmissionsDelta);
        mult = Mathf.Min(1, mult);
        emission *= mult;
        TileTypeState state = TileStateCatalog.Instance.Get(def.Id);

        // Store calculated values in the utility object
        util.emissionMultiplier = mult;
        util.actualEmission = Mathf.FloorToInt(emission);

        GameState.Instance.EmissionsReductionDelta += util.actualEmission;

        tileObject.AddTime(delta);
    }

    public override List<StatRow> GetStats(TileObject tileObject)
    {
        List<StatRow> stats = new List<StatRow>();
        
        if (tileObject is not UtilityTileObject util)
        {
            return stats;
        }

        stats.Add(new StatRow("Emission Reduction", (-util.actualEmission).ToString(), Color.green));
        stats.Add(new StatRow("Effectiveness", $"{Mathf.RoundToInt(util.emissionMultiplier * 100)}%", Color.cyan));

        return stats;
    }
}