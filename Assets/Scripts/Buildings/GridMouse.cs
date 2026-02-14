using UnityEngine;
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

    void TryInteract()
    {
        if (GameState.Instance.CurrentMode == GameState.InteractionMode.None) return;

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, groundMask))
            return;

        Vector3 worldPos = hit.point;
        Vector2Int gridPos = GridManager.Instance.WorldToGrid(worldPos);

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

    void TryPlace(Vector2Int gridPos)
    {
        bool placed = GameState.Instance.TryPlaceSelected(gridPos);
        if (!placed) // placement failed (invalid location or insufficient funds)
        {
            // feedback can be added here (sound, UI message)
        }
    }

    void UpdatePlacementHighlight()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f, groundMask))
            return;

        placementHighlight.SetActive(true);
        placementHighlight.transform.position = GridManager.Instance.SnapToGrid(hit.point) + new Vector3(0, 0.1f, 0);
    }
}