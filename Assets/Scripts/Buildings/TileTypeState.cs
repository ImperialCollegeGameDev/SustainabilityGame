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
    private readonly List<int> rewardThresholds = new List<int>() { 30, 60, 120, 300, 600, 1100 };

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

            foreach (var tileObj in GridManager.Instance.GetTileObjects(o => o.Definition == Definition))
            {
                FlavourManager.Instance.SpawnText(tileObj.Center + Vector3.up * 2.5f, $"+{currentPointReward - 1} policy point!", Color.paleGreen);
            }
        }
    }
}