using UnityEngine;
using UnityEngine.UI;

public class BuildingSelector : MonoBehaviour
{
    public TileObjectDefinition tile;
    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        GameState.Instance.SetSelectedTile(tile);
    }
}
