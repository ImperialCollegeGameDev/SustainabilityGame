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

        // Initialize the buildings list
        buildings = new List<TileObject>();

        if (Settings == null)
        {
            Debug.Log("GameSettings not assigned in GameState. Using default settings.");
            Settings = ScriptableObject.CreateInstance<GameSettings>();
        }
    }

    public GameObject GameOverPrefab;
    public GameSettings Settings;
    public bool PAUSED = true; // game doesnt exist on first load
    private float _timer = 0f;
    
    private const float TickInterval = 0.3f;
    private const float FastTickInterval = 0.2f;
    private float Timescale = 1f;
    private bool isTicking = true;
    private float _fastTimer = 0f;

    // UI callbacks (UI scripts can subscribe to these)
    public Action<long> OnMoneyChanged;
    public Action<int> OnEnergyChanged;
    public Action<int> OnEmissionsChanged;
    public Action<int> OnPopulationChanged;
    public Action<int> OnHappinessChanged;

    public long money { get; private set; }
    public int population = 0;
    public float weightedHappinessSum = 0;
    public int dissatisfiedPopulation { get; private set; } = 0;
    private int projectedHappiness = 100;
    public float happiness { get; private set; } = 100;

    // Score Management - moved from Main
    private int maxPopulation = 0;
    private int currentScore = 0;

    public float effectiveEnergyReqPerPerson = 0;
    public int Power = 0;
    public float ExcessPower { get; private set; } = 0; // Excess energy produced this tick, available for storage
    public float PowerDeficit { get; private set; } = 0; // Power shortage this tick, can be supplied by batteries
    
    public float TotalEmissions { get; private set; } = 0;
    public float EmissionsDelta = 0;
    public float PreviousEmissionsDelta { get; private set; } = 0;
    public float EmissionsReductionDelta = 0;
    public float EmissionsLogarithmicScale => Mathf.Log(TotalEmissions + 1 + Mathf.Pow(Settings.EmissionLogBase, Settings.EmissionScale)) / Mathf.Log(Settings.EmissionLogBase) - Settings.EmissionScale;
    public float EmissionsPercentage => EmissionsLogarithmicScale / Settings.MaxEmissionLogarithmic;

    public int requiredEnergy { get; private set; } = 0;

    public TileObjectDefinition buildingToBePlaced;
    public InteractionMode CurrentMode { get; private set; } = InteractionMode.None;

    // keeps track of unlocked skills and corresponding buildings
    HashSet<string> unlockedBuildings = new HashSet<string>();
    public bool IsBuildingUnlocked(string id) => unlockedBuildings.Contains(id);
    public event Action OnBuildingUnlocksChanged;

    // Maintains the authoritative list of all placed buildings in the game
    private List<TileObject> buildings = new List<TileObject>();

    void Start()
    {
        money = Settings.StartingMoney;
        //UpdateHappinessAndDisplay();
    }

    private void Update()
    {
        if (PAUSED) return;

        _timer += Time.deltaTime;

        if (_timer >= TickInterval)
        {
            Tick(TickInterval * Timescale);
            _timer -= TickInterval;
        }

        _fastTimer += Time.deltaTime;
        if (_fastTimer >= FastTickInterval)
        {
            FastTick(FastTickInterval * Timescale);
            _fastTimer -= FastTickInterval;
        }

        if (happiness <= 25f)
        {
            MusicManager.Instance?.PlayGameSFX(MusicManager.SFXSoundType.LungCancer);
        } else if (happiness > 99f) {
            MusicManager.Instance?.PlayGameSFX(MusicManager.SFXSoundType.LSD);
        }

        // Check for game over condition
        if (happiness <= 3f)
        {
            PAUSED = true;
            Instantiate(GameOverPrefab);
        }


    }

    public void Tick(float delta) // Delta is the time in seconds since last tick
    {
        if (!isTicking) return;

        population = 0;
        Power = 0;
        weightedHappinessSum = 0;
        EmissionsDelta = 0;
        EmissionsReductionDelta = 0;

        // Use the internal buildings list instead of querying GridManager
        foreach (TileObject tileObj in buildings)
        {
            tileObj.Tick(delta);
            if (tileObj is ResidentialTileObject res)
            {
                population += Mathf.FloorToInt(res.occupancy);
                weightedHappinessSum += Mathf.FloorToInt(res.occupancy * res.LocalHappiness);
            }
        }

        // Calculate energy requirements
        effectiveEnergyReqPerPerson = Settings.EnergyReqPerPerson;
        if (!DayNight.Instance.IsDaytime)
            effectiveEnergyReqPerPerson = Settings.EnergyReqPerPerson * Settings.NighttimeEnergyMultiplier;

        requiredEnergy = Mathf.CeilToInt(population * effectiveEnergyReqPerPerson);

        // Calculate excess power or deficit BEFORE battery processing
        if (Power >= requiredEnergy)
        {
            ExcessPower = Power - requiredEnergy;
            PowerDeficit = 0;
        }
        else
        {
            ExcessPower = 0;
            PowerDeficit = requiredEnergy - Power;
        }

        TotalEmissions += EmissionsDelta + EmissionsReductionDelta;
        TotalEmissions -= Settings.AtmosphericDissipation * TotalEmissions * delta;
        TotalEmissions = Math.Max(TotalEmissions, 0);

        PreviousEmissionsDelta = EmissionsDelta;

        TaxThePoor(delta);
        UpdateHappinessAndDisplay();

        // Update max population tracking
        if (population > maxPopulation)
        {
            maxPopulation = population;
            currentScore = maxPopulation;
        }
    }

    public float ConsumeExcessPower(float amount)
    {
        float consumed = Mathf.Min(amount, ExcessPower);
        ExcessPower -= consumed;
        return consumed;
    }

    public float SupplyPowerFromStorage(float amount)
    {
        float supplied = Mathf.Min(amount, PowerDeficit);
        PowerDeficit -= supplied;
        Power += Mathf.FloorToInt(supplied); // Add to total power
        return supplied;
    }

    public void FastTick(float delta) // For things that are very inexpensive to compute and we want fast feedback on
    {
        happiness += (projectedHappiness - happiness) * Math.Min(1, Settings.HappinessVolatility * delta * Timescale);
        if (Math.Abs(happiness - projectedHappiness) < 0.1f)
            happiness = projectedHappiness;
        happiness = Mathf.Max(happiness, 0);

        OnHappinessChanged?.Invoke(Mathf.RoundToInt(happiness));
    }

    public void SetSelectedTile(TileObjectDefinition tile) // Called by UI building selector buttons
    {
        buildingToBePlaced = tile;
        Notifications.Instance.PostNotification($"Selected building set to {tile.Id}");       // if do "PostNotification" first, next following lines arr never executed!!!
    }

    public void UpdateHappinessAndDisplay()
    {
        dissatisfiedPopulation = Mathf.FloorToInt(population - Power / effectiveEnergyReqPerPerson);
        dissatisfiedPopulation = Math.Max(dissatisfiedPopulation, 0);

        projectedHappiness = Mathf.RoundToInt(100f * (1 - 1.5f * EmissionsPercentage));
        projectedHappiness = Math.Max(projectedHappiness, 0);

        if (population > 0)
        {
            projectedHappiness = Mathf.FloorToInt(projectedHappiness * (1 - Settings.DissatisfactionDanger * dissatisfiedPopulation / (float)population));
            projectedHappiness = Mathf.FloorToInt(projectedHappiness * (weightedHappinessSum / population));
        }

        StatChangeUpdate();
    }

    void TaxThePoor(float delta)
    {
        money += Mathf.CeilToInt(population * Settings.TaxRate * delta);
    }

    private void StatChangeUpdate()
    {
        OnMoneyChanged?.Invoke(money);
        OnEnergyChanged?.Invoke(Power);
        OnEmissionsChanged?.Invoke(Mathf.FloorToInt(TotalEmissions));
        OnPopulationChanged?.Invoke(population);
    }

    public void ChangeMoney(long amount)
    {
        money += amount;
        OnMoneyChanged?.Invoke(money);
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
        GridMouse.Instance.ClearPlacementHighlight();
        SelectionManager.Instance.Deselect();
        CurrentMode = InteractionMode.None;
    }

    public void SetModeSelect() //bool toggleMode = false)
    {
        CurrentMode = InteractionMode.Select;
        GridMouse.Instance.ClearPlacementHighlight();
    }

    public void SetModePlace() //bool toggleMode = false)
    {
        SelectionManager.Instance.Deselect();
        CurrentMode = InteractionMode.Place;
        if (buildingToBePlaced == null)
        {
            Debug.Log("No building selected to preview.");
            return;
        } else if (buildingToBePlaced.Prefab == null)
        {
            Debug.LogError($"Selected building '{buildingToBePlaced.Id}' does not have a prefab assigned.");
            return;
        } else if (buildingToBePlaced.Prefab.GetComponent<TileObject>() == null)
        {
            Debug.LogError($"Prefab for building '{buildingToBePlaced.Id}' does not have a TileObject component.");
            return;
        }
        GameObject obj = Instantiate(buildingToBePlaced.Prefab);
        TileObject tileObj = obj.GetComponent<TileObject>();
        GridMouse.Instance.SetPreview(tileObj);
        tileObj.Init(buildingToBePlaced);
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
        GridMouse.Instance.ClearPlacementHighlight();
    }

    public void UnlockBuilding(string id)
    {
        if (unlockedBuildings.Add(id))
            OnBuildingUnlocksChanged?.Invoke();
    }

    public void SetTicking(bool value)
    {
        isTicking = value;
    }

    public void ToggleTicking()
    {
        isTicking = !isTicking;
    }

    public void SetTimescale(float value = 1)
    {
        Timescale = value;
    }

    #region Score Management

    public int GetScore()
    {
        return currentScore;
    }

    public int GetMaxPopulation()
    {
        return maxPopulation;
    }

    public void ResetScore()
    {
        maxPopulation = 0;
        currentScore = 0;
        happiness = 100f;
    }

    #endregion

    #region Building Management

    /// <summary>
    /// Register a building when it's placed on the grid
    /// Called by GridManager after successful placement
    /// </summary>
    public void RegisterBuilding(TileObject building)
    {
        if (building == null)
        {
            Debug.LogWarning("[GameState] Attempted to register null building");
            return;
        }

        if (!buildings.Contains(building))
        {
            buildings.Add(building);
            Debug.Log($"[GameState] Registered building: {building.Definition.Id} at {building.Origin}. Total: {buildings.Count}");
        }
        else
        {
            Debug.LogWarning($"[GameState] Building already registered: {building.Definition.Id}");
        }
    }

    /// <summary>
    /// Unregister a building when it's removed from the grid
    /// Called by GridManager before deletion
    /// </summary>
    public void UnregisterBuilding(TileObject building)
    {
        if (building == null)
        {
            Debug.LogWarning("[GameState] Attempted to unregister null building");
            return;
        }

        if (buildings.Remove(building))
        {
            Debug.Log($"[GameState] Unregistered building: {building.Definition.Id}. Remaining: {buildings.Count}");
        }
        else
        {
            Debug.LogWarning($"[GameState] Building not found for unregistration: {building.Definition.Id}");
        }
    }

    /// <summary>
    /// Clear all registered buildings
    /// Called during game reset or before loading
    /// </summary>
    public void ClearBuildings()
    {
        buildings.Clear();
        Debug.Log("[GameState] Cleared all registered buildings");
    }

    /// <summary>
    /// Get the list of all registered buildings
    /// </summary>
    public List<TileObject> GetBuildings()
    {
        return buildings;
    }

    #endregion

    #region Save/Load Data Application

    public void ApplyLoadedData(SaveState data)
    {
        money = data.money;
        happiness = data.happiness;
        TotalEmissions = data.emissions;
        maxPopulation = data.maxPopulation;
        currentScore = maxPopulation;

        Debug.Log($"[GameState] Loading save data with {data.tiles.Count} buildings");

        // Clear the buildings list before regenerating
        buildings.Clear();

        // Ensure GridManager is ready
        if (GridManager.Instance == null)
        {
            Debug.LogError("[GameState] GridManager.Instance is null during ApplyLoadedData!");
            return;
        }

        GridManager.Instance.DeleteAll();
        GridManager.Instance.GenerateGrid();

        foreach (TileSaveData tileSave in data.tiles)
        {
            if (tileSave.def == null)
            {
                Debug.LogWarning($"[GameState] Skipping tile with null definition at {tileSave.gridPosition}");
                continue;
            }
            Debug.Log($"[GameState] Loading tile: {tileSave.gridPosition} {tileSave.def.Id} occ: {tileSave.occupancy}");
            GridManager.Instance.TryForcePlace(tileSave.def, tileSave.gridPosition, tileSave.occupancy);
        }

        Debug.Log($"[GameState] Load complete. {buildings.Count} buildings registered.");
        UpdateHappinessAndDisplay();
    }

    #endregion
}