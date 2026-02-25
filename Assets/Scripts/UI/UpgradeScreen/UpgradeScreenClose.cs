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
        Debug.Log("something 1");
        MusicManager.Instance?.PlayUISound(MusicManager.UISoundType.Close);
        UpgradeScreen.Instance.Close();
    }
}
