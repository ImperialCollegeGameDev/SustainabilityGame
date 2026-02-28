using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CityBuilder/TickBehaviours/SolarPark")]
class SolarPark : UtilityDefault
{
    public override void Tick(TileObject tileObject, float delta)
    {
        TileObjectDefinition def = tileObject.Definition;
        if (tileObject is not UtilityTileObject util)
        {
            Debug.LogError("TickBehaviour UtilityDefault applied to non-utility tile object.");
            return;
        }

        util.repairCostMult = 0.5f;

        int output = def.Utility.Output;
        float emission = def.Utility.Emission * delta;

        float outputMult = 1f;
        float emissionMult = 1f;
        float degradeMult = 1f;

        bool canOutputAtNight = false;
        outputMult -= 0.02f * GridManager.Instance.GetWithinRadius(tileObject, 2, obj => obj.Definition != tileObject.Definition).Count;

        if (tileObject.HasUpgrade("Monocrystalline")) outputMult += 0.15f;
        if (tileObject.HasUpgrade("AxisTracking")) outputMult += 0.25f;
        if (tileObject.HasUpgrade("SmartInverters"))
        {
            if (GridManager.Instance.GetWithinRadius(tileObject, 1, obj => obj.Definition == tileObject.Definition).Count >= 1)
            {
                outputMult += 0.2f;
            }
        }
        if (tileObject.HasUpgrade("Rooftop"))
        {
            outputMult += 0.05f * GridManager.Instance.GetWithinRadius(tileObject, 1, obj => obj.Definition.Category == BuildingCategory.Residential).Count;
        }
        if (tileObject.HasUpgrade("Battery") && !DayNight.Instance.IsDaytime)
        {
            outputMult *= 0.4f;
            canOutputAtNight = true;
        }

        if (!canOutputAtNight && !DayNight.Instance.IsDaytime)
        {
            outputMult = 0f;
            emissionMult = 0f;
            degradeMult = 0.7f;
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

        util.actualEmission /= delta;

        tileObject.AddTime(delta);
        UpdateStatus(util);
    }
}