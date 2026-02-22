using System.Collections.Generic;
using UnityEngine;

public class TileStateCatalog : MonoBehaviour
{
    public static TileStateCatalog Instance { get; private set; }

    [SerializeField] public List<TileTypeState> buildings; // This singleton stores a set of unique buildings that are available in game

    private Dictionary<string, TileTypeState> lookup;

    private void Awake()
    {
        Instance = this;
        lookup = new Dictionary<string, TileTypeState>();

        foreach (var building in buildings)
        {
            lookup[building.Definition.Id] = building;
        }
    }

    public TileTypeState Get(string id) // Use this to get data about a building by its definition id, e.g. get the unlocked upgrades for Coal plants
    {
        lookup.TryGetValue(id, out var building);
        return building;
    }
}
