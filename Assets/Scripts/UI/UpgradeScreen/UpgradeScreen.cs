using TMPro;
using UnityEngine;

public class UpgradeScreen : MonoBehaviour
{
    public static UpgradeScreen Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        GetComponent<Canvas>().enabled = false;
    }

    public GameObject PathContainer;
    public UpgradePathUI UpgradePathPrefab;
    public TextMeshProUGUI NameText;

    public void Open(TileObject tileObject)
    {
        Debug.Log("Open called!");
        if (tileObject == null)
        {
            Debug.LogError("UpgradeScreen.Open called with null tileObject.");
            return;
        }
        GetComponent<Canvas>().enabled = true;
        // Clear existing paths
        foreach (Transform child in PathContainer.transform)
        {
            Destroy(child.gameObject);
        }
        // Add new paths
        foreach (UpgradePath path in tileObject.Definition.UpgradeTree.Paths)
        {
            UpgradePathUI upgradePath = Instantiate(UpgradePathPrefab, PathContainer.transform, false);
            upgradePath.Init(path);
        }
        NameText.text = tileObject.Definition.DisplayName;
    }

    public void Close()
    {
        GetComponent<Canvas>().enabled = false;
    }
}
