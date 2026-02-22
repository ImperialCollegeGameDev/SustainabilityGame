using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ProgressStat : MonoBehaviour
{
    [SerializeField] private Image img;
    [SerializeField] private StatType stat = StatType.Emissions;

    public float speed = 0.7f;

    void Start()
    {
        if (img == null)
        {
            Debug.LogWarning("ProgressStat: Image is null.");
            return;
        }

        if (GameState.Instance == null)
        {
            Debug.LogWarning("DisplayStat: GameState.Instance is null. Make sure GameState exists in the scene.");
            return;
        }
    }

    private void Update()
    {
        if (img == null || GameState.Instance == null)
        {
            return;
        }

        float final = 0;
        switch (stat)
        {
            case StatType.Emissions:
                final = GameState.Instance.TotalEmissions / (float) GameState.Instance.Settings.MaxEmission;
                break;
            case StatType.Happiness:
                final = GameState.Instance.happiness / 100;
                break;
            case StatType.Energy:
                if (GameState.Instance.requiredEnergy < 1) final = 1;
                else final = GameState.Instance.Power / (float) GameState.Instance.requiredEnergy;
                break;
            default:
                Debug.LogWarning($"ProgressStat: Unsupported stat type {stat}. Defaulting to 0.");
                break;
        }
        final = Mathf.Clamp01(final);

        float current   = img.fillAmount;

        img.fillAmount = Mathf.Lerp(
            img.fillAmount,
            final,
            1f - Mathf.Exp(-speed * Time.deltaTime)
        );

        img.fillAmount = Mathf.Clamp01(img.fillAmount);
    }
}
