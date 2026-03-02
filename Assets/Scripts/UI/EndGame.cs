using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class EndGame : MonoBehaviour
{
    [SerializeField] private Button saveAndExitButton;

    private void Awake()
    {
        // Ensure the button is wired even if not set in the Inspector.
        if (saveAndExitButton != null)
        {
            saveAndExitButton.onClick.RemoveListener(OnSaveAndExit);
            saveAndExitButton.onClick.AddListener(OnSaveAndExit);
        }

        // No animations: ensure it's simply visible.
        gameObject.SetActive(false);
    }

    public void SetVisible()
    {
        if (GameState.Instance.happiness < 0.5f)
            gameObject.SetActive(true);
    }

    public void OnSaveAndExit()
    {
        Main.Instance.SaveGame();
        Main.Instance.ReturnHome();
    }
}