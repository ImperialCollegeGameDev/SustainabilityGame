using UnityEngine;

public class SelectionManager : MonoBehaviour // Singleton manager for handling logic when a building is selected
{
    public static SelectionManager Instance { get; private set; }

    public GameObject SelectedTileInfoCanvas;
    public GameObject SelectedTileInfoPanelPrefab;
    private GameObject SelectedTileInfoPanel;

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
        SelectedTileInfoPanel = Instantiate(SelectedTileInfoPanelPrefab, SelectedTileInfoCanvas.transform);
        SelectedTileInfoPanel.TryGetComponent(out TileInfoPanel panel);
        if (panel != null)
        {
            panel.SetTile(obj.Definition);
        }
        else
        {
            Debug.LogError("SelectedTileInfoPanelPrefab does not have a TileInfoPanel component.");
        }
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
