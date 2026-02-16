using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteAlways]
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }
    // Any script can use GridManager.Instance

    public int width = 10;
    public int height = 10;
    public float tileSize = 1f;

    private Dictionary<Vector2Int, Tile> tiles = new();

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
                tiles[pos] = new Tile(pos);
            }
        }
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
            Debug.LogWarning($"Selected TileObjectDefinition not found");
            return false;
        }

        if (!GridManager.Instance.CanPlace(def.Size, gridPos))
        {
            Debug.Log("Cannot place there (out of bounds or occupied).");
            return false;
        }

        if (GameState.Instance.money - def.Cost < 0)
        {
            Debug.Log("Not enough money to place that building.");
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

        GridManager.Instance.Occupy(tileObj, gridPos, def.Size); // Handles grid logic - marking tiles as occupied
        GameState.Instance.PostNotification($"Created building {def.name}.");

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
        GameState.Instance.ChangeMoney(def.Cost / 2); // simple 50% refund

        GridManager.Instance.Clear(obj.Origin, def.Size); // Handles grid logic of marking tiles as unoccupied

        GameState.Instance.PostNotification($"Deleted building {def.name}.");
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
            Debug.Log($"Deleted building {def.name}.");
        }
    }

    public Boolean CanPlace(Vector2Int size, Vector2Int origin)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int checkPos = new Vector2Int(origin.x + x, origin.y + y);
                if (!tiles.ContainsKey(checkPos)) return false;
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

    private void OnDrawGizmos()
    {
        if (tiles == null) return;

        Gizmos.color = Color.gray;

        foreach (var tile in tiles.Values)
        {
            Vector3 worldPos = GridToWorld(tile.GridPosition);
            Gizmos.DrawWireCube(worldPos + new Vector3(0f, 0.5f * tileSize, 0f), Vector3.one * tileSize);
        }
    }

    private void OnValidate()
    {
        GenerateGrid();
    }
}
