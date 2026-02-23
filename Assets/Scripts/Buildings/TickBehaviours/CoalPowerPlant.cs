using UnityEngine;

[CreateAssetMenu(menuName = "CityBuilder/TickBehaviours/CoalPowerPlant")]
class CoalPowerPlant : TickBehaviour
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

        float outputMult = 1f;
        float emissionMult = 1f;

        if (state.HasUpgrade("Preheated")) outputMult += 0.2f;
        if (state.HasUpgrade("SupplementaryFiring"))
        {
            outputMult += 0.4f;
            emissionMult += 0.5f;
        }
        if (state.HasUpgrade("OverfireAir")) emissionMult -= 0.15f;
        if (state.HasUpgrade("SulfurScrubbers")) emissionMult -= 0.25f;

        util.efficiency -= (delta / def.Utility.DegradeTime) * (1 - GameState.Instance.Settings.MinimumEfficiency);
        util.efficiency = Mathf.Max(util.efficiency, GameState.Instance.Settings.MinimumEfficiency);

        GameState.Instance.Power += Mathf.FloorToInt(output * outputMult * util.efficiency);
        GameState.Instance.EmissionsDelta += Mathf.FloorToInt(emission * emissionMult);

        state.AddTime(delta);
    }
}