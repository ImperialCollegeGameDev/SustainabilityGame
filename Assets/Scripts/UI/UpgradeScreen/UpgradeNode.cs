using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UpgradeNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI NameText;

    private Upgrade upgrade;
    private Button button;
    private TileObjectDefinition def;

    public Color UnlockedColor;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    public void Init(Upgrade upgrade, TileObjectDefinition def)
    {
        this.upgrade = upgrade;
        this.def = def;
        NameText.text = upgrade.DisplayName;
    }

    private Coroutine hideRoutine;
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        UpgradeTooltip.Instance.Show(upgrade.DisplayName, upgrade.PointsCost, upgrade.MoneyCost, upgrade.Description, this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hideRoutine = StartCoroutine(HideDelayed());
    }

    private IEnumerator HideDelayed()
    {
        yield return new WaitForSeconds(0.05f);
        UpgradeTooltip.Instance.Hide(this);
    }

    private void OnClick()
    {
        TileTypeState tile = TileStateCatalog.Instance.Get(def.Id);
        if (tile.policyPoints < upgrade.PointsCost)
        {
            MusicManager.Instance?.PlayUISound(MusicManager.UISoundType.Error);
            Notifications.Instance.PostNotification("Not enough policy points!");
            return;
        }
        if (GameState.Instance.money < upgrade.MoneyCost)
        {
            MusicManager.Instance?.PlayUISound(MusicManager.UISoundType.Error);
            Notifications.Instance.PostNotification("Not enough money!");
            return;
        }
        
        MusicManager.Instance?.PlayUISound(MusicManager.UISoundType.Success);
        tile.policyPoints -= upgrade.PointsCost;
        GameState.Instance.ChangeMoney(upgrade.MoneyCost);
        tile.UnlockUpgrade(upgrade);
        UpgradeScreen.Instance.UpdateInfo();
    }

    public void UpdateInfo()
    {
        TileTypeState tile = TileStateCatalog.Instance.Get(def.Id);
        if (tile.HasUpgrade(upgrade))
        {
            button.interactable = false;
            ColorBlock colors = button.colors;
            colors.disabledColor = UnlockedColor;
            button.colors = colors;
        }
        else if (tile.CanUnlock(upgrade))
        {
            button.interactable = true;
        }
        else
        {
            button.interactable = false;
        }
    }
}
