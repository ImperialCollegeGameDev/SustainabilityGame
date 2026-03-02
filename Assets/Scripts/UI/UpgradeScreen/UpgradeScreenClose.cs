using UnityEngine;
using UnityEngine.UI;

public class UpgradeScreenClose : MonoBehaviour
{
    private Button button;

    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        MusicManager.Instance?.PlayUISound(MusicManager.UISoundType.Click);
        UpgradeScreen.Instance.Close();
    }
}
