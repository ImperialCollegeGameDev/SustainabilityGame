using UnityEngine;

[CreateAssetMenu(menuName = "CityBuilder/TickBehaviours/SolarPark")]
class SolarPark : TickBehaviour
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

        if (tileObject.HasUpgrade("Monocrystalline")) outputMult += 0.15f;
        if (tileObject.HasUpgrade("AxisTracking")) outputMult += 0.25f;
        if (tileObject.HasUpgrade("Bifacial"))
        {
            outputMult += 0.02f * (GridManager.Instance.GetWithinRadius(tileObject, 2, obj => obj.Definition == tileObject.Definition).Count
                + GridManager.Instance.GetEmptyWithinRadius(tileObject, 2));
        }
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

        GameState.Instance.Power += Mathf.FloorToInt(output * outputMult * util.efficiency);
        GameState.Instance.EmissionsDelta += emission * emissionMult;

        tileObject.AddTime(delta);
    }
}