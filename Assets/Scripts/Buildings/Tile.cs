using UnityEngine;

public class Tile
{
    public Vector2Int GridPosition { get; private set; }
    public TileObject Occupant { get; set; } // null if empty

    public Tile(Vector2Int gridPosition)
    {
        GridPosition = gridPosition;
        Occupant = null;
    }

    public bool IsOccupied => Occupant != null;
}
