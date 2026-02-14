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

    private TileObjectDefinition buildingToBePlaced;
    public InteractionMode CurrentMode { get; private set; } = InteractionMode.None;

    void Start()
    {
        RecomputeTotals();
    }

    public void SetSelectedTile(TileObjectDefinition tile) // Called by UI building selector buttons
    {
        Debug.Log($"Selected building set to {tile.Id}");
        buildingToBePlaced = tile;
    }

    // New: central placement + purchase method
    // Returns true if placement succeeded
    public bool TryPlaceSelected(Vector2Int gridPos)
    {
        var def = buildingToBePlaced;
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
        Debug.Log($"Instantiated prefab for '{def.Id}' at {gridPos}");

        TileObject tileObj = obj.GetComponent<TileObject>(); // TileObject is attached to the model
        if (tileObj == null)
        {
            Debug.LogWarning("Prefab missing TileObject component.");
            Destroy(obj);
            return false;
        }

        tileObj.DefinitionId = def.Id; // So TileObject can reference back to its definition data if needed
        tileObj.Place(gridPos); // Handles location of the physical model

        GridManager.Instance.Occupy(tileObj, gridPos, def.Size); // Handles grid logic - marking tiles as occupied

        // If it's a utility, register it
        if (def.Category == BuildingCategory.Utility)
        {
            // All buildings have an optional Utility field
            OwnedUtilities.Add(def.Utility);
            RecomputeTotals();
            Debug.Log($"Placed utility {def.name} at {gridPos} (cost {def.Cost})");
        }
        else
        {
            // For non-utility buildings we may later create other game-models
            Debug.Log($"Placed building '{def.Id}' at {gridPos} (cost {def.Cost})");
        }

        return true;
    }

    public void Delete(TileObject obj)
    {
        obj.Remove(); // Handles visual/model removal

        TileObjectDefinition def = obj.GetDefinition();
        money += def.Cost / 2; // simple 50% refund

        GridManager.Instance.Clear(obj.Origin, def.Size); // Handles grid logic of marking tiles as unoccupied

        // If it's a utility, unregister it
        if (def.Category == BuildingCategory.Utility)
        {
            // All buildings have an optional Utility field
            OwnedUtilities.Remove(def.Utility);
            RecomputeTotals();
            Debug.Log($"Deleted utility {def.name}.");
        }
        else
        {
            // For non-utility buildings
            Debug.Log($"Deleted building {def.name}.");
        }
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

        PublishUI();
    }

    void PublishUI()
    {
        OnMoneyChanged?.Invoke(money);
        OnEnergyChanged?.Invoke(TotalEnergy);
        OnEmissionsChanged?.Invoke(TotalEmissions);
        OnUtilitiesChanged?.Invoke(new List<Utility>(OwnedUtilities));
    }

    public enum InteractionMode
    {
        None,
        Select,
        Place,
        Delete
    }

    public void SetModeNone()
    {
        SelectionManager.Instance.Deselect();
        CurrentMode = InteractionMode.None;
        Debug.Log("Interaction mode set to None");
    }

    public void SetModeSelect(bool toggleMode = false)
    {
        if (toggleMode && CurrentMode == InteractionMode.Select)
        {
            SetModeNone();
            return;
        }
        CurrentMode = InteractionMode.Select;
        Debug.Log("Interaction mode set to Select");
    }

    public void SetModePlace(bool toggleMode = false)
    {
        SelectionManager.Instance.Deselect();
        if (toggleMode && CurrentMode == InteractionMode.Place)
        {
            SetModeNone();
            return;
        }
        CurrentMode = InteractionMode.Place;
        Debug.Log("Interaction mode set to Place");
    }

    public void SetModeDelete(bool toggleMode = false)
    {
        SelectionManager.Instance.Deselect();
        if (toggleMode && CurrentMode == InteractionMode.Delete)
        {
            SetModeNone();
            return;
        }
        CurrentMode = InteractionMode.Delete;
        Debug.Log("Interaction mode set to Delete");
    }
}