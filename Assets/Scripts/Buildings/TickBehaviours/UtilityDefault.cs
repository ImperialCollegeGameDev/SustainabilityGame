using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CityBuilder/TickBehaviours/UtilityDefault")]
class UtilityDefault : TickBehaviour
{
    public override void Tick(TileObject tileObject, float delta)
    {
        TileObjectDefinition def = tileObject.Definition;
        if (tileObject is not UtilityTileObject util)
        {
            Debug.LogError("TickBehaviour UtilityDefault applied to non-utility tile object.");
            return;
        }

        int output = def.Utility.Output;
        float emission = def.Utility.Emission * delta;

        TileTypeState state = TileStateCatalog.Instance.Get(def.Id);

        util.efficiency -= (delta / def.Utility.DegradeTime) * (1 - GameState.Instance.Settings.MinimumEfficiency);
        util.efficiency = Mathf.Max(util.efficiency, GameState.Instance.Settings.MinimumEfficiency);

        util.outputMultiplier = 1f;
        util.emissionMultiplier = 1f;
        util.degradeMultiplier = 1f;
        util.actualOutput = output * util.outputMultiplier * util.efficiency;
        util.actualEmission = emission * util.emissionMultiplier;

        GameState.Instance.Power += Mathf.FloorToInt(util.actualOutput);
        GameState.Instance.EmissionsDelta += util.actualEmission;

        tileObject.AddTime(delta);
    }

    public override List<StatRow> GetStats(TileObject tileObject)
    {
        List<StatRow> stats = new List<StatRow>();

        if (tileObject is not UtilityTileObject util)
        {
            return stats;
        }

        stats.Add(new StatRow("Power Output", util.actualOutput.ToString(), Color.green));
        stats.Add(new StatRow("Emissions", util.actualEmission.ToString(), Color.red));
        stats.Add(new StatRow("Output Multiplier", $"{Mathf.RoundToInt(util.outputMultiplier * 100)}%", Color.cyan));
        stats.Add(new StatRow("Emission Multiplier", $"{Mathf.RoundToInt(util.emissionMultiplier * 100)}%", Color.magenta));

        return stats;
    }
}