using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "Config/GameSettings")]
public class GameSettings : ScriptableObject
{
    public int EnergyReqPerPerson = 5;
    public float NighttimeEnergyMultiplier = 0.4f;
    public float MaxEmissionLogarithmic = 1.5f;
    public int EmissionLogBase = 7; // Lower is more punishing
    public float EmissionScale = 4.5f;
    public int StartingMoney = 775;
    public float TaxRate = 0.0003f;
    public float DissatisfactionDanger = 3.0f;
    public float HappinessVolatility = 0.02f;
    public float SellRatio = 0.5f;
    public float MinimumEfficiency = 0.4f;
    public float FullRepairCost = 0.8f;
    public float AtmosphericDissipation = 0.02f;
}
