using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TileTypeState // Can store runtime info about a specific type of building, e.g. total energy produced by all coal power plants
{
    public TileObjectDefinition Definition;
    
    // Policy points and progression
    [NonSerialized] public int policyPoints = 0; // Points that can be spent on upgrades for this building type
    [NonSerialized] public float timeSpent = 0; // Accumulated time spent across all instances of this building type
    [NonSerialized] public int currentPointReward = 1; // The number of policy points awarded for the next threshold, increases by 1 each time
    private readonly List<int> rewardThresholds = new List<int>() { 30, 60, 120, 180, 360 };

    // Upgrades unlocked for this building type (shared across all instances)
    private readonly HashSet<Upgrade> typeUnlockedUpgrades = new HashSet<Upgrade>();

    public void AddTime(float delta)
    {
        timeSpent += delta;
        
        if (Definition.UpgradeTree == null || Definition.UpgradeTree.Paths.Length == 0)
            return;
            
        if (rewardThresholds.Count > 0 && timeSpent >= rewardThresholds[0])
        {
            policyPoints += currentPointReward;
            currentPointReward++;
            rewardThresholds.RemoveAt(0);

            foreach (TileObject tileObject in GridManager.Instance.GetTileObjects())
            {
                if (tileObject.Definition == Definition)
                {
                    FlavourManager.Instance.SpawnText(tileObject.Center + Vector3.up * 4,
                        $"+{currentPointReward - 1} policy point{(currentPointReward - 1 > 1 ? "s" : "")}!",
                        Color.green);
                }
            }
        }
    }

    public bool HasUpgradeUnlocked(Upgrade upgrade)
    {
        return typeUnlockedUpgrades.Contains(upgrade);
    }

    public void UnlockUpgrade(Upgrade upgrade)
    {
        typeUnlockedUpgrades.Add(upgrade);
    }

    public bool IsUpgradeUnlockedForType(string upgradeId)
    {
        foreach (var upgrade in typeUnlockedUpgrades)
        {
            if (upgrade.Id == upgradeId)
                return true;
        }
        return false;
    }
}