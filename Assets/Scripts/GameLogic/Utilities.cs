using System.Collections.Generic;

public class Utilities
{
    Dictionary<int, Utility> All_Utilities = new Dictionary<int, Utility>
    {
        [1] = new Utility(1, "Nuclear", PowerType.Nuclear, 300, 1000000000, 100),
        [2] = new Utility(2, "Wind", PowerType.Wind, 120, 10000, 10),
        [3] = new Utility(3, "Solar", PowerType.Solar, 30, 2000, 20),
        [4] = new Utility(4, "Conventional", PowerType.Conventional, 50, 10000000, 1000000000),
    };

    public Dictionary<PowerType, List<int>> UtilityData = new Dictionary<PowerType, List<int>>
    {

    };

    public Utility Create(PowerType powerType)
    {
        return All_Utilities[UtilityData[powerType][0]];
    }


}

