using System.Collections.Generic;
using UnityEngine;

public class TileObjectCatalog : MonoBehaviour
{
    public static TileObjectCatalog Instance { get; private set; }
    // Any script can use TileObjectCatalog.Instance

    public List<TileObjectDefinition> definitions;

    private Dictionary<string, TileObjectDefinition> lookup;

    private void Awake()
    {
        Instance = this;
        lookup = new Dictionary<string, TileObjectDefinition>();

        foreach (var def in definitions)
        {
            lookup[def.Id] = def;
        }
    }

    public TileObjectDefinition Get(string id)
    {
        lookup.TryGetValue(id, out var def);
        return def;
    }
}
