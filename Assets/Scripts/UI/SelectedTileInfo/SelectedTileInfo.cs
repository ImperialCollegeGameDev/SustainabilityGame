using UnityEngine;

public class SelectedTileInfo : MonoBehaviour
{
    public static SelectedTileInfo Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}
