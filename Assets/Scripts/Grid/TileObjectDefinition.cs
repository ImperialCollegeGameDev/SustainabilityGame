using UnityEngine;

[CreateAssetMenu(menuName = "CityBuilder/TileObjectDefinition")]
public class TileObjectDefinition : ScriptableObject
{
    public string Id;
    public GameObject Prefab;
    public Vector2Int Size = Vector2Int.one;
}
