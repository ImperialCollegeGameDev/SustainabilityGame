using System;
using System.Collections.Generic;
using UnityEngine;

public class TileObject : MonoBehaviour
{
    public Vector2Int Origin { get; private set; }

    public TileObjectDefinition Definition { get; private set; } // Important details about this particular building like its power output, max occupancy etc.

    private MeshRenderer[] renderers; // All renderers in this object and its children, for selection highlighting
    [SerializeField] private MaterialPropertyBlock previewMPB; // For the placement preview material, to set the color based on whether placement is valid

    private readonly HashSet<Upgrade> unlockedUpgrades = new HashSet<Upgrade>();
    private readonly List<int> rewardThresholds = new List<int>() { 30, 60, 120, 180, 360 };
    private int currentPointReward = 1; // The number of policy points awarded for the next threshold, increases by 1 each time
    public int policyPoints = 0; // Points that can be spent on upgrades for this building
    public float timeSpent = 0; // Multiple copies of the building can stack this up to unlock upgrades

    private Boolean HasUpgrades => Definition.UpgradeTree != null && Definition.UpgradeTree.Paths.Length > 0;

    protected virtual void Awake()
    {
        renderers = GetComponentsInChildren<MeshRenderer>(true);
    }

    public void Init(TileObjectDefinition def)
    {
        Definition = def;
    }

    public void Place(Vector2Int origin)
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
            );
        // Occupancy status of tiles is also stored in GridManager
    }

    public virtual void Remove()
    {
        Destroy(gameObject);
        // Occupancy is handled by GridManager
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
        timeSpent += delta;
        if (!HasUpgrades) return;
        if (rewardThresholds.Count > 0 && timeSpent >= rewardThresholds[0])
        {
            policyPoints += currentPointReward;
            currentPointReward++;
            rewardThresholds.RemoveAt(0);
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
}