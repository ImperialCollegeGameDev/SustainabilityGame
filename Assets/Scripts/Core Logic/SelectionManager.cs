using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class SelectionManager : MonoBehaviour // Singleton manager for handling logic when a building is selected
{
    public static SelectionManager Instance { get; private set; }

    public GameObject SelectedTileInfoPanelPrefab;
    private GameObject SelectedTileInfoPanel;
    private Canvas canvas => SelectedTileInfo.Instance?.GetComponentInParent<Canvas>();
    private RectTransform canvasRect => canvas?.GetComponent<RectTransform>();
    private Camera mainCamera;

    [SerializeField] private Vector2 panelOffset = new Vector2(-60f, 60f);
    private bool clampToScreen = true;
    private float screenPadding = 20f;
    private Vector3 fixedPanelScale = Vector3.one * 0.7f; // Changed to Vector3 for proper 3D scale

    public TileObject Selected { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        if (SelectedTileInfoPanelPrefab == null)
        {
            Debug.LogWarning("SelectedTileInfoPanel is not assigned in the inspector.");
        }
        
        mainCamera = Camera.main;
    }

    public void Select(TileObject obj)
    {
        if (obj == null || obj == Selected)
        {
            Deselect();
            return;
        }
        if (Selected != null) Selected.Deselect();
        Selected = obj;
        obj.Select();
        if (SelectedTileInfoPanel != null) Destroy(SelectedTileInfoPanel);
        
        // Try instantiating directly under canvas
        if (canvas == null)
        {
            Debug.LogError("No Canvas found for instantiating SelectedTileInfoPanel.");
            return;
        }
        SelectedTileInfoPanel = Instantiate(SelectedTileInfoPanelPrefab, canvas.transform);
        
        if (SelectedTileInfoPanel.TryGetComponent(out TileInfoPanel panel))
        {
            panel.SetTile(obj);
        }
        else
        {
            Debug.LogError("SelectedTileInfoPanelPrefab does not have a TileInfoPanel component.");
        }

        // Force layout rebuild before positioning
        Canvas.ForceUpdateCanvases();
        
        PositionPanelNearBuilding(obj);
    }

    private void PositionPanelNearBuilding(TileObject building)
    {
        if (SelectedTileInfoPanel == null || canvas == null || mainCamera == null || canvasRect == null)
            return;

        RectTransform panelRect = SelectedTileInfoPanel.GetComponent<RectTransform>();
        if (panelRect == null)
            return;

        // Set anchors and pivot to bottom-left for consistent positioning
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.zero;
        panelRect.pivot = new Vector2(0f, 1f); // Top-left pivot

        // IMPORTANT: Set the local scale explicitly to ensure it's applied correctly
        // This must be done as localScale (not scale) and should include Z axis
        panelRect.localScale = fixedPanelScale;

        // Get building world position (slightly above the building)
        Vector3 worldPosition = building.transform.position + Vector3.up * 2f;
        
        // Convert world position to screen point
        Vector3 screenPoint = mainCamera.WorldToScreenPoint(worldPosition);

        // Check if behind camera
        if (screenPoint.z < 0)
        {
            panelRect.anchoredPosition = new Vector2(100f, -100f);
            return;
        }

        // Convert screen point to canvas local position
        // This automatically handles Canvas Scaler settings
        Vector2 canvasPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCamera,
            out canvasPosition
        );

        // Apply offset
        Vector2 finalPosition = canvasPosition + panelOffset;

        // Clamp to canvas bounds
        if (clampToScreen)
        {
            Vector2 canvasSize = canvasRect.sizeDelta;
            Vector2 panelSize = panelRect.sizeDelta;
            
            // Clamp based on top-left pivot
            finalPosition.x = Mathf.Clamp(finalPosition.x, screenPadding, canvasSize.x - panelSize.x - screenPadding);
            finalPosition.y = Mathf.Clamp(finalPosition.y, panelSize.y + screenPadding, canvasSize.y - screenPadding);
        }

        panelRect.anchoredPosition = finalPosition;
    }

    public void Deselect()
    {
        if (Selected != null)
        {
            Selected.Deselect();
            Selected = null;
            if (SelectedTileInfoPanel != null) Destroy(SelectedTileInfoPanel);
        }
    }
}
