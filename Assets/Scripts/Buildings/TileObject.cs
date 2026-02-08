using System.Collections.Generic;
using UnityEngine;

public class TileObject : MonoBehaviour
{
    public Vector2Int Size = Vector2Int.one;
    public Vector2Int Origin { get; private set; }

    // runtime info set at placement
    public string DefinitionId;
    public Utility Logic; // can be null for non-utility buildings

    public virtual void Place(Vector2Int origin)
    {
        Origin = origin;

        Vector3 worldPos = GridManager.Instance.GridToWorld(origin);
        transform.position = new Vector3(
                worldPos.x + (Size.x - 1) * 0.5f,
                worldPos.y,
                worldPos.z + (Size.y - 1) * 0.5f
            );
        // Occupancy status of tiles is also stored in GridManager
    }

    public void BindLogic(Utility u)
    {
        Logic = u;
    }

    public List<Vector2Int> OccupiedTiles(TileObject obj)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        for (int x = 0; x < obj.Size.x; x++)
            for (int y = 0; y < obj.Size.y; y++)
                positions.Add(obj.Origin + new Vector2Int(x, y));
        return positions;
    }
}