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
    //public bool TryPlaceSelected(Vector2Int gridPos)
    //{
    //    return gameLogic.BuyBuilding(selectedTile, gridPos);
    //}

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