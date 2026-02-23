using TMPro;
using UnityEngine;

public class UtilityBlock : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI EfficiencyText;
    [SerializeField] private RepairButton RepairButton;

    private UtilityTileObject util;

    public void Init(UtilityTileObject util)
    {
        RepairButton.Init(util);
        this.util = util;
    }

    void Update()
    {
        EfficiencyText.text = "Operating at " + Mathf.RoundToInt(util.efficiency * 100) + "% efficiency";
    }
}