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

        string pointsCostText = $"{upgrade.PointsCost} Policy Points";
        if (TileStateCatalog.Instance.Get(tileObj.Definition.Id) is TileTypeState state && state.HasUpgradeUnlocked(upgrade))
        {
            pointsCostText = "Already Unlocked";
        }

        UpgradeTooltip.Instance.Show(upgrade.DisplayName, pointsCostText, upgrade.MoneyCost, upgrade.Description, this);
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
        TileTypeState state = TileStateCatalog.Instance.Get(tileObj.Definition.Id);
        if (state == null)
        {
            Debug.LogError($"TileTypeState not found for {tileObj.Definition.Id}");
            return;
        }

        // Check if this specific building instance already has the upgrade
        if (tileObj.HasUpgrade(upgrade))
        {
            Notifications.Instance.PostNotification("This building already has this upgrade!");
            return;
        }

        // Check if this is the first time unlocking for this building type
        bool isFirstTimeForType = !state.HasUpgradeUnlocked(upgrade);

        // Calculate costs
        int pointsCost = isFirstTimeForType ? upgrade.PointsCost : 0;
        long moneyCost = upgrade.MoneyCost;

        // Check if we have enough policy points (only if first time for type)
        if (isFirstTimeForType && state.policyPoints < pointsCost)
        {
            Notifications.Instance.PostNotification("Not enough policy points!");
            return;
        }

        // Check if we have enough money
        if (GameState.Instance.money < moneyCost)
        {
            Notifications.Instance.PostNotification("Not enough money!");
            return;
        }

        // Deduct policy points only if it's the first time for this building type
        if (isFirstTimeForType)
        {
            state.policyPoints -= pointsCost;
            state.UnlockUpgrade(upgrade);
            Notifications.Instance.PostNotification($"Unlocked {upgrade.DisplayName} for all {tileObj.Definition.DisplayName}s!");
        }
        else
        {
            Notifications.Instance.PostNotification($"Applied {upgrade.DisplayName} to this {tileObj.Definition.DisplayName}!");
        }

        // Always deduct money
        GameState.Instance.ChangeMoney(-moneyCost);

        // Unlock for this specific building instance
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
