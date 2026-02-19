using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "Config/GameSettings")]
public class GameSettings : ScriptableObject
{
    public int EnergyReqPerPerson = 500;
    public int MaxEmission = 10000;
    public int StartingMoney = 77500;
    public float TaxRate = 1f;
    public float DissatisfactionDanger = 3.0f;
    public float HappinessVolatility = 0.007f;
    public float SellRatio = 0.5f;
}
