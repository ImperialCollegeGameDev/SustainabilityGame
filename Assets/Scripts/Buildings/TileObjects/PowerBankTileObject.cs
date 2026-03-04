using System;
using UnityEngine;

public class PowerBankTileObject : TileObject
{
    [NonSerialized] public float storedEnergy = 0f; // Current energy stored in this battery
    [NonSerialized] public float chargeRate = 50f; // Energy charged per tick when excess is available
    [NonSerialized] public float dischargeRate = 50f; // Energy discharged per tick when deficit exists
}