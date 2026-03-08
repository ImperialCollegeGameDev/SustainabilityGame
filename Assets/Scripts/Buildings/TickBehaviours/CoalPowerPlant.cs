using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CityBuilder/TickBehaviours/CoalPowerPlant")]
class CoalPowerPlant : UtilityDefault
{
    public override void Tick(TileObject tileObject, float delta)
    {
        TileObjectDefinition def = tileObject.Definition;
        if (tileObject is not UtilityTileObject util)
        {
            Debugger.LogError("TickBehaviour UtilityDefault applied to non-utility tile object.");
            return;
        }

        util.repairCostMult = 1.5f;

        int output = def.Utility.Output;
        float emission = def.Utility.Emission * delta;

        float outputMult = 1f;
        float emissionMult = 1f;
        float degradeMult = 1f;

        if (tileObject.HasUpgrade("Preheated")) outputMult += 0.2f;
        if (tileObject.HasUpgrade("SupplementaryFiring"))
        {
            outputMult += 0.4f;
            emissionMult += 0.5f;
        }
        if (tileObject.HasUpgrade("OverfireAir")) emissionMult -= 0.15f;
        if (tileObject.HasUpgrade("SulfurScrubbers")) emissionMult -= 0.25f;
        if (tileObject.HasUpgrade("Cogeneration"))
        {
            emissionMult -= 0.1f;
            outputMult += 0.05f * GridManager.Instance.GetWithinRadius(tileObject.Origin, tileObject.Definition.Size, 1, obj => obj.Definition.Category == BuildingCategory.Residential).Count;
        }
        if (tileObject.HasUpgrade("Maintenance"))
        {
            degradeMult -= 0.2f;
            util.repairCostMult *= 1.1f;
        }
        if (tileObject.HasUpgrade("Calibration"))
        {
            if (util.efficiency > 0.8f) degradeMult -= 0.3f;
        }
        if (tileObject.HasUpgrade("Ash"))
        {
            emissionMult -= 0.2f;
            degradeMult -= 0.1f;
        }

        util.efficiency -= degradeMult * (delta / def.Utility.DegradeTime) * (1 - GameState.Instance.Settings.MinimumEfficiency);
        util.efficiency = Mathf.Max(util.efficiency, GameState.Instance.Settings.MinimumEfficiency);

        float actualOutput = output * outputMult * util.efficiency;
        float actualEmission = emission * emissionMult;

        GameState.Instance.Power += Mathf.FloorToInt(actualOutput);
        GameState.Instance.EmissionsDelta += actualEmission;

        actualEmission /= delta;

        tileObject.AddTime(delta);
        UpdateStatus(util);

        List<StatRow> stats = new List<StatRow>();

        stats.Add(new StatRow("Power Output", actualOutput.ToString(), Color.green));
        stats.Add(new StatRow("Emissions", actualEmission.ToString(), Color.red));
        stats.Add(new StatRow("Output Multiplier", $"{Mathf.RoundToInt(outputMult * 100)}%", Color.cyan));
        stats.Add(new StatRow("Emission Multiplier", $"{Mathf.RoundToInt(emissionMult * 100)}%", Color.magenta));

        tileObject.Stats = stats;
    }
}