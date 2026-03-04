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
            Debug.LogError($"TickBehaviour UtilityDefault applied to non-utility tile object {def.DisplayName}.");
            return;
        }

        int output = def.Utility.Output;
        float emission = def.Utility.Emission * delta;

        TileTypeState state = TileStateCatalog.Instance.Get(def.Id);

        util.efficiency -= (delta / def.Utility.DegradeTime) * (1 - GameState.Instance.Settings.MinimumEfficiency);
        util.efficiency = Mathf.Max(util.efficiency, GameState.Instance.Settings.MinimumEfficiency);

        float outputMultiplier = 1f;
        float emissionMultiplier = 1f;
        float actualOutput = output * outputMultiplier * util.efficiency;
        float actualEmission = emission * emissionMultiplier;

        GameState.Instance.Power += Mathf.FloorToInt(actualOutput);
        GameState.Instance.EmissionsDelta += actualEmission;

        actualEmission /= delta;

        tileObject.AddTime(delta);
        UpdateStatus(util);

        List<StatRow> stats = new List<StatRow>();

        stats.Add(new StatRow("Power Output", actualOutput.ToString(), Color.green));
        stats.Add(new StatRow("Emissions", actualEmission.ToString(), Color.red));
        stats.Add(new StatRow("Output Multiplier", $"{Mathf.RoundToInt(outputMultiplier * 100)}%", Color.cyan));
        stats.Add(new StatRow("Emission Multiplier", $"{Mathf.RoundToInt(emissionMultiplier * 100)}%", Color.magenta));

        tileObject.Stats = stats;
    }

    protected void UpdateStatus(UtilityTileObject util)
    {
        if (util.efficiency > 0.7f)
        {
            if (util.Status != BuildingStatus.None)
                util.SetStatus(BuildingStatus.None);
        } else
        {
            if (util.Status != BuildingStatus.NeedsRepair)
                util.SetStatus(BuildingStatus.NeedsRepair);
        }
    }
}