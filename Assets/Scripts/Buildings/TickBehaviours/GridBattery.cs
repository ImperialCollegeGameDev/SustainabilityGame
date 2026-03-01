using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CityBuilder/TickBehaviours/GridBattery")]
public class GridBattery : TickBehaviour
{
    public override void Tick(TileObject tileObject, float delta)
    {
        TileObjectDefinition def = tileObject.Definition;
        if (tileObject is not PowerBankTileObject battery)
        {
            Debug.LogError("TickBehaviour GridBattery applied to non-PowerBank tile object.");
            return;
        }

        PowerBankData powerBankData = def.PowerBank;
        if (powerBankData == null)
        {
            Debug.LogError($"PowerBank TileObject {def.Id} missing PowerBankData.");
            return;
        }

        battery.chargeRate = powerBankData.ChargeRate * delta;
        battery.dischargeRate = powerBankData.DischargeRate * delta;

        if (GameState.Instance.PowerDeficit > 0 && battery.storedEnergy > 0)
        {
            // Calculate how much we can discharge
            float dischargeAmount = Mathf.Min(
                battery.dischargeRate,
                battery.storedEnergy,
                GameState.Instance.PowerDeficit
            );

            // Discharge the battery
            battery.storedEnergy -= dischargeAmount;

            // Supply power to the grid
            float actuallySupplied = GameState.Instance.SupplyPowerFromStorage(dischargeAmount);

            // If we couldn't supply all of it, add it back
            if (actuallySupplied < dischargeAmount)
            {
                battery.storedEnergy += (dischargeAmount - actuallySupplied);
            }
        }
        // Then, try to charge if there's excess power
        else if (GameState.Instance.ExcessPower > 0 && battery.storedEnergy < powerBankData.StorageCapacity)
        {
            // Calculate how much we can charge
            float availableCapacity = powerBankData.StorageCapacity - battery.storedEnergy;
            float chargeAmount = Mathf.Min(
                battery.chargeRate,
                availableCapacity,
                GameState.Instance.ExcessPower
            );

            // Consume excess power from the grid
            float actuallyConsumed = GameState.Instance.ConsumeExcessPower(chargeAmount);

            // Charge the battery
            battery.storedEnergy += actuallyConsumed;
        }

        // Ensure stored energy stays within bounds
        battery.storedEnergy = Mathf.Clamp(battery.storedEnergy, 0f, powerBankData.StorageCapacity);

        tileObject.AddTime(delta);


        // STAT DISPLAY
        List<StatRow> stats = new List<StatRow>();

        // Display current charge
        float chargePercent = (battery.storedEnergy / powerBankData.StorageCapacity) * 100f;
        Color chargeColor = chargePercent > 75f ? Color.green :
                           chargePercent > 25f ? Color.yellow : Color.red;

        stats.Add(new StatRow("Stored Energy", Mathf.RoundToInt(battery.storedEnergy).ToString(), chargeColor));
        stats.Add(new StatRow("Capacity", powerBankData.StorageCapacity.ToString(), Color.cyan));
        stats.Add(new StatRow("Charge %", $"{Mathf.RoundToInt(chargePercent)}%", chargeColor));
        stats.Add(new StatRow("Charge Rate", powerBankData.ChargeRate.ToString(), Color.green));
        stats.Add(new StatRow("Discharge Rate", powerBankData.DischargeRate.ToString(), Color.magenta));

        // Show current status
        string status = "Idle";
        Color statusColor = Color.gray;

        if (GameState.Instance.PowerDeficit > 0 && battery.storedEnergy > 0)
        {
            status = "Discharging";
            statusColor = Color.red;
        }
        else if (GameState.Instance.ExcessPower > 0 && battery.storedEnergy < powerBankData.StorageCapacity)
        {
            status = "Charging";
            statusColor = Color.green;
        }
        else if (battery.storedEnergy >= powerBankData.StorageCapacity)
        {
            status = "Full";
            statusColor = Color.cyan;
        }
        else if (battery.storedEnergy <= 0)
        {
            status = "Empty";
            statusColor = new Color(1f, 0.5f, 0f);
        }

        stats.Add(new StatRow("Status", status, statusColor));

        tileObject.Stats = stats;
    }
}