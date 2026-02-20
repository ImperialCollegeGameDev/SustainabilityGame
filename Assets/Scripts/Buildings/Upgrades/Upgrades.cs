using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CityBuilder/UpgradeTree")]
public class UpgradeTree : ScriptableObject
{
    public UpgradePath[] Paths;
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