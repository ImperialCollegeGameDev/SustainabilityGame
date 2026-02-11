using System;
using System.Collections.Generic;

public class GameLogic : IGameLogic
{
    private readonly IGameState gameState;

    public int TotalEnergy { get; private set; }
    public int TotalEmissions { get; private set; }
    public List<Utility> OwnedUtilities { get; } = new List<Utility>();
    public List<Residential> OwnedResidentials { get; } = new List<Residential>();
    //public List<Industry> OwnedIndustries { get; } = new List<Industry>();

    public GameLogic(IGameState state)
    {
        gameState = state;
        RecomputeTotals();
    }

    public bool CanAfford(int cost) => gameState.Money >= cost;

    public void SpendMoney(int amount)
    {
        gameState.Money -= amount;
        gameState.OnMoneyChanged(gameState.Money);
    }

    public void AddUtility(Utility utility)
    {
        OwnedUtilities.Add(utility);
        RecomputeTotals();
        gameState.OnUtilitiesChanged(new List<Utility>(OwnedUtilities));
    }

    public void AddResidential(Residential residential)
    {
        OwnedResidentials.Add(residential);
        RecomputeTotals();
    }

    //public void AddIndustry(Industry industry)
    //{
    //    OwnedIndustries.Add(industry);
    //    RecomputeTotals();
    //}

    public void UpgradeUtility(int index, Utility upgradedUtility)
    {
        if (index >= 0 && index < OwnedUtilities.Count)
        {
            OwnedUtilities[index] = upgradedUtility;
            RecomputeTotals();
        }
    }

    public void UpgradeResidential(int index, Residential upgradedResidential)
    {
        if (index >= 0 && index < OwnedResidentials.Count)
        {
            OwnedResidentials[index] = upgradedResidential;
            RecomputeTotals();
        }
    }

    //public void UpgradeIndustry(int index, Industry upgradedIndustry)
    //{
    //    if (index >= 0 && index < OwnedIndustries.Count)
    //    {
    //        OwnedIndustries[index] = upgradedIndustry;
    //        RecomputeTotals();
    //    }
    //}

    public void RecomputeTotals()
    {
        TotalEnergy = 0;
        TotalEmissions = 0;

        foreach (var utility in OwnedUtilities)
        {
            TotalEnergy += utility.Output;
            TotalEmissions += utility.Emission;
        }
        // Add similar calculations for residential and industry if needed
        gameState.OnEnergyChanged(TotalEnergy);
        gameState.OnEmissionsChanged(TotalEmissions);
    }
}