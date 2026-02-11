using UnityEngine;
using UnityEngine.InputSystem;

public class PlacementManager : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask groundMask;
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
            TryPlaceFromMouse(true);    // bool place = true
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            TryPlaceFromMouse(false);   // false = remove
        }

    }

    void TryPlaceFromMouse(bool place)
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundMask))
        {
            Vector3 worldPos = hit.point;
            Vector2Int gridPos = GridManager.Instance.WorldToGrid(worldPos);

            // Ask the GameState to place (it will handle cost, game-model creation and occupancy)
            bool placed;
            if (place == true)
            {
                placed = GameState.Instance.TryPlaceSelected(gridPos);
                if (!placed) // placement failed (invalid location or insufficient funds)
                {
                    // feedback can be added here (sound, UI message)
                }
            }

            else
                placed = GameState.Instance.TryPlaceSelected(gridPos);
        }
    }

    void TryRemoveFromMouse()
    {

    }
}