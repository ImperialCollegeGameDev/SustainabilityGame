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
    private TileObject tileObj;

    public Color UnlockedColor;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    public void Init(Upgrade upgrade, TileObject tileObj)
    {
        this.upgrade = upgrade;
        this.tileObj = tileObj;
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
        if (tileObj.policyPoints < upgrade.PointsCost)
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
        tileObj.policyPoints -= upgrade.PointsCost;
        GameState.Instance.ChangeMoney(upgrade.MoneyCost);
        tileObj.UnlockUpgrade(upgrade);
        UpgradeScreen.Instance.UpdateInfo();
    }

    public void UpdateInfo()
    {
        if (tileObj.HasUpgrade(upgrade))
        {
            button.interactable = false;
            ColorBlock colors = button.colors;
            colors.disabledColor = UnlockedColor;
            button.colors = colors;
        }
        else if (tileObj.CanUnlock(upgrade))
        {
            button.interactable = true;
        }
        else
        {
            button.interactable = false;
        }
    }
}
