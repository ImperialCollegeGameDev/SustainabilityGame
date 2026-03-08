using TMPro;
using UnityEngine;

public class TileStatDisplay : MonoBehaviour
{
    public TextMeshProUGUI statNameText;
    public TextMeshProUGUI statValueText;

    private string statName;

    private void Awake()
    {
        if (statNameText == null)
        {
            Debugger.LogError("Stat Name Text is not assigned in the inspector.");
        }
        if (statValueText == null)
        {
            Debugger.LogError("Stat Value Text is not assigned in the inspector.");
        }
    }

    public void Init(string statName, string statValue, Color statColor)
    {
        this.statName = statName;
        this.statNameText.text = statName;
        UpdateValue(statValue, statColor);
    }

    public void UpdateValue(string statValue, Color statColor)
    {
        statValueText.text = statValue;
        statNameText.color = statColor;
        statValueText.color = statColor;
    }
}
