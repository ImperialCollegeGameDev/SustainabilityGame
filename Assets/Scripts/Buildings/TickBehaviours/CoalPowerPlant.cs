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
            Debug.LogError("TickBehaviour UtilityDefault applied to non-utility tile object.");
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

        util.outputMultiplier = outputMult;
        util.emissionMultiplier = emissionMult;
        util.degradeMultiplier = degradeMult;
        util.actualOutput = output * outputMult * util.efficiency;
        util.actualEmission = emission * emissionMult;

        GameState.Instance.Power += Mathf.FloorToInt(util.actualOutput);
        GameState.Instance.EmissionsDelta += util.actualEmission;

        tileObject.AddTime(delta);
    }
}