using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CityBuilder/UpgradeTree")]
public class UpgradeTree : ScriptableObject
{
    public UpgradePath[] Paths;

    public bool Contains(Upgrade upgrade)
    {
        foreach (UpgradePath path in Paths)
        {
            foreach (Upgrade up in path.Upgrades)
            {
                if (up.Id == upgrade.Id)
                {
                    return true;
                }
            }
        }
        return false;
    }
}

[System.Serializable]
public class UpgradePath
{
    public string PathName;
    public Upgrade[] Upgrades;
}

[System.Serializable]
public class Upgrade
{
    public string Id;
    public string DisplayName;
    public string Description;
    public int Cost;
}