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

    void UpdateFromInt(int value)
    {
        if (text == null) return;
        text.text = $"{prefix}{FormatNumber(value)}{suffix}";
    }

    void UpdateFromUtilities(List<Utility> utilities)
    {
        if (text == null) return;
        text.text = $"{prefix}{FormatNumber(utilities?.Count ?? 0)}{suffix}";
    }

    /// <summary>
    /// Formats integer values:
    /// - less than 1000: as-is ("999")
    /// - 1,000 to 999,999: one decimal 'k' ("3356" -> "3.3k")
    /// - 1,000,000 and above: one decimal 'm' ("3356750" -> "3.3m")
    /// Keeps sign for negative numbers and trims trailing ".0" (e.g. "1.0k" -> "1k").
    /// </summary>
    private string FormatNumber(int value)
    {
        int absValue = Math.Abs(value);
        string sign = value < 0 ? "-" : "";

        if (absValue < 1000)
        {
            return sign + absValue.ToString() + " ";
        }

        if (absValue < 1_000_000)
        {
            float v = absValue / 1000f;
            // "0.#" yields one decimal if needed, no decimal if .0
            string s = v.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
            return sign + s + " K";
        }

        float mv = absValue / 1_000_000f;
        string sm = mv.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        return sign + sm + " M";
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
            GameState.Instance.OnPopulationChanged -= intSubscription;
            GameState.Instance.OnHappinessChanged -= intSubscription;
            intSubscription = null;
        }

        if (utilitiesSubscription != null)
        {
            GameState.Instance.OnUtilitiesChanged -= utilitiesSubscription;
            utilitiesSubscription = null;
        }
    }
}
