using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class TileStatDisplay : MonoBehaviour
{
    public TextMeshProUGUI statNameText;
    public TextMeshProUGUI statValueText;

    private void Awake()
    {
        if (statNameText == null)
        {
            Debug.LogError("Stat Name Text is not assigned in the inspector.");
        }
        if (statValueText == null)
        {
            Debug.LogError("Stat Value Text is not assigned in the inspector.");
        }
    }

    public void Init(string statName, string statValue, Color statColor)
    {
        this.statNameText.text = statName;
        this.statValueText.text = statValue;
        this.statNameText.color = statColor;
        this.statValueText.color = statColor;

        if (statName == "Power")
        {
            statValueText.text = NumberFormatter.FormatPower(double.Parse(statValue));
        }
    }
}
