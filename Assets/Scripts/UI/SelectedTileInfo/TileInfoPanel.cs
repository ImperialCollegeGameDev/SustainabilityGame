using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TileInfoPanel : MonoBehaviour
{
    public GameObject _StatDisplayPrefab;
    public UtilityBlock UtilityBlock;

    public TextMeshProUGUI TileNameText;
    public UpgradesButton UpgradesButton;
    public DeleteButton DeleteButton;
    public VerticalLayoutGroup TileStatList;

    private TileObject currentTileObject;
    private Dictionary<string, TileStatDisplay> statDisplays = new Dictionary<string, TileStatDisplay>();


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

    void Update()
    {
        if (currentTileObject != null)
        {
            UpdateStats();
        }
    }

    public void SetTile(TileObject tileObj)
    {
        if (tileObj == null)
        {
            Debug.LogError("TileObject passed to SetTile is null.");
            return;
        }

        currentTileObject = tileObj;
        TileObjectDefinition def = tileObj.Definition;

        // Clear previous stats
        foreach (Transform child in TileStatList.transform)
        {
            Destroy(child.gameObject);
        }
        statDisplays.Clear();

        // Add new stats
        TileNameText.text = def.DisplayName;

        // Create stat displays from TickBehaviour
        if (def.TickLogic != null)
        {
            List<StatRow> stats = def.TickLogic.GetStats(tileObj);
            foreach (StatRow stat in stats)
            {
                TileStatDisplay display = CreateStatDisplay(stat.Name, stat.Value.ToString(), stat.Color);
                statDisplays[stat.Name] = display;
            }
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
        }
        else
        {
            UtilityBlock.gameObject.SetActive(false);
        }
        DeleteButton.Init(tileObj);
    }

    private void UpdateStats()
    {
        if (currentTileObject == null || currentTileObject.Definition.TickLogic == null)
        {
            return;
        }

        List<StatRow> stats = currentTileObject.Definition.TickLogic.GetStats(currentTileObject);

        foreach (StatRow stat in stats)
        {
            if (statDisplays.TryGetValue(stat.Name, out TileStatDisplay display))
            {
                display.UpdateValue(stat.Value.ToString(), stat.Color);
            }
        }
    }

    private TileStatDisplay CreateStatDisplay(string name, string value, Color color)
    {
        GameObject obj = Instantiate(_StatDisplayPrefab, TileStatList.transform, false);

        if (!obj.TryGetComponent(out TileStatDisplay statDisplay))
        {
            Debug.LogError("Stat Display Prefab does not have a TileStatDisplay component.");
            return null;
        }
        statDisplay.Init(name, value, color);
        return statDisplay;
    }
}
