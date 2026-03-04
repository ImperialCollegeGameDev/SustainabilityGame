using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RepairAllButton : MonoBehaviour
{
    private Button button;
    private UtilityTileObject util;
    [SerializeField] private TextMeshProUGUI PriceText;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    public void Init(UtilityTileObject util)
    {
        this.util = util;
    }

    private void Update()
    {
        PriceText.text = UpdatePriceText();
    }


    string UpdatePriceText()
    {
        int totalPrice = 0;
        foreach (TileObject tileObj in GridManager.Instance.GetTileObjects())
        {
            if (tileObj is UtilityTileObject util)
                totalPrice += util.CurrentRepairCost();
        }

        return NumberFormatter.FormatMoney(totalPrice);
    }


    void OnClick()
    {
        Debug.Log("clicked");
        foreach (TileObject tileObj in GridManager.Instance.GetTileObjects())
        {
            if (tileObj is UtilityTileObject util)
                util.TryRepair();
        }
    }
}
