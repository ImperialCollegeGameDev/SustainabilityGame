using System.Collections.Generic;

public interface IGameState
{
    int Money { get; set; }
    void OnMoneyChanged(int money);
    void OnEnergyChanged(int energy);
    void OnEmissionsChanged(int emissions);
    void OnUtilitiesChanged(List<Utility> utilities);
}