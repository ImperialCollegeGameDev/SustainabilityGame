using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// this is where all UI commands call functions to influence the game logic
/// + where all the game logic callback functions all are
/// Also now owns placement/purchase rules for tile objects.
/// </summary>
public class GameState : MonoBehaviour
{
    // Singleton
    public static GameState Instance { get; private set; }

    [SerializeField] private Transform platform;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // UI callbacks (UI scripts can subscribe to these)
    public Action<int> OnMoneyChanged;
    public Action<int> OnEnergyChanged;
    public Action<int> OnEmissionsChanged;
    public Action<List<Utility>> OnUtilitiesChanged;

    public int money = 200;

    public List<Utility> OwnedUtilities = new List<Utility>();

    public int TotalEnergy;
    public int TotalEmissions;

    private TileObjectDefinition selectedTile;

    void Start()
    {
        RecomputeTotals();
        PublishUI();
        PrintState();
    }

    public void SetSelectedTile(TileObjectDefinition tile) // Called by UI building selector buttons
    {
        Debug.Log($"Selected building set to {tile.Id}");
        selectedTile = tile;
    }

    // New: central placement + purchase method
    // Returns true if placement succeeded
    public bool TryPlaceSelected(Vector2Int gridPos)
    {
        var def = selectedTile;
        if (def == null)
        {
            Debug.LogWarning($"Selected TileObjectDefinition not found");
            return false;
        }

        if (!GridManager.Instance.CanPlace(def.Size, gridPos))
        {
            Debug.Log("Cannot place there (out of bounds or occupied).");
            return false;
        }

        if (money - def.Cost < 0)
        {
            Debug.Log("Not enough money to place that building.");
            return false;
        }

        // Deduct money
        money -= def.Cost;
        OnMoneyChanged?.Invoke(money);

        // Instantiate visual prefab and place it on grid
        GameObject obj = Instantiate(def.Prefab);
        obj.transform.SetParent(platform, false);

        TileObject tileObj = obj.GetComponent<TileObject>();
        if (tileObj == null)
        {
            Debug.LogWarning("Prefab missing TileObject component.");
            Destroy(obj);
            return false;
        }

        tileObj.DefinitionId = def.Id;
        tileObj.Size = def.Size;
        tileObj.Place(gridPos);

        GridManager.Instance.Occupy(tileObj, gridPos, def.Size);

        // If it's a utility, register it
        if (def.Category == BuildingCategory.Utility)
        {
            // All buildings have an optional Utility field
            OwnedUtilities.Add(def.Utility);
            tileObj.BindLogic(def.Utility);
            RecomputeTotals();
            PublishUI();
            Debug.Log($"Placed utility {def.name} at {gridPos} (cost {def.Cost})");
        }
        else
        {
            // For non-utility buildings we may later create other game-models
            RecomputeTotals();
            PublishUI();
            Debug.Log($"Placed building '{def.Id}' at {gridPos} (cost {def.Cost})");
        }

        PrintState();
        return true;
    }

    public void TryRemoveSelected(Vector2Int gridPos)
    {
        Tile obj_tile = GridManager.Instance.GetTile(gridPos);
        TileObject obj = obj_tile.Occupant;
        
        // Deduct money
        //money += def.Cost / 2;    // simple 50% refund
        //OnMoneyChanged?.Invoke(money);

        // recompute totals
    }

    void RecomputeTotals()
    {
        TotalEnergy = 0;
        TotalEmissions = 0;

        for (int i = 0; i < OwnedUtilities.Count; i++)
        {
            TotalEnergy += OwnedUtilities[i].Output;
            TotalEmissions += OwnedUtilities[i].Emission;
        }
    }

    void PublishUI()
    {
        OnMoneyChanged?.Invoke(money);
        OnEnergyChanged?.Invoke(TotalEnergy);
        OnEmissionsChanged?.Invoke(TotalEmissions);
        OnUtilitiesChanged?.Invoke(new List<Utility>(OwnedUtilities));
    }

    [ContextMenu("DEBUG/Print State")]
    public void PrintState()
    {
        Debug.Log($"Money: {money}      Energy: {TotalEnergy}       Emissions: {TotalEmissions}");
    }

    /*[ContextMenu("DEBUG/Test Build One of Each")]
    public void DebugBuildOneOfEach()
    {
        BuildUtility(PowerType.Solar);
        BuildUtility(PowerType.Wind);
        BuildUtility(PowerType.Conventional);
        BuildUtility(PowerType.Nuclear);
    }*/
}