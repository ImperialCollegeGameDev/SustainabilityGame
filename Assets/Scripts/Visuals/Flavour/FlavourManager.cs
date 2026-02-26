using Unity.VisualScripting;
using UnityEngine;

public class FlavourManager : MonoBehaviour
{
    public static FlavourManager Instance;
    public GameObject flavourTextPrefab;
    public GameObject repairParticlePrefab;

    void Awake()
    {
        Instance = this;
    }

    public void SpawnText(
        Vector2Int gridPosition,
        string text,
        Color color,
        float fontSize = 16)
    {
        Vector3 worldPosition = GridManager.Instance.GridToWorld(gridPosition);
        SpawnText(worldPosition, text, color, fontSize);
    }

    public void SpawnText(
        Vector3 worldPosition,
        string text,
        Color color,
        float fontSize = 16)
    {
        GameObject obj = Instantiate(
            flavourTextPrefab
        );

        FlavourText floating = obj.GetComponent<FlavourText>();
        floating.Initialize(text, fontSize, color);
        floating.SetWorldPosition(worldPosition);
    }

    public void SpawnRepairParticles(Vector3 worldPosition)
    {
        Instantiate(repairParticlePrefab, worldPosition, Quaternion.Euler(-90f, 0f, 0f));
    }
}