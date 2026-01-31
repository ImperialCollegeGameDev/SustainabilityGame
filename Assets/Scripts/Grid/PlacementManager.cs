using UnityEngine;
using UnityEngine.InputSystem;

public class PlacementManager : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    public static PlacementManager Instance { get; private set; }

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

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 worldPos = hit.point;
            Vector2Int gridPos = GridManager.Instance.WorldToGrid(worldPos);

            TryPlace("example2", gridPos);
        }
    }

    public bool TryPlace(string tileObjectId, Vector2Int gridPos)
    {
        var def = TileObjectCatalog.Instance.Get(tileObjectId);
        if (def == null)
            return false;

        if (!GridManager.Instance.CanPlace(def.Size, gridPos))
            return false;

        GameObject obj = Instantiate(def.Prefab);
        TileObject tileObj = obj.GetComponent<TileObject>();
        tileObj.Place(gridPos);

        GridManager.Instance.Occupy(tileObj, gridPos, def.Size);
        return true;
    }
}
