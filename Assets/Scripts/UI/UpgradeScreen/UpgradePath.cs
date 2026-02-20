using TMPro;
using UnityEngine;

public class UpgradePathUI : MonoBehaviour
{
    public UpgradeNode UpgradeNodePrefab;

    public void Init(UpgradePath upgradePath)
    {
        foreach (Upgrade upgrade in upgradePath.Upgrades)
        {
            UpgradeNode upgradeUI = Instantiate(UpgradeNodePrefab, transform, false);
            upgradeUI.Init(upgrade);
        }
    }
}
