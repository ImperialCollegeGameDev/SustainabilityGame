using System.Collections.Generic;

/// <summary>
/// Pure game-logic registry + factory for Utility instances.
/// </summary>

public class Utilities
{
    private readonly Dictionary<int, Utility> All_Utilities = new Dictionary<int, Utility>
    {
                                                                        // cost     output  emissions
        [1] = new Utility(1, "Nuclear Plant",       PowerType.Nuclear,      80,     100,    5   ),
        [2] = new Utility(2, "Wind Farm",           PowerType.Wind,         40,     2,      2   ),
        [3] = new Utility(3, "Solar Park",          PowerType.Solar,        20,     1,      1   ),
        [4] = new Utility(4, "Conventional Plant",  PowerType.Conventional, 10,     30,     20  ),
    };

    // For each type, store list of template IDs (first one is used)
    public Dictionary<PowerType, List<int>> UtilityData = new Dictionary<PowerType, List<int>>
    {
        [PowerType.Nuclear] = new List<int> { 1 },
        [PowerType.Wind] = new List<int> { 2 },
        [PowerType.Solar] = new List<int> { 3 },
        [PowerType.Conventional] = new List<int> { 4 },
    };


    public Utility Create(PowerType powerType)          // Creates a new utility instance from a template
    {
        int templateId = UtilityData[powerType][0];
        Utility t = All_Utilities[templateId];

        return new Utility(t.id, t.name, t.type, t.Cost, t.Output, t.Emission);
    }

    public void PrintTemplates()
    {
        UnityEngine.Debug.Log("=== Utility Templates ===");
        foreach (var kvp in All_Utilities)
            UnityEngine.Debug.Log($"Key={kvp.Key} -> {kvp.Value}");
    }
}

