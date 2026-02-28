using System;
using System.Collections.Generic;
using UnityEngine;

public enum BuildingStatus
{
    None,
    NeedsRepair,
    NeedsPower
}

public class TileObject : MonoBehaviour
{
    public Vector2Int Origin { get; private set; }
    public Vector3 Center => GridManager.Instance.GridToWorldTrue(Origin + new Vector2(0.5f * Definition.Size.x, 0.5f * Definition.Size.y));

    public TileObjectDefinition Definition { get; private set; } // Important details about this particular building like its power output, max occupancy etc.

    private MeshRenderer[] renderers; // All renderers in this object and its children, for selection highlighting
    [SerializeField] private MaterialPropertyBlock previewMPB; // For the placement preview material, to set the color based on whether placement is valid

    private readonly HashSet<Upgrade> unlockedUpgrades = new HashSet<Upgrade>();

    [SerializeField] private Vector3 Offset = Vector3.zero;

    // Status icon system
    [Header("Status")]
    [SerializeField] private float iconHeightOffset = 7f;
    private float iconScale = 9f;
    
    private BuildingStatus currentStatus = BuildingStatus.None;
    private GameObject statusIconObject;
    private SpriteRenderer statusIconRenderer;
    private Camera mainCamera;

    public BuildingStatus Status => currentStatus;

    protected virtual void Awake()
    {
        renderers = GetComponentsInChildren<MeshRenderer>(true);
        mainCamera = Camera.main;
    }

    protected virtual void Update()
    {
        // Billboard behavior - make icon face camera
        if (statusIconObject != null && statusIconObject.activeSelf && mainCamera != null)
        {
            statusIconObject.transform.rotation = Quaternion.LookRotation(statusIconObject.transform.position - mainCamera.transform.position);
        }
    }

    public void Init(TileObjectDefinition def)
    {
        Definition = def;
    }

    public virtual void Place(Vector2Int origin)
    {
        Origin = origin;
        if (Definition == null)
        {
            Debug.Log("TileObject missing definition data.");
            return;
        } else if (Definition.Size == null)
        {
            Debug.Log("TileObject defintion missing size data.");
        }

            Vector3 worldPos = GridManager.Instance.GridToWorld(origin);
        transform.position = new Vector3(
                worldPos.x + (Definition.Size.x - 1) * 0.5f * GridManager.Instance.tileSize,
                worldPos.y,
                worldPos.z + (Definition.Size.y - 1) * 0.5f * GridManager.Instance.tileSize
            ) + Offset;
        // Occupancy status of tiles is also stored in GridManager
    }

    public virtual void Remove()
    {
        if (statusIconObject != null)
        {
            Destroy(statusIconObject);
        }
        Destroy(gameObject);
        // Occupancy is handled by GridManager
    }

    public void SetStatus(BuildingStatus newStatus)
    {
        if (currentStatus == newStatus)
            return;

        currentStatus = newStatus;

        if (newStatus == BuildingStatus.None)
        {
            // Hide icon
            if (statusIconObject != null)
            {
                statusIconObject.SetActive(false);
            }
        }
        else
        {
            // Show icon with appropriate sprite
            if (statusIconObject == null)
            {
                CreateStatusIcon();
            }
            else
            {
                statusIconObject.SetActive(true);
            }

            UpdateStatusIcon();
        }
    }

    private void CreateStatusIcon()
    {
        // Create icon GameObject
        statusIconObject = new GameObject("StatusIcon");
        statusIconObject.transform.SetParent(transform);
        
        // Position above the building
        Vector3 iconPosition = transform.position + Vector3.up * iconHeightOffset;
        statusIconObject.transform.position = iconPosition;
        
        // Add SpriteRenderer for 2D icon in 3D space
        statusIconRenderer = statusIconObject.AddComponent<SpriteRenderer>();
        statusIconRenderer.transform.localScale = Vector3.one * iconScale;
        
        // Set sorting layer to render above everything
        statusIconRenderer.sortingOrder = 1;
    }

    private void UpdateStatusIcon()
    {
        if (statusIconRenderer == null)
            return;

        switch (currentStatus)
        {
            case BuildingStatus.NeedsRepair:
                statusIconRenderer.sprite = GridManager.Instance.needsRepairIcon;
                statusIconRenderer.color = new Color(1f, 0.5f, 0f, 1f); // Orange tint
                break;
            case BuildingStatus.NeedsPower:
                statusIconRenderer.sprite = GridManager.Instance.needsPowerIcon;
                statusIconRenderer.color = new Color(1f, 1f, 0f, 1f); // Yellow tint
                break;
            case BuildingStatus.None:
                statusIconRenderer.sprite = null;
                break;
        }
    }

    public List<Vector2Int> OccupiedTiles()
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        for (int x = 0; x < Definition.Size.x; x++)
            for (int y = 0; y < Definition.Size.y; y++)
                positions.Add(Origin + new Vector2Int(x, y));
        return positions;
    }

    public void Select()
    {
        foreach (MeshRenderer mr in renderers)
            mr.renderingLayerMask = 2u; // Selected object layer (Project Settings -> Tags and Layers -> Rendering Layers)
    }

    public void Deselect()
    {
        foreach (MeshRenderer mr in renderers)
            mr.renderingLayerMask = 1u;
    }

    public void MakePreview()
    {
        previewMPB = new MaterialPropertyBlock();
        previewMPB.SetColor("_BaseColor", new UnityEngine.Color(0f, 0.8f, 0.8f, 0.5f));

        foreach (MeshRenderer mr in renderers) {
            mr.material = GridMouse.Instance.PreviewMaterial;
            mr.SetPropertyBlock(previewMPB);
        }
    }

    public void UpdatePreview()
    {
        if (previewMPB == null) return;
        bool valid = GridManager.Instance.CanPlace(Definition.Size, Origin, Definition.TileType);
        previewMPB.SetColor("_BaseColor", valid ? new UnityEngine.Color(0f, 0.8f, 0.8f, 0.5f) : new UnityEngine.Color(0.8f, 0f, 0f, 0.5f));
        foreach (MeshRenderer mr in renderers)
            mr.SetPropertyBlock(previewMPB);
    }

    public void Tick(float delta) {
        Definition.TickLogic.Tick(this, delta);
    }

    public void AddTime(float delta)
    {
        TileTypeState state = TileStateCatalog.Instance.Get(Definition.Id);
        if (state != null)
        {
            state.AddTime(delta);
        }
    }

    public void UnlockUpgrade(Upgrade upgrade)
    {
        if (Definition.UpgradeTree.Contains(upgrade))
        {
            unlockedUpgrades.Add(upgrade);
            Notifications.Instance.PostNotification($"Unlocked upgrade: {upgrade.DisplayName} for {Definition.DisplayName}");
        }
        else
        {
            Debug.LogWarning($"Upgrade {upgrade.DisplayName} is not part of the upgrade tree for {Definition.DisplayName}");
        }
    }

    public void UnlockUpgrade(String upgradeID)
    {
        Upgrade upgrade = GetUpgradeByID(upgradeID);
        UnlockUpgrade(upgrade);
    }

    public bool HasUpgrade(String upgradeID)
    {
        Upgrade upgrade = GetUpgradeByID(upgradeID);
        return unlockedUpgrades.Contains(upgrade);
    }

    public bool HasUpgrade(Upgrade upgrade)
    {
        return unlockedUpgrades.Contains(upgrade);
    }

    public bool CanUnlock(Upgrade upgrade)
    {
        if (!Definition.UpgradeTree.Contains(upgrade))
            return false;
        foreach (var path in Definition.UpgradeTree.Paths)
        {
            for (int i = 0; i < path.Upgrades.Length; i++)
            {
                if (path.Upgrades[i] == upgrade)
                {
                    if (i == 0 || HasUpgrade(path.Upgrades[i - 1]))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
        return false;
    }

    public bool CanUnlock(String upgradeID)
    {
        Upgrade upgrade = GetUpgradeByID(upgradeID);
        return CanUnlock(upgrade);
    }

    private Upgrade GetUpgradeByID(String upgradeID)
    {
        foreach (var path in Definition.UpgradeTree.Paths)
        {
            foreach (var upgrade in path.Upgrades)
                if (upgrade.Id == upgradeID)
                    return upgrade;
        }
        return null;
    }

    public virtual void GridUpdate()
    {
        return;
    }
}