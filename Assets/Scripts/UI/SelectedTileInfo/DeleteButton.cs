using UnityEngine;
using UnityEngine.UI;

public class DeleteButton : MonoBehaviour
{
    private Button button;
    private TileObject tileObj;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    public void Init(TileObject tileObj)
    {
        this.tileObj = tileObj;
    }

    void OnClick()
    {
        GridManager.Instance.Delete(tileObj);
    }
}
