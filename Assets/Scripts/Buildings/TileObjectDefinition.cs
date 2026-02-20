using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public enum BuildingCategory
{
    Utility,
    Residential,
    Infrastructure
}

[CreateAssetMenu(menuName = "CityBuilder/TileObjectDefinition")]
public class TileObjectDefinition : ScriptableObject
{
    [Header("General")]
    public string Id;
    public string DisplayName;
    public GameObject Prefab;
    public Vector2Int Size = Vector2Int.one;
    public TileType TileType = TileType.Normal;

    [Header("Gameplay")]
    public BuildingCategory Category = BuildingCategory.Utility;
    public int Cost = 0;

    public Utility Utility;
    public ResidentialData Residential;
    public InfrastructureData Infrastructure;

    public List<StatRow> GetStats()
    {
        List<StatRow> stats = new List<StatRow>();
        stats.Add(new StatRow("Type", Category.ToString(), Color.lightGray));
        stats.Add(new StatRow("Cost", Mathf.FloorToInt(Cost * GameState.Instance.Settings.SellRatio).ToString(), Color.yellow));
        switch (Category)
        {
            case BuildingCategory.Utility:
                stats.Add(new StatRow("Power", Utility.Output.ToString(), Color.aquamarine));
                stats.Add(new StatRow("Emissions", Utility.Emission.ToString(), Color.red));
                break;
            case BuildingCategory.Residential:
                stats.Add(new StatRow("Max Occupancy", Residential.MaxOccupancy.ToString(), new Color(0.8f, 0.8f, 0.2f, 1.0f)));
                break;
            case BuildingCategory.Infrastructure:
                stats.Add(new StatRow("Resource Production", Infrastructure.ResourceProduction.ToString(), Color.magenta));
                break;
        }
        return stats;
    }
}

[System.Serializable]
public class ResidentialData
{
    public int MaxOccupancy;
}

[System.Serializable]
public class InfrastructureData
{
    public int ResourceProduction;
}

public class StatRow
{
    public string Name;
    public object Value;
    public Color Color;

    public StatRow(string name, object value, Color color)
    {
        Name = name;
        Value = value;
        Color = color;
    }
}
