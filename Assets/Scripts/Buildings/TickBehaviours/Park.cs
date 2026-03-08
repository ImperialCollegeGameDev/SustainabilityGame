using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CityBuilder/TickBehaviours/Park")]
class Park : TickBehaviour
{
    public override void Tick(TileObject tileObject, float delta)
    {
        TileObjectDefinition def = tileObject.Definition;
        if (tileObject is not PollutionReducerTileObject pr)
        {
            Debugger.LogError("TickBehaviour applied to non-PollutionReducerTileObject.");
            return;
        }

        float emission = def.PollutionReducer.EmissionReduction * delta;
        float mult = def.PollutionReducer.EmissionReducingCapacity / Mathf.Max(1f, GameState.Instance.PreviousEmissionsDelta);
        mult = Mathf.Clamp(mult, 0, 1);
        emission *= mult;

        pr.emissionMultiplier = mult;
        pr.emissionReduction = emission;

        GameState.Instance.EmissionsReductionDelta -= pr.emissionReduction;

        pr.emissionReduction /= delta;

        tileObject.AddTime(delta);

        List<StatRow> stats = new List<StatRow>();

        stats.Add(new StatRow("Emission Reduction", NumberFormatter.Format(pr.emissionReduction), Color.green));
        stats.Add(new StatRow("Effectiveness", $"{Mathf.RoundToInt(pr.emissionMultiplier * 100)}%", Color.cyan));

        tileObject.Stats = stats;
    }
}