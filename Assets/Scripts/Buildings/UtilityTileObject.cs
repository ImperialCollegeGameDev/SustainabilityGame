using System;
using UnityEngine;

public class UtilityTileObject : TileObject
{
    [NonSerialized] public float efficiency = 1f;
    [NonSerialized] public float repairCostMult = 1f;

    public int CurrentRepairCost()
    {
        float damagePercent = 1f - efficiency;
        float repairMultiplier = Mathf.Floor(damagePercent * 50f) / 50f;

        return Mathf.RoundToInt(
            repairMultiplier
            * GameState.Instance.Settings.FullRepairCost
            * Definition.Cost
        );
    }

    public void TryRepair()
    {
        if (GameState.Instance.money >= CurrentRepairCost())
        {
            GameState.Instance.ChangeMoney(-CurrentRepairCost());
            efficiency = 1f;
            Notifications.Instance.PostNotification($"Successfully repaired {Definition.DisplayName}!");
        } else
        {
            Notifications.Instance.PostNotification("Not enough money to repair!");
        }
    }
}