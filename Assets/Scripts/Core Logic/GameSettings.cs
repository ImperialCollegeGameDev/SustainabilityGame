using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "Config/GameSettings")]
public class GameSettings : ScriptableObject
{
    public int EnergyReqPerPerson = 500;
    public int MaxEmission = 123;
    public int StartingMoney = 77500;
    public float TaxRate = 0.5f;
    public float DissatisfactionDanger = 2.0f;
    public float HappinessVolatility = 0.005f;
}
