using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject buildingSelector;

    private void Update()
    {
        bool placementActive = GameState.Instance.CurrentMode == GameState.InteractionMode.Place;

        if (buildingSelector.activeSelf != placementActive)
            buildingSelector.SetActive(placementActive);
    }
}
