using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance { get; private set; }

    public TileObject Selected { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void Select(TileObject obj)
    {
        if (obj == null || obj == Selected)
        {
            Deselect();
            return;
        }
        Selected = obj;
        obj.Select();
        Debug.Log("Selected: " + obj.name);
    }

    public void Deselect()
    {
        if (Selected != null)
        {
            Selected.Deselect();
            Debug.Log("Deselected.");
            Selected = null;
        }
    }
}
