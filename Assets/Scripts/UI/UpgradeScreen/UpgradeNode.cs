using TMPro;
using UnityEngine;

public class UpgradeNode : MonoBehaviour
{
    public TextMeshProUGUI NameText;

    public void Init(Upgrade upgrade)
    {
        NameText.text = upgrade.DisplayName;
    }
}
