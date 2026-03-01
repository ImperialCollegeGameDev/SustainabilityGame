using TMPro;
using UnityEngine;

public class PopulationDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI displayText;
    [SerializeField] private float lerpSpeed = 5f; // Speed at which the number transitions
    [SerializeField] private bool useThousandsSeparator = true;

    private float currentDisplayValue = 0f;
    private int targetValue = 0;

    private void Awake()
    {
        if (displayText == null)
        {
            displayText = GetComponent<TextMeshProUGUI>();
        }

        if (displayText == null)
        {
            Debug.LogError("PopulationDisplay requires a TextMeshProUGUI component!");
        }
    }

    private void Start()
    {
        // Subscribe to GameState population changes
        if (GameState.Instance != null)
        {
            GameState.Instance.OnPopulationChanged += OnPopulationChanged;
            // Initialize with current population
            targetValue = GameState.Instance.population;
            currentDisplayValue = targetValue;
            UpdateDisplay();
        }
    }

    private void Update()
    {
        // Smoothly lerp towards target value
        if (Mathf.Abs(currentDisplayValue - targetValue) > 0.1f)
        {
            currentDisplayValue = Mathf.Lerp(currentDisplayValue, targetValue, Time.deltaTime * lerpSpeed);
            UpdateDisplay();
        }
        else if (Mathf.RoundToInt(currentDisplayValue) != targetValue)
        {
            // Snap to target if very close
            currentDisplayValue = targetValue;
            UpdateDisplay();
        }
    }

    private void OnPopulationChanged(int newPopulation)
    {
        targetValue = newPopulation;
    }

    private void UpdateDisplay()
    {
        int displayedValue = Mathf.RoundToInt(currentDisplayValue);

        if (displayText != null)
        {
            if (useThousandsSeparator)
            {
                displayText.text = displayedValue.ToString("N0"); // Formats with commas: 1,234,567
            }
            else
            {
                displayText.text = displayedValue.ToString();
            }
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (GameState.Instance != null)
        {
            GameState.Instance.OnPopulationChanged -= OnPopulationChanged;
        }
    }
}