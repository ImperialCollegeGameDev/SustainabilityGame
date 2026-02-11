using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles UI commands and delegates game logic to GameLogic
/// </summary>
public class GameState : MonoBehaviour, IGameState
{
    // Singleton
    public static GameState Instance { get; private set; }

    [SerializeField] public Transform platform;

    private GameLogic gameLogic;

    public int Money
    {
        get => money;
        set => money = value;
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        gameLogic = new GameLogic(this);
    }

    // UI callbacks (UI scripts can subscribe to these)
    public Action<int> OnMoneyChangedEvent;
    public Action<int> OnEnergyChangedEvent;
    public Action<int> OnEmissionsChangedEvent;
    public Action<List<Utility>> OnUtilitiesChangedEvent;

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

    // Central placement + purchase method
    public bool TryPlaceSelected(Vector2Int gridPos)
    {
        // Example: Only allow placement if a tile is selected and enough money
        if (selectedTile == null) return false;
        if (money < selectedTile.Cost) return false;
        // Place logic here (call GameLogic, update money, etc.)
        money -= selectedTile.Cost;
        OnMoneyChangedEvent?.Invoke(money);
        // Add to utilities if it's a utility
        if (selectedTile.Category == BuildingCategory.Utility && selectedTile.Utility != null)
        {
            OwnedUtilities.Add(selectedTile.Utility);
            PublishUI();
        }
        // Placement logic (instantiate prefab, occupy grid, etc.) should be here in a real game
        return true;
    }

    //public bool TryUpgradeSelected(Vector2Int gridPos, TileObjectDefinition upgradeDef)
    //{
    //    Tile obj_tile = GridManager.Instance.GetTile(gridPos);
    //    TileObject obj = obj_tile.Occupant;
    //    return gameLogic.UpgradeBuilding(obj, upgradeDef);
    //}

    public void TryRemoveSelected(Vector2Int gridPos)
    {
        Tile obj_tile = GridManager.Instance.GetTile(gridPos);
        TileObject obj = obj_tile.Occupant;
        // Removal logic can be added to GameLogic if needed
    }

    public void OnMoneyChanged(int money)
    {
        OnMoneyChangedEvent?.Invoke(money);
    }

    public void OnEnergyChanged(int energy)
    {
        OnEnergyChangedEvent?.Invoke(energy);
    }

    public void OnEmissionsChanged(int emissions)
    {
        OnEmissionsChangedEvent?.Invoke(emissions);
    }

    public void OnUtilitiesChanged(List<Utility> utilities)
    {
        OnUtilitiesChangedEvent?.Invoke(utilities);
    }

    public void RecomputeTotals()
    {
        TotalEnergy = 0;
        TotalEmissions = 0;

        for (int i = 0; i < OwnedUtilities.Count; i++)
        {
            TotalEnergy += OwnedUtilities[i].Output;
            TotalEmissions += OwnedUtilities[i].Emission;
        }
    }

    public void PublishUI()
    {
        OnMoneyChangedEvent?.Invoke(money);
        OnEnergyChangedEvent?.Invoke(TotalEnergy);
        OnEmissionsChangedEvent?.Invoke(TotalEmissions);
        OnUtilitiesChangedEvent?.Invoke(new List<Utility>(OwnedUtilities));
    }

    [ContextMenu("DEBUG/Print State")]
    public void PrintState()
    {
        Debug.Log($"Money: {money}      Energy: {TotalEnergy}       Emissions: {TotalEmissions}");
    }
}