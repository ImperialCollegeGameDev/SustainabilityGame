using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UpgradeScreen : MonoBehaviour
{
    public static UpgradeScreen Instance { get; private set; }

    private List<UpgradePathUI> upgradePaths;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        GetComponent<Canvas>().enabled = false;
    }

    public GameObject PathContainer;
    public UpgradePathUI UpgradePathPrefab;
    public TextMeshProUGUI NameText;
    public TextMeshProUGUI PointsText;

    private TileObject currentTile;

    public void Open(TileObject tileObj)
    {
        TileObjectDefinition def = tileObj.Definition;
        upgradePaths = new List<UpgradePathUI>();
        GetComponent<Canvas>().enabled = true;
        // Clear existing paths
        foreach (Transform child in PathContainer.transform)
        {
            Destroy(child.gameObject);
        }
        // Add new paths
        foreach (UpgradePath path in def.UpgradeTree.Paths)
        {
            UpgradePathUI upgradePath = Instantiate(UpgradePathPrefab, PathContainer.transform, false);
            upgradePaths.Add(upgradePath);
            upgradePath.Init(path, tileObj);
        }
        NameText.text = def.DisplayName;
        currentTile = tileObj;
        UpdateInfo();
        GameState.Instance.SetTicking(false);
    }

    public void Close()
    {
        GetComponent<Canvas>().enabled = false;
        GameState.Instance.SetTicking(true);
    }

    public void UpdateInfo()
    {
        PointsText.text = $"{currentTile.policyPoints}";
        foreach (var path in upgradePaths)
        {
            path.UpdateInfo();
        }
    }
}
