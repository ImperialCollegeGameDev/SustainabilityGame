using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class TileObject : MonoBehaviour
{
    public Vector2Int Origin { get; private set; }

    public TileObjectDefinition Definition { get; private set; } // Important details about this particular building like its power output, max occupancy etc.

    private MeshRenderer[] renderers; // All renderers in this object and its children, for selection highlighting

    protected virtual void Awake()
    {
        renderers = GetComponentsInChildren<MeshRenderer>(true);
    }

    public void Init(TileObjectDefinition def)
    {
        Definition = def;
    }

    public void Place(Vector2Int origin)
    {
        Origin = origin;

        Vector3 worldPos = GridManager.Instance.GridToWorld(origin);
        transform.position = new Vector3(
                worldPos.x + (Definition.Size.x - 1) * 0.5f * GridManager.Instance.tileSize,
                worldPos.y,
                worldPos.z + (Definition.Size.y - 1) * 0.5f * GridManager.Instance.tileSize
            );
        // Occupancy status of tiles is also stored in GridManager
    }

    public virtual void Remove()
    {
        Destroy(gameObject);
        // Occupancy is handled by GridManager
    }

    public List<Vector2Int> OccupiedTiles()
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        for (int x = 0; x < Definition.Size.x; x++)
            for (int y = 0; y < Definition.Size.y; y++)
                positions.Add(Origin + new Vector2Int(x, y));
        return positions;
    }

    public void Select()
    {
        foreach (MeshRenderer mr in renderers)
            mr.renderingLayerMask = 2u; // Selected object layer (Project Settings -> Tags and Layers -> Rendering Layers)
    }

    public void Deselect()
    {
        foreach (MeshRenderer mr in renderers)
            mr.renderingLayerMask = 1u;
    }

    public virtual void Tick(float delta) { }
}