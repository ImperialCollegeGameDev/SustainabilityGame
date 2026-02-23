using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RepairButton : MonoBehaviour
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
        PriceText.text = NumberFormatter.FormatMoney(util.CurrentRepairCost());
    }

    void OnClick()
    {
        util.TryRepair();
    }
}
