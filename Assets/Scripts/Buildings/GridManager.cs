using System;
using System.Collections.Generic;
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
            0.5f,
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

    private void OnDrawGizmos()
    {
        if (tiles == null) return;

        Gizmos.color = Color.gray;

        foreach (var tile in tiles.Values)
        {
            Vector3 worldPos = GridToWorld(tile.GridPosition);
            Gizmos.DrawWireCube(worldPos, Vector3.one * tileSize);
        }
    }

    private void OnValidate()
    {
        GenerateGrid();
    }
}
