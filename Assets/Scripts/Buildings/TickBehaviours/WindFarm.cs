using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CityBuilder/TickBehaviours/WindFarm")]
class WindFarm : UtilityDefault
{
    private float variationAmount = 0.3f;
    private float variationSpeed = 0.3f;
    
    private static Dictionary<int, WindData> windDataCache = new Dictionary<int, WindData>();
    
    private class WindData
    {
        public float currentWindStrength = 1f;
        public float targetWindStrength = 1f;
        public float noiseOffset;
        
        public WindData()
        {
            // Random offset so different wind farms have different patterns
            noiseOffset = Random.Range(0f, 1000f);
        }
    }

    public override void Tick(TileObject tileObject, float delta)
    {
        TileObjectDefinition def = tileObject.Definition;
        if (tileObject is not UtilityTileObject util)
        {
            Debug.LogError("TickBehaviour applied to incorrect tile object.");
            return;
        }

        // Get or create wind data for this specific wind farm
        int instanceId = tileObject.GetInstanceID();
        if (!windDataCache.ContainsKey(instanceId))
        {
            windDataCache[instanceId] = new WindData();
        }
        WindData windData = windDataCache[instanceId];

        // Use Perlin noise for smooth, natural wind variation
        float noiseValue = Mathf.PerlinNoise(
            Time.time * variationSpeed + windData.noiseOffset, 
            0f
        );
        
        // Convert noise (0-1) to wind strength (0.7 to 1.3 for ±30%)
        windData.targetWindStrength = 1f + (noiseValue - 0.5f) * 4f * variationAmount;
        
        // Smoothly lerp current wind strength towards target
        windData.currentWindStrength = Mathf.Lerp(
            windData.currentWindStrength, 
            windData.targetWindStrength, 
            delta * 2f // Smooth transition speed
        );

        int baseOutput = def.Utility.Output;
        float emission = def.Utility.Emission * delta;

        float outputMult = 1f;
        float emissionMult = 1f;
        float degradeMult = 1f;

        outputMult -= 0.1f * GridManager.Instance.GetWithinRadius(tileObject, 1, obj => true).Count;

        util.efficiency -= degradeMult * (delta / def.Utility.DegradeTime) * (1 - GameState.Instance.Settings.MinimumEfficiency);
        util.efficiency = Mathf.Max(util.efficiency, GameState.Instance.Settings.MinimumEfficiency);

        float actualOutput = baseOutput * outputMult * util.efficiency * windData.currentWindStrength;
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
        stats.Add(new StatRow("Wind Strength", $"{Mathf.RoundToInt(windData.currentWindStrength * 100)}%", Color.magenta));

        tileObject.Stats = stats;
    }
}