using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class TileObject : MonoBehaviour
{
    public Vector2Int Origin { get; private set; }

    // runtime info set at placement
    public string DefinitionId;

    public void Place(Vector2Int origin)
    {
        Origin = origin;

        Vector3 worldPos = GridManager.Instance.GridToWorld(origin);
        transform.position = new Vector3(
                worldPos.x + (GetDefinition().Size.x - 1) * 0.5f,
                worldPos.y,
                worldPos.z + (GetDefinition().Size.y - 1) * 0.5f
            );
        // Occupancy status of tiles is also stored in GridManager
    }

    public void Remove()
    {
        Destroy(gameObject);
        // Occupancy is handled by GridManager
    }

    public List<Vector2Int> OccupiedTiles()
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        for (int x = 0; x < GetDefinition().Size.x; x++)
            for (int y = 0; y < GetDefinition().Size.y; y++)
                positions.Add(Origin + new Vector2Int(x, y));
        return positions;
    }

    public TileObjectDefinition GetDefinition()
    {
        return TileObjectCatalog.Instance.Get(DefinitionId);
    }

    public void Select()
    {
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr == null)
        {
            Debug.LogWarning("No MeshRenderer found on " + name);
            return;
        }
        mr.renderingLayerMask = 2u; // Selected object layer (Project Settings -> Tags and Layers -> Rendering Layers)
    }

    public void Deselect()
    {
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr == null)
        {
            Debug.LogWarning("No MeshRenderer found on " + name);
            return;
        }
        mr.renderingLayerMask = 1u;
    }
}