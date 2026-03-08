using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class GridMouse : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask buildingMask; // Add a layer mask for buildings
    [SerializeField] private Transform groundTransform; // assign the same tiltTarget used by CameraMovement
    
    public Material PreviewMaterial; // a transparent material for the placement preview

    public TileObject placementHighlight;
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


        TileObject clickedBuilding = TryGetClickedBuilding();
        if (!TryGetMouseGridPosition(out Vector2Int gridPos)) return;

        if (GameState.Instance.CurrentMode == GameState.InteractionMode.Place) TryPlace(gridPos);

        TileObject obj = clickedBuilding;
        if (obj == null)
        {
            GridManager.Instance.TryGetTile(gridPos, out Tile tile);
            if (tile != null)
            {
                obj = tile.Occupant;
            }
        }

        switch (GameState.Instance.CurrentMode)
        {
            case GameState.InteractionMode.Select:
                SelectionManager.Instance.Select(obj);
                break;
            case GameState.InteractionMode.Delete:
                if (obj != null)
                    GridManager.Instance.Delete(obj);
                break;
        }
    }

    TileObject TryGetClickedBuilding()
    {
        // Fail if pointer is over UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return null;

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        // Raycast against building layer
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, buildingMask))
        {
            // Try to find TileObject component in the hit object or its parents
            TileObject tileObject = hit.collider.GetComponentInParent<TileObject>();
            if (tileObject != null)
            {
                return tileObject;
            }
        }
        return null;
    }

    void TryPlace(Vector2Int gridPos) // Attempts to place the current chosen building at the given position
    {
        bool placed = GridManager.Instance.TryPlace(GameState.Instance.buildingToBePlaced, gridPos);
        if (!placed) // placement failed (invalid location or insufficient funds)
        {
            MusicManager.Instance.PlayUISound(MusicManager.UISoundType.Fail);
        }
    }

    public void ClearPlacementHighlight()
    {
        if (placementHighlight != null)
        {
            Destroy(placementHighlight.gameObject);
            placementHighlight = null;
        }
    }

    void UpdatePlacementHighlight() // The highlight square during placement
    {
        if (placementHighlight == null) return;
        if (placementHighlight.gameObject == null) return;

        if (!TryGetMouseGridPosition(out Vector2Int gridPos))
        {
            placementHighlight.gameObject.SetActive(false);
            return;
        }

        placementHighlight.gameObject.SetActive(true);
        placementHighlight.Place(gridPos);
        placementHighlight.UpdatePreview();
    }

    public bool TryGetMouseGridPosition(out Vector2Int gridPosition)
    {
        gridPosition = default;

        // Fail if pointer is over UI
        if (EventSystem.current != null && IsPointerOverBlockingUI())
        {
            // Remove or comment out this debug log since it will spam when Post FX canvas is present
            // Debugger.Log("Pointer is over blocking UI, ignoring mouse position.");
            return false;
        }

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        // Raycast ONLY against ground layer (ignores buildings automatically)
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f, groundMask))
        {
            Debugger.Log("Mouse raycast did not hit ground layer.");
            return false;
        }

        // Convert to the grid's local coordinate space (take tilt/parent into account).
        // If groundTransform is the tilted parent of the grid, get the local point in that transform.
        Vector3 samplePos = (groundTransform != null) ? groundTransform.InverseTransformPoint(hit.point) : hit.point;

        // Convert to grid position using the grid-local coordinates.
        gridPosition = GridManager.Instance.WorldToGrid(samplePos);

        return true;
    }

    public void SetPreview(TileObject tileObject) // Expects a TileObject component of an instantiated prefab
    {
        if (placementHighlight != null) ClearPlacementHighlight();

        tileObject.MakePreview();

        placementHighlight = tileObject;
    }
    
    bool IsPointerOverBlockingUI()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Mouse.current.position.ReadValue();

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        // Get the "Ignore Raycast" layer index
        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");

        foreach (var r in results)
        {
            // Skip UI elements on the "Ignore Raycast" layer
            if (r.gameObject.layer == ignoreRaycastLayer)
                continue;

            // Skip if the RawImage has Raycast Target disabled
            var rawImage = r.gameObject.GetComponent<UnityEngine.UI.RawImage>();
            if (rawImage != null && !rawImage.raycastTarget)
                continue;

            // Check for interactive UI elements
            if (r.gameObject.GetComponent<UnityEngine.UI.Button>() != null)
                return true;

            if (r.gameObject.GetComponent<UnityEngine.UI.Toggle>() != null)
                return true;
        }

        return false;
    }
}