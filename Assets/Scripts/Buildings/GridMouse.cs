using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class GridMouse : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private GameObject placementHighlight;
    public static GridMouse Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        if (GameState.Instance.CurrentMode == GameState.InteractionMode.Place)
        {
            UpdatePlacementHighlight();
        } else
        {
            placementHighlight.SetActive(false);
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryInteract();
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            TryInteract();
        }

    }

    void TryInteract() // Decides what to do based on the current mode (select / place / delete)
    {
        if (GameState.Instance.CurrentMode == GameState.InteractionMode.None) return;

        if (!TryGetMouseGridPosition(out Vector2Int gridPos)) return;

        if (GameState.Instance.CurrentMode == GameState.InteractionMode.Place) TryPlace(gridPos);

        GridManager.Instance.TryGetTile(gridPos, out Tile tile);
        if (tile == null) return;
        TileObject obj = tile.Occupant;

        switch (GameState.Instance.CurrentMode)
        {
            case GameState.InteractionMode.Select:
                SelectionManager.Instance.Select(obj);
                break;
            case GameState.InteractionMode.Delete:
                if (obj != null)
                    GameState.Instance.Delete(obj);
                break;
        }
    }

    void TryPlace(Vector2Int gridPos) // Attempts to place the current chosen building at the given position
    {
        bool placed = GameState.Instance.TryPlaceSelected(gridPos);
        if (!placed) // placement failed (invalid location or insufficient funds)
        {
            // feedback can be added here (sound, UI message)
        }
    }

    void UpdatePlacementHighlight() // The highlight square during placement
    {
        if(!TryGetMouseGridPosition(out Vector2Int gridPos))
        {
            placementHighlight.SetActive(false);
            return;
        }

        placementHighlight.SetActive(true);
        placementHighlight.transform.position = GridManager.Instance.GridToWorld(gridPos) + new Vector3(0, 0.01f, 0);
    }
    public bool TryGetMouseGridPosition(out Vector2Int gridPosition)
    {
        gridPosition = default;

        // Fail if pointer is over UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return false;

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        // Raycast ONLY against ground layer (ignores buildings automatically)
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f, groundMask))
            return false;

        // Convert to grid position
        Vector3 worldPos = hit.point;
        gridPosition = GridManager.Instance.WorldToGrid(worldPos);

        return true;
    }

}