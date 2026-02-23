using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UpgradePathUI : MonoBehaviour
{
    public UpgradeNode UpgradeNodePrefab;

    private List<UpgradeNode> upgrades;

    public void Init(UpgradePath upgradePath, TileObjectDefinition def)
    {
        upgrades = new List<UpgradeNode>();
        foreach (Upgrade upgrade in upgradePath.Upgrades)
        {
            UpgradeNode upgradeUI = Instantiate(UpgradeNodePrefab, transform, false);
            upgrades.Add(upgradeUI);
            upgradeUI.Init(upgrade, def);
        }
    }

    public void UpdateInfo()
    {
        foreach (var node in upgrades)
        {
            node.UpdateInfo();
        }
    }
}
