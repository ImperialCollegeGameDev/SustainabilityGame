using UnityEngine;
using UnityEngine.UI;

public class UpgradesButton : MonoBehaviour
{
    public TileObject tileObject;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    public void OnClick()
    {
        if (tileObject == null)
        {
            Debug.LogWarning("UpgradesButton clicked but tileObject is null.");
            return;
        }
        if (UpgradeScreen.Instance == null)
        {
            Debug.LogError("UpgradeScreen instance is not set.");
            return;
        }
        UpgradeScreen.Instance.Open(tileObject);
    }
}
