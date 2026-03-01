using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CityBuilder/TickBehaviours/WindFarm")]
class WindFarm : UtilityDefault
{
    public override void Tick(TileObject tileObject, float delta)
    {
        TileObjectDefinition def = tileObject.Definition;
        if (tileObject is not UtilityTileObject util)
        {
            Debug.LogError("TickBehaviour applied to incorrect tile object.");
            return;
        }

        util.repairCostMult = 0.5f;

        int output = def.Utility.Output;
        float emission = def.Utility.Emission * delta;

        float outputMult = 1f;
        float emissionMult = 1f;
        float degradeMult = 1f;

        outputMult -= 0.02f * GridManager.Instance.GetWithinRadius(tileObject, 2, obj => true).Count;

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