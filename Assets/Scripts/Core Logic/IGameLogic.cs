using System.Collections.Generic;

public interface IGameLogic
{
    int TotalEnergy { get; }
    int TotalEmissions { get; }
    List<Utility> OwnedUtilities { get; }
    void RecomputeTotals();
    void AddUtility(Utility utility);
    // Add similar methods for Residential and Industry as needed
}