using UnityEngine;
using UnityEngine.InputSystem;

public class PlacementManager : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask groundMask;

    public static PlacementManager Instance { get; private set; }
    public TileObjectDefinition building;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (Mouse.current == null) return;
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryPlaceFromMouse();
        }
    }

    void TryPlaceFromMouse()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundMask))
        {
            Vector3 worldPos = hit.point;
            Vector2Int gridPos = GridManager.Instance.WorldToGrid(worldPos);

            TryPlace(building, gridPos);
        }
    }

    public bool TryPlace(TileObjectDefinition tileObjectDef, Vector2Int gridPos)
    {
        if (!GridManager.Instance.CanPlace(tileObjectDef.Size, gridPos))
            return false;

        GameObject obj = Instantiate(tileObjectDef.Prefab);
        TileObject tileObj = obj.GetComponent<TileObject>();
        tileObj.Place(gridPos);

        GridManager.Instance.Occupy(tileObj, gridPos, tileObjectDef.Size);
        return true;
    }
}
