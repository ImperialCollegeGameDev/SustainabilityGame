using UnityEngine;

public class Nightlight : MonoBehaviour
{
    [SerializeField] private Light lightComponent;
    void OnEnable()
    {
        DayNight.OnNightStarted += TurnOn;
        DayNight.OnDayStarted += TurnOff;
    }

    void OnDisable()
    {
        DayNight.OnNightStarted -= TurnOn;
        DayNight.OnDayStarted -= TurnOff;
    }

    void TurnOn()
    {
        lightComponent.enabled = true;
    }

    void TurnOff()
    {
        lightComponent.enabled = false;
    }
}
