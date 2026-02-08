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
    public GameObject Prefab;
    public Vector2Int Size = Vector2Int.one;

    [Header("Gameplay")]
    public BuildingCategory Category = BuildingCategory.Utility;
    public int Cost = 0;

    public Utility Utility;
    public ResidentialData Residential;
    public InfrastructureData Infrastructure;
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