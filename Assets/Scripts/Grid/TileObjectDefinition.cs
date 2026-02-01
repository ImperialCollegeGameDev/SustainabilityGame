using UnityEngine;

[CreateAssetMenu(menuName = "CityBuilder/TileObjectDefinition")]
public class TileObjectDefinition : ScriptableObject
{
    public string Id;
    public GameObject Prefab;
    public Vector2Int Size = Vector2Int.one;

    #if UNITY_EDITOR
    private void OnValidate()
    {
        if (Prefab == null)
            return;

        if (Prefab.GetComponent<TileObject>() == null)
        {
            Debug.LogError(
                $"{name} {GetType().Name}: Assigned Prefab '{Prefab.name}' does not contain a TileObject component.",
                this
            );
            Prefab = null;
        }
    }
    #endif

}
