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

            // Ask the GameState to place (it will handle cost, game-model creation and occupancy)
            bool placed = GameState.Instance.TryPlaceSelected(gridPos);
            if (!placed) // placement failed (invalid location or insufficient funds)
            {
                // feedback can be added here (sound, UI message)
            }
        }
    }
}