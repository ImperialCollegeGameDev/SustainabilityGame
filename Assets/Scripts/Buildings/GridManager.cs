using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }
    // Any script can use GridManager.Instance

    public int width = 10;
    public int height = 10;
    public float tileSize = 1f;

    public Transform terrain;

    private Dictionary<Vector2Int, Tile> tiles = new();

    [SerializeField] private LayerMask groundMask;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        GenerateGrid();
    }

    void GenerateGrid()
    {
        tiles = new Dictionary<Vector2Int, Tile>();

        for (int x = 0; x < width; x++) // Don't show this to Nic. Wu
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                tiles[pos] = new Tile(pos); // By default tiles are invalid (nothing can be placed on them)
            }
        }

        foreach (var tri in hardcodedTriangles)
        {
            SetTileTypeTriangle(tri.Type, tri.A, tri.B, tri.C);
        }
    }

    [SerializeField] private List<TileTriangle> hardcodedTriangles = new List<TileTriangle>
    {
        new TileTriangle(TileType.Normal,
            new Vector2Int(2, 17),
            new Vector2Int(24, 16),
            new Vector2Int(14, 35)),

        new TileTriangle(TileType.Normal,
            new Vector2Int(17, 29),
            new Vector2Int(21, 35),
            new Vector2Int(14, 35)),

        new TileTriangle(TileType.Normal,
            new Vector2Int(25, 30),
            new Vector2Int(20, 27), 
            new Vector2Int(24, 16)),

        new TileTriangle(TileType.Normal,
            new Vector2Int(24, 16),
            new Vector2Int(7, 16),
            new Vector2Int(20, 12)),

        new TileTriangle(TileType.Water,
            new Vector2Int(0, 18),
            new Vector2Int(12, 35),
            new Vector2Int(0, 35)),
    };

    private void SetTileTypeRectangle(TileType type, int x1, int y1, int x2, int y2)
    {
        for (int x = x1; x <= x2; x++)
        {
            for (int y = y1; y <= y2; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (tiles.ContainsKey(pos))
                {
                    tiles[pos].type = type;
                }
            }
        }
    }

    private void SetTileTypeTriangle(TileType type, Vector2Int v1, Vector2Int v2, Vector2Int v3)
    {
        // Compute bounding box of triangle
        int minX = Mathf.Min(v1.x, v2.x, v3.x);
        int maxX = Mathf.Max(v1.x, v2.x, v3.x);
        int minY = Mathf.Min(v1.y, v2.y, v3.y);
        int maxY = Mathf.Max(v1.y, v2.y, v3.y);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int p = new Vector2Int(x, y);

                if (!tiles.ContainsKey(p))
                    continue;

                if (PointInTriangle(p, v1, v2, v3))
                {
                    tiles[p].type = type;
                }
            }
        }
    }

    private bool PointInTriangle(Vector2Int p, Vector2Int a, Vector2Int b, Vector2Int c)
    {
        float d1 = Sign(p, a, b);
        float d2 = Sign(p, b, c);
        float d3 = Sign(p, c, a);

        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

        return !(hasNeg && hasPos);
    }

    private float Sign(Vector2Int p1, Vector2Int p2, Vector2Int p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y)
             - (p2.x - p3.x) * (p1.y - p3.y);
    }

    public bool TryGetTile(Vector2Int pos, out Tile tile)
    {
        return tiles.TryGetValue(pos, out tile);
    }

    public Vector3 GridToWorld(Vector2Int gridPos) // Returns the center of the tile
    {
        return new Vector3(
            (gridPos.x + 0.5f) * tileSize,
            0f,
            (gridPos.y + 0.5f) * tileSize
        );
    }

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / tileSize),
            Mathf.FloorToInt(worldPos.z / tileSize)
        );
    }

    public Vector3 SnapToGrid(Vector3 worldPos)
    {
        Vector2Int gridPos = WorldToGrid(worldPos);
        return GridToWorld(gridPos);
    }

    public bool TryPlaceSelected(Vector2Int gridPos)
    {
        var def = GameState.Instance.buildingToBePlaced;
        if (def == null)
        {
            //Debug.LogWarning($"Selected TileObjectDefinition not found");
            Notifications.Instance.PostNotification($"Select a building first.");
            return false;
        }

        if (!GridManager.Instance.CanPlace(def.Size, gridPos, def.TileType))
        {
            //Debug.Log("Cannot place there (out of bounds or occupied).");
            Notifications.Instance.PostNotification($"Cannot be constructed at this location.");
            return false;
        }

        if (GameState.Instance.money - def.Cost < 0)
        {
            //Debug.Log("Not enough money to place that building.");
            Notifications.Instance.PostNotification($"Not enough money to place that building.");
            return false;
        }

        // Deduct money
        GameState.Instance.ChangeMoney(-def.Cost);

        // Instantiate visual prefab and place it on grid
        GameObject obj = Instantiate(def.Prefab);

        TileObject tileObj = obj.GetComponent<TileObject>(); // TileObject is attached to the model
        if (tileObj == null)
        {
            Debug.LogWarning("Prefab missing TileObject component.");
            Destroy(obj);
            return false;
        }

        tileObj.Init(def); // So TileObject can reference back to its definition data if needed
        tileObj.Place(gridPos); // Handles location of the physical model

        tileObj.transform.SetParent(terrain);

        GridManager.Instance.Occupy(tileObj, gridPos, def.Size); // Handles grid logic - marking tiles as occupied
        Notifications.Instance.PostNotification($"Created building {def.name}.");

        // If it's a utility, register it
        if (def.Category == BuildingCategory.Utility)
        {
            // All buildings have an optional Utility field
            GameState.Instance.OwnedUtilities.Add(def.Utility);
            GameState.Instance.RecomputeTotals();
        }
        else
        {
            // For non-utility buildings we may later create other game-models
        }

        return true;
    }

    public void Delete(TileObject obj)
    {
        obj.Remove(); // Handles visual/model removal

        TileObjectDefinition def = obj.Definition;
        GameState.Instance.ChangeMoney(Mathf.FloorToInt(def.Cost * GameState.Instance.Settings.SellRatio)); // simple 50% refund

        GridManager.Instance.Clear(obj.Origin, def.Size); // Handles grid logic of marking tiles as unoccupied

        Notifications.Instance.PostNotification($"Deleted building {def.name}.");
        // If it's a utility, unregister it
        if (def.Category == BuildingCategory.Utility)
        {
            // All buildings have an optional Utility field
            GameState.Instance.OwnedUtilities.Remove(def.Utility);
            GameState.Instance.RecomputeTotals();
            Debug.Log($"Deleted utility {def.name}.");
        }
        else
        {
            // For non-utility buildings
            //Debug.Log($"Deleted building {def.name}.");
            Notifications.Instance.PostNotification($"Deleted building {def.name}.");
        }
    }

    public Boolean CanPlace(Vector2Int size, Vector2Int origin, TileType tileType)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int checkPos = new Vector2Int(origin.x + x, origin.y + y);
                if (!tiles.ContainsKey(checkPos)) return false;
                if (tiles[checkPos].type != tileType) return false;
                if (tiles[checkPos].IsOccupied) return false;
            }
        }
        return true;
    }

    public void Occupy(TileObject obj, Vector2Int origin, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int pos = new Vector2Int(origin.x + x, origin.y + y);
                tiles[pos].Occupant = obj;
            }
        }
    }

    public void Clear(Vector2Int origin, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int pos = new Vector2Int(origin.x + x, origin.y + y);
                tiles[pos] = new Tile(pos);
            }
        }
    }

    public List<TileObject> GetTileObjects()
    {
        return tiles.Values.Select(t => t.Occupant).Where(o => o != null).Distinct().ToList();
    }


    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Ensure grid exists in edit mode
        if (tiles == null || tiles.Count == 0)
        {
            GenerateGrid();
        }

        // Draw grid
        Gizmos.color = Color.gray;

        foreach (var tile in tiles.Values)
        {
            Vector3 worldPos = GridToWorld(tile.GridPosition);
            Gizmos.DrawWireCube(
                worldPos + new Vector3(0f, 0.5f * tileSize, 0f),
                Vector3.one * tileSize
            );
        }

        DrawMouseGridCoordinate();
        DrawHardcodedTriangles();
    }

    private void DrawHardcodedTriangles()
    {
        if (hardcodedTriangles == null)
            return;

        foreach (var tri in hardcodedTriangles)
        {
            switch(tri.Type)
            {
                case TileType.Normal:
                    Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
                    break;
                case TileType.Water:
                    Gizmos.color = new Color(0f, 0f, 1f, 0.5f);
                    break;
                case TileType.Thermal:
                    Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
                    break;
                default:
                    Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
                    break;
            }

            Vector3 a = GridToWorld(tri.A);
            Vector3 b = GridToWorld(tri.B);
            Vector3 c = GridToWorld(tri.C);

            // Lift slightly so lines are visible above grid
            float yOffset = 0.1f;
            a.y += yOffset;
            b.y += yOffset;
            c.y += yOffset;

            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, a);
        }
    }

    private void DrawMouseGridCoordinate()
    {
        Event e = Event.current;
        if (e == null) return;

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundMask))
        {
            Vector2Int gridPos = WorldToGrid(hit.point);

            Vector3 labelPos = GridToWorld(gridPos);
            labelPos.y += tileSize * 0.6f;

            Handles.color = Color.yellow;
            Handles.Label(labelPos, $"({gridPos.x}, {gridPos.y})");
        }
    }
    #endif

    private void OnValidate()
    {
        GenerateGrid();
        #if UNITY_EDITOR
        SceneView.RepaintAll();
        #endif
    }

    [System.Serializable]
    private struct TileTriangle
    {
        public TileType Type;
        public Vector2Int A;
        public Vector2Int B;
        public Vector2Int C;

        public TileTriangle(TileType type, Vector2Int a, Vector2Int b, Vector2Int c)
        {
            Type = type;
            A = a;
            B = b;
            C = c;
        }
    }
}
