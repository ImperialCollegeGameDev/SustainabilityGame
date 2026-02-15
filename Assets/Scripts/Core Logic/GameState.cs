using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR;

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
            Settings = new GameSettings();
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
    public Action<List<Utility>> OnUtilitiesChanged;

    public int money { get; private set; }
    public int population = 0;
    public int dissatisfiedPopulation { get; private set; } = 0;
    private int projectedHappiness = 100;
    public float happiness { get; private set; } = 100;

    public List<Utility> OwnedUtilities = new List<Utility>();

    public int TotalEnergy;
    public int TotalEmissions;

    public TileObjectDefinition buildingToBePlaced;
    public InteractionMode CurrentMode { get; private set; } = InteractionMode.None;

    void Start()
    {
        money = Settings.StartingMoney;
        RecomputeTotals();
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
    }

    public void Tick(float delta) // Delta is the time in seconds since last tick
    {
        if (!isTicking) return;

        List<TileObject> tileObjects = GridManager.Instance.GetTileObjects();
        foreach (TileObject tileObj in tileObjects)
        {
            tileObj.Tick(delta);
        }

        TaxThePoor(delta);
        RecomputeTotals();
    }

    public void FastTick(float delta) // For things that are very inexpensive to compute and we want fast feedback on
    {
        happiness += (projectedHappiness - happiness) * Settings.HappinessVolatility;
        if (Math.Abs(happiness - projectedHappiness) < 0.1f)
            happiness = projectedHappiness;

        OnHappinessChanged?.Invoke(Mathf.RoundToInt(happiness));
    }

    public void SetSelectedTile(TileObjectDefinition tile) // Called by UI building selector buttons
    {
        PostNotification($"Selected building set to {tile.Id}");
        buildingToBePlaced = tile;
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

        dissatisfiedPopulation = population - TotalEnergy / Settings.EnergyReqPerPerson;
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
        OnEnergyChanged?.Invoke(TotalEnergy);
        OnEmissionsChanged?.Invoke(TotalEmissions);
        OnUtilitiesChanged?.Invoke(new List<Utility>(OwnedUtilities));
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
        SelectionManager.Instance.Deselect();
        CurrentMode = InteractionMode.None;
        PostNotification("Interaction mode set to None");
    }

    public void SetModeSelect(bool toggleMode = false)
    {
        if (toggleMode && CurrentMode == InteractionMode.Select)
        {
            SetModeNone();
            return;
        }
        CurrentMode = InteractionMode.Select;
        PostNotification("Interaction mode set to Select");
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
        PostNotification("Interaction mode set to Place");
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
        PostNotification("Interaction mode set to Delete");
    }

    // Feel free to move this somewhere else.
    public GameObject notificationsTray;
    public GameObject notificationPrefab; // Assign in inspector also if you move.
    public int notificationLifetime = 2;
    public void PostNotification(string message)
    {
        Debug.Log($"Notification: {message}");
        GameObject notification = Instantiate(notificationPrefab, notificationsTray.transform);
        notification.GetComponent<TextMeshProUGUI>().SetText(message);
        Destroy(notification, notificationLifetime);
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
}