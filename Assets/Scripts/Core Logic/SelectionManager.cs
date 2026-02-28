using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class SelectionManager : MonoBehaviour // Singleton manager for handling logic when a building is selected
{
    public static SelectionManager Instance { get; private set; }

    public GameObject SelectedTileInfoPanelPrefab;
    private GameObject SelectedTileInfoPanel;
    private Canvas canvas;
    private RectTransform canvasRect;
    private Camera mainCamera;

    private Vector2 panelOffset = new Vector2(60f, -20f);
    private bool clampToScreen = true;
    private float screenPadding = 20f;

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

        canvas = SelectedTileInfo.Instance?.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindAnyObjectByType<Canvas>();
            Debug.LogWarning("Canvas found via FindObjectOfType. Position conversion may not work correctly.");
        }
        
        canvasRect = canvas?.GetComponent<RectTransform>();
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

        // Reset anchors and pivot to known state
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.zero;
        panelRect.pivot = new Vector2(0f, 1f); // Top-left pivot for easier positioning

        // Get building world position
        Vector3 worldPosition = building.transform.position + Vector3.up * 2f;
        
        // Convert to screen space
        Vector3 screenPoint = mainCamera.WorldToScreenPoint(worldPosition);

        // Check if behind camera
        if (screenPoint.z < 0)
        {
            panelRect.anchoredPosition = new Vector2(100f, -100f);
            return;
        }

        // For Screen Space - Overlay with CanvasScaler, we need to account for scaling
        // Get the canvas scaler
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        float scaleFactor = 1f;
        
        if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            // Calculate actual scale factor
            Vector2 referenceResolution = scaler.referenceResolution;
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            
            float widthRatio = screenSize.x / referenceResolution.x;
            float heightRatio = screenSize.y / referenceResolution.y;
            
            // Use match value to blend between width and height ratios
            scaleFactor = Mathf.Lerp(widthRatio, heightRatio, scaler.matchWidthOrHeight);
        }

        // Convert screen point to canvas coordinates
        // For overlay canvas with bottom-left anchors, this is straightforward
        Vector2 canvasPosition = new Vector2(
            screenPoint.x / scaleFactor,
            screenPoint.y / scaleFactor
        );

        // Apply offset
        Vector2 finalPosition = canvasPosition + panelOffset;

        // Clamp to canvas bounds if enabled
        if (clampToScreen)
        {
            Vector2 canvasSize = canvasRect.sizeDelta;
            Vector2 panelSize = panelRect.sizeDelta;
            
            // Since pivot is top-left (0, 1), adjust clamping
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
