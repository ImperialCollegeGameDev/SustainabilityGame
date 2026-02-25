using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BuildingSelector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TileObjectDefinition tile;
    [SerializeField] private BuildingSelectorTooltip tooltip;
    private Button button;

    void Awake()
    {
        if (tile != null)
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(OnClick);
        }
    }

    void OnClick()
    {
        GameState.Instance.SetSelectedTile(tile);
        GameState.Instance.SetModePlace();
    }

    private Coroutine hideRoutine;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        tooltip.Show(tile.DisplayName, tile.Cost, this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hideRoutine = StartCoroutine(HideDelayed());
    }

    private IEnumerator HideDelayed()
    {
        yield return new WaitForSeconds(0.05f);
        tooltip.Hide(this);
    }
}
