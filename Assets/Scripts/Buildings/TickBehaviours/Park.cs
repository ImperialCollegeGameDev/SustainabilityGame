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
            Debug.LogError("TickBehaviour applied to non-PollutionReducerTileObject.");
            return;
        }

        float emission = def.PollutionReducer.EmissionReduction * delta;
        float mult = def.PollutionReducer.EmissionReducingCapacity / Mathf.Max(1f, GameState.Instance.PreviousEmissionsDelta);
        mult = Mathf.Clamp(mult, 0, 1);
        Debug.Log($"Multiplier: {mult}, Fraction: {def.PollutionReducer.EmissionReducingCapacity} / {GameState.Instance.PreviousEmissionsDelta}");
        emission *= mult;

        pr.emissionMultiplier = mult;
        pr.emissionReduction = emission;

        GameState.Instance.EmissionsReductionDelta -= pr.emissionReduction;
        Debug.Log($"Subtracted {pr.emissionReduction} from delta");

        pr.emissionReduction /= delta;

        tileObject.AddTime(delta);
    }

    public override List<StatRow> GetStats(TileObject tileObject)
    {
        List<StatRow> stats = new List<StatRow>();
        
        if (tileObject is not PollutionReducerTileObject pr)
        {
            Debug.LogError("GetStats called on non-PollutionReducerTileObject.");
            return stats;
        }

        stats.Add(new StatRow("Emission Reduction", pr.emissionReduction.ToString(), Color.green));
        stats.Add(new StatRow("Effectiveness", $"{Mathf.RoundToInt(pr.emissionMultiplier * 100)}%", Color.cyan));

        return stats;
    }
}