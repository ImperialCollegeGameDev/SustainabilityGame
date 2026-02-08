using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum StatType
{
    Money,
    Energy,
    Emissions,
    UtilitiesCount
}

/// <summary>
/// Generic stat display that can be pointed at one of the GameState stats in the inspector.
/// Drag a TMP Text into `text` and choose the `stat` to display.
/// </summary>
public class DisplayStat : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private StatType stat = StatType.Money;
    [SerializeField] private string prefix = "";
    [SerializeField] private string suffix = "";

    // keep references so we can unsubscribe cleanly
    private Action<int> intSubscription;
    private Action<List<Utility>> utilitiesSubscription;

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
                intSubscription = UpdateFromInt;
                GameState.Instance.OnMoneyChanged += intSubscription;
                UpdateFromInt(GameState.Instance.money);
                break;

            case StatType.Energy:
                intSubscription = UpdateFromInt;
                GameState.Instance.OnEnergyChanged += intSubscription;
                UpdateFromInt(GameState.Instance.TotalEnergy);
                break;

            case StatType.Emissions:
                intSubscription = UpdateFromInt;
                GameState.Instance.OnEmissionsChanged += intSubscription;
                UpdateFromInt(GameState.Instance.TotalEmissions);
                break;

            case StatType.UtilitiesCount:
                utilitiesSubscription = UpdateFromUtilities;
                GameState.Instance.OnUtilitiesChanged += utilitiesSubscription;
                UpdateFromUtilities(new List<Utility>(GameState.Instance.OwnedUtilities));
                break;

            default:
                Debug.LogWarning($"DisplayStat: Unsupported stat {stat}");
                break;
        }
    }

    void UpdateFromInt(int value)
    {
        if (text == null) return;
        text.text = $"{prefix}{value}{suffix}";
    }

    void UpdateFromUtilities(List<Utility> utilities)
    {
        if (text == null) return;
        text.text = $"{prefix}{utilities?.Count ?? 0}{suffix}";
    }

    void OnDestroy()
    {
        if (GameState.Instance == null) return;

        if (intSubscription != null)
        {
            // Try unsubscribing from all int events (safe even if not subscribed)
            GameState.Instance.OnMoneyChanged -= intSubscription;
            GameState.Instance.OnEnergyChanged -= intSubscription;
            GameState.Instance.OnEmissionsChanged -= intSubscription;
            intSubscription = null;
        }

        if (utilitiesSubscription != null)
        {
            GameState.Instance.OnUtilitiesChanged -= utilitiesSubscription;
            utilitiesSubscription = null;
        }
    }
}       
