using System;
using UnityEngine;

public class UtilityTileObject : TileObject
{
    [NonSerialized] public float efficiency = 1f;
    [NonSerialized] public float repairCostMult = 1f;

    // Cached calculated values from last Tick
    [NonSerialized] public float actualOutput = 0;
    [NonSerialized] public float actualEmission = 0;
    [NonSerialized] public float outputMultiplier = 1f;
    [NonSerialized] public float emissionMultiplier = 1f;
    [NonSerialized] public float degradeMultiplier = 1f;

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
            MusicManager.Instance.PlayUISound(MusicManager.UISoundType.Repair);
            FlavourManager.Instance.SpawnRepairParticles(Center + Vector3.up * 1.2f);
        }
    }
}