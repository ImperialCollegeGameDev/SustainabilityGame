using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum StatType
{
    Money,
    Energy,
    Emissions,
    UtilitiesCount,
    Population,
    Happiness
}

/// <summary>
/// Generic stat display that can be pointed at one of the GameState stats in the inspector.
/// Drag a TMP Text into `text` and choose the `stat` to display.
/// </summary>
public class DisplayStat : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    public StatType stat = StatType.Money;
    [SerializeField] private string prefix = "";
    [SerializeField] private string suffix = "";

    // keep references so we can unsubscribe cleanly
    private Action<int> intSubscription;
    private Action<long> longSubscription;

    void Start()
    {
        if (text == null)
        {
            Debug.LogWarning("DisplayStat: TMP_Text reference is null.");
            return;
        }

        if (GameState.Instance == null)
        {
            Debug.LogWarning("DisplayStat: GameState.Instance is null. Make sure GameState exists in the scene.");
            return;
        }

        // Subscribe to the selected stat's event and initialize the text with current value
        switch (stat)
        {
            case StatType.Money:
                longSubscription = UpdateFromLong;
                GameState.Instance.OnMoneyChanged += longSubscription;
                UpdateFromLong(GameState.Instance.money);
                break;

            case StatType.Energy:
                intSubscription = UpdateFromInt;
                GameState.Instance.OnEnergyChanged += intSubscription;
                UpdateFromInt(GameState.Instance.Power);
                break;

            case StatType.Emissions:
                intSubscription = UpdateFromInt;
                GameState.Instance.OnEmissionsChanged += intSubscription;
                UpdateFromInt(Mathf.FloorToInt(GameState.Instance.TotalEmissions));
                break;

            case StatType.Population:
                intSubscription = UpdateFromInt;
                GameState.Instance.OnPopulationChanged += intSubscription;
                UpdateFromInt(GameState.Instance.population);
                break;

            case StatType.Happiness:
                intSubscription = UpdateFromInt;
                GameState.Instance.OnHappinessChanged += intSubscription;
                UpdateFromInt(Mathf.RoundToInt(GameState.Instance.happiness));
                break;

            default:
                Debug.LogWarning($"DisplayStat: Unsupported stat {stat}");
                break;
        }
    }

    void UpdateFromInt(int _value)
    {
        long value = _value;
        if (text == null) return;
        if (stat == StatType.Energy) value *= 1000;
        String formattedValue = NumberFormatter.Format(value, true);
        text.text = $"{prefix}{formattedValue}{suffix}".Trim();
    }

    void UpdateFromLong(long value)
    {
        if (text == null) return;
        if (stat == StatType.Money)
        {
            text.text = NumberFormatter.FormatMoney(value, false);
            return;
        }
        text.text = $"{prefix}{NumberFormatter.Format(value, true)}{suffix}".Trim();
    }

    void OnDestroy()
    {
        if (GameState.Instance == null) return;

        if (intSubscription != null)
        {
            // Try unsubscribing from all int events (safe even if not subscribed)
            GameState.Instance.OnMoneyChanged -= longSubscription;
            GameState.Instance.OnEnergyChanged -= intSubscription;
            GameState.Instance.OnEmissionsChanged -= intSubscription;
            GameState.Instance.OnPopulationChanged -= intSubscription;
            GameState.Instance.OnHappinessChanged -= intSubscription;
            intSubscription = null;
        }
    }
}
