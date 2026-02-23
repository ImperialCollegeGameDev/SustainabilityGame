using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TileInfoPanel : MonoBehaviour
{
    public GameObject _StatDisplayPrefab;
    public UtilityBlock UtilityBlock;

    public TextMeshProUGUI TileNameText;
    public UpgradesButton UpgradesButton;
    public VerticalLayoutGroup TileStatList;


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

    public void SetTile(TileObject tileObj)
    {
        if (tileObj == null) {
            Debug.LogError("TileObject passed to SetTile is null.");
            return;
        }
        TileObjectDefinition def = tileObj.Definition;
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
            CreateStatDisplay(stat.Name, stat.Value.ToString(), stat.Color);
        }

        if (tileObj.Definition.UpgradeTree != null)
        {
            UpgradesButton.gameObject.SetActive(true);
            UpgradesButton.tileObject = tileObj;
        }
        else
        {
            UpgradesButton.gameObject.SetActive(false);
        }

        if (tileObj is UtilityTileObject util)
        {
            UtilityBlock.gameObject.SetActive(true);
            UtilityBlock.Init(util);
        } else
        {
            UtilityBlock.gameObject.SetActive(false);
        }
    }

    private void CreateStatDisplay(string name, string value, Color color)
    {
        GameObject obj = Instantiate(_StatDisplayPrefab, TileStatList.transform, false);

        if (!obj.TryGetComponent(out TileStatDisplay statDisplay))
        {
            Debug.LogError("Stat Display Prefab does not have a TileStatDisplay component.");
            return;
        }
        statDisplay.Init(name, value, color);
    }
}
