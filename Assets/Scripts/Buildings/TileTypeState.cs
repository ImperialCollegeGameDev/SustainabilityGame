using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TileTypeState // Can store runtime info about a specific type of building, e.g. total energy produced by all coal power plants
{
    public TileObjectDefinition Definition;
    private HashSet<Upgrade> unlockedUpgrades = new HashSet<Upgrade>();
    public int policyPoints = 0; // Points that can be spent on upgrades for this building
    public float timeSpent = 0; // Multiple copies of the building can stack this up to unlock upgrades
    private List<int> rewardThresholds = new List<int>() { 30, 60, 120, 180, 360 };
    private int currentPointReward = 1; // The number of policy points awarded for the next threshold, increases by 1 each time

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
                    } else
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

    public void AddTime(float delta)
    {
        timeSpent += delta;
        if (rewardThresholds.Count > 0 && timeSpent >= rewardThresholds[0])
        {
            policyPoints += currentPointReward;
            Notifications.Instance.PostNotification($"Gained {currentPointReward} policy points for {Definition.DisplayName}! Total: {policyPoints}");
            currentPointReward++;
            rewardThresholds.RemoveAt(0);
        }
    }
}