using System;
using System.Collections.Generic;
using UnityEditor.Overlays;
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

        if (Settings == null)
        {
            Debug.LogWarning("GameSettings not assigned in GameState. Using default settings.");
            Settings = ScriptableObject.CreateInstance<GameSettings>();
        }
    }

    public GameSettings Settings;

    private const float TickInterval = 1f;
    private const float FastTickInterval = 0.2f;
    private float Timescale = 1f;
    private bool isTicking = true;
    private float _timer = 0f;
    private float _fastTimer = 0f;

    // UI callbacks (UI scripts can subscribe to these)
    public Action<int> OnMoneyChanged;
    public Action<int> OnEnergyChanged;
    public Action<int> OnEmissionsChanged;
    public Action<int> OnPopulationChanged;
    public Action<int> OnHappinessChanged;

    public int money { get; private set; }
    public int population = 0;
    public int dissatisfiedPopulation { get; private set; } = 0;
    private int projectedHappiness = 100;
    public float happiness { get; private set; } = 100;

    public int Power = 0;
    public int TotalEmissions = 0;

    public int requiredEnergy { get; private set; } = 0;

    public TileObjectDefinition buildingToBePlaced;
    public InteractionMode CurrentMode { get; private set; } = InteractionMode.None;

    // keeps track of unlocked skills and corresponding buildings
    HashSet<string> unlockedBuildings = new HashSet<string>();
    public bool IsBuildingUnlocked(string id) => unlockedBuildings.Contains(id);
    public event System.Action OnBuildingUnlocksChanged;


    void Start()
    {
        money = Settings.StartingMoney;
        UpdateHappinessAndDisplay();
    }

    private void Update()
    {
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

        //print(buildingToBePlaced);
    }

    public void Tick(float delta) // Delta is the time in seconds since last tick
    {
        if (!isTicking) return;

        population = 0;
        Power = 0;

        List<TileObject> tileObjects = GridManager.Instance.GetTileObjects();
        foreach (TileObject tileObj in tileObjects)
        {
            tileObj.Tick(delta);
            if (tileObj is ResidentialTileObject res)
            {
                population += Mathf.FloorToInt(res.occupancy);
            }
        }

        TotalEmissions = Math.Max(TotalEmissions, 0);

        TaxThePoor(delta);
        UpdateHappinessAndDisplay();
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
        requiredEnergy = population * Settings.EnergyReqPerPerson;

        dissatisfiedPopulation = population - Power / Settings.EnergyReqPerPerson;
        dissatisfiedPopulation = Math.Max(dissatisfiedPopulation, 0);

        projectedHappiness = Mathf.RoundToInt(100f * (1f - TotalEmissions / (float) Settings.MaxEmission));
        projectedHappiness = Math.Max(projectedHappiness, 0);

        if (population > 0)
            projectedHappiness = Mathf.FloorToInt(projectedHappiness * (1 - Settings.DissatisfactionDanger * dissatisfiedPopulation / (float) population));

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
        OnEmissionsChanged?.Invoke(TotalEmissions);
        OnPopulationChanged?.Invoke(population);
    }

    public void ChangeMoney(int amount)
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
        Notifications.Instance.PostNotification("Interaction mode set to None");
    }

    public void SetModeSelect() //bool toggleMode = false)
    {
        //if (toggleMode && CurrentMode == InteractionMode.Select)
        //{
        //    SetModeNone();
        //    return;
        //}
        CurrentMode = InteractionMode.Select;
        GridMouse.Instance.ClearPlacementHighlight();
        Notifications.Instance.PostNotification("Interaction mode set to Select");
    }

    public void SetModePlace() //bool toggleMode = false)
    {
        SelectionManager.Instance.Deselect();
        //if (toggleMode && CurrentMode == InteractionMode.Place)
        //{
        //    SetModeNone();
        //    return;
        //}
        CurrentMode = InteractionMode.Place;
        Notifications.Instance.PostNotification("Interaction mode set to Place");
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
        Notifications.Instance.PostNotification("Interaction mode set to Delete");
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

    public void LoadGame()
    {
        SaveState data = SaveManager.Load();
        if (data == null) return;

        money = data.money;
        happiness = data.happiness;
        TotalEmissions = data.emissions;

        GridManager.Instance.DeleteAll();
        GridManager.Instance.GenerateGrid();

        foreach (TileSaveData tileSave in data.tiles)
        {
            GridManager.Instance.TryForcePlace(tileSave.def, tileSave.gridPosition, tileSave.occupancy);
        }
    }

    public void SaveGame()
    {
        SaveManager.Save();
    }
}