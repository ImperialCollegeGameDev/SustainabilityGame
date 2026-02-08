using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// this is where all UI commands call functions to influence the game logic
/// + where all the game logic callback functions all are
/// </summary>

public class GameState : MonoBehaviour
{
    // UI callbacks (UI scripts can subscribe to these)
    public Action<int> OnMoneyChanged;
    public Action<int> OnEnergyChanged;
    public Action<int> OnEmissionsChanged;
    public Action<List<Utility>> OnUtilitiesChanged;

    public int money = 200;

    public List<Utility> OwnedUtilities = new List<Utility>();

    public int TotalEnergy;
    public int TotalEmissions;

    Utilities utilities;

    PowerType currentType;

    void Start()
    {
        utilities = new Utilities();

        currentType = PowerType.Solar;

        utilities.PrintTemplates();
        RecomputeTotals();
        PublishUI();
        PrintState();
    }


    public void SetCurrentType(int typeIndex)
    {
        currentType = (PowerType)typeIndex;
        Debug.Log($"Current power type set to {currentType}");
        BuildUtility(currentType);
    }


    // UI calls this
    public void BuildUtility(PowerType powerType)
    {
        Utility u = utilities.Create(powerType);

        if (money - u.Cost < 0)
        {
            Debug.Log("Youre broke af :<");
            return;
        }

        money -= u.Cost;

        OwnedUtilities.Add(u);

        RecomputeTotals();
        PublishUI();

        Debug.Log($"Built {u.name} (cost {u.Cost})");
        PrintState();
    }

    // UI calls this (index in the OwnedUtilities list)
    public void SellUtility(int index)
    {
        Utility u = OwnedUtilities[index];
        OwnedUtilities.RemoveAt(index);

        money += u.Cost / 2; // simple 50% refund

        RecomputeTotals();
        PublishUI();

        Debug.Log($"Sold {u.name} (+{u.Cost / 2})");
        PrintState();
    }

    void RecomputeTotals()
    {
        TotalEnergy = 0;
        TotalEmissions = 0;

        for (int i = 0; i < OwnedUtilities.Count; i++)
        {
            TotalEnergy += OwnedUtilities[i].Output;
            TotalEmissions += OwnedUtilities[i].Emission;
        }
    }

    void PublishUI()
    {
        OnMoneyChanged?.Invoke(money);
        OnEnergyChanged?.Invoke(TotalEnergy);
        OnEmissionsChanged?.Invoke(TotalEmissions);
        OnUtilitiesChanged?.Invoke(new List<Utility>(OwnedUtilities));
    }

    [ContextMenu("DEBUG/Print State")]
    public void PrintState()
    {
        Debug.Log($"Money: {money}      Energy: {TotalEnergy}       Emissions: {TotalEmissions}");
    }

    [ContextMenu("DEBUG/Test Build One of Each")]
    public void DebugBuildOneOfEach()
    {
        BuildUtility(PowerType.Solar);
        BuildUtility(PowerType.Wind);
        BuildUtility(PowerType.Conventional);
        BuildUtility(PowerType.Nuclear);
    }
}
