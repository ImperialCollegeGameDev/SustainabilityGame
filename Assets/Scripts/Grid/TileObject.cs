using System.Collections.Generic;
using UnityEngine;

public class TileObject : MonoBehaviour
{
    public Vector2Int Size = Vector2Int.one;
    public Vector2Int Origin { get; private set; }

    public virtual void Place(Vector2Int origin)
    {
        Origin = origin;

        Vector3 worldPos = GridManager.Instance.GridToWorld(origin);
        worldPos.x += 0.5f;
        transform.position = worldPos;
        // Occupancy status of tiles is also stored in GridManager
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
