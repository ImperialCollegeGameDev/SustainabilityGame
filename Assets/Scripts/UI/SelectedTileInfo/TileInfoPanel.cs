using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TileInfoPanel : MonoBehaviour
{
    public GameObject _StatDisplayPrefab;
    private TileStatDisplay StatDisplay;

    public TextMeshProUGUI TileNameText;
    public VerticalLayoutGroup TileStatList;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (TileNameText == null)
        {
            Debug.LogError("Tile Name Text is not assigned in the inspector.");
        }
        if (TileStatList == null)
        {
            Debug.LogError("Tile Stat List is not assigned in the inspector.");
        }
        if (_StatDisplayPrefab == null)
        {
            Debug.LogError("Stat Display Prefab is not assigned in the inspector.");
        }
    }

    public void SetTile(TileObjectDefinition def)
    {
        // Clear previous stats
        foreach (Transform child in TileStatList.transform)
        {
            Destroy(child.gameObject);
        }

        // Add new stats
        TileNameText.text = def.DisplayName;
        foreach (StatRow stat in def.GetStats())
        {
            if (stat.Name == "Cost") continue;
            Debug.Log("Stat name: " + stat.Name);
            Debug.Log("Stat value: " + stat.Value.ToString());
            Debug.Log("Stat color: " + stat.Color);
            CreateStatDisplay(stat.Name, stat.Value.ToString(), stat.Color);
        }
    }

    private void CreateStatDisplay(string name, string value, Color color)
    {
        Debug.Log("Creating stat display");
        GameObject obj = Instantiate(_StatDisplayPrefab, TileStatList.transform, false);

        if (!obj.TryGetComponent(out TileStatDisplay statDisplay))
        {
            Debug.LogError("Stat Display Prefab does not have a TileStatDisplay component.");
            return;
        }
        statDisplay.Init(name, value, color);
    }
}
