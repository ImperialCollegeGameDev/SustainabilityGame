using UnityEngine;

public class HomeUIBridge : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    public void NewGame()
    {
        Main.Instance.StartNewGame();
    }

    public void LoadGame()
    {
        Main.Instance.LoadGame();
    }

    public void VisitLB()
    {
        Main.Instance.VisitLeaderboard();
    }

    public void QuitGame()
    {
        Main.Instance.onExitGame();
    }

    public void ViewCredits()
    {
        Main.Instance.ViewCredits();
    }

    public void OpenSettings()
    {
        Main.Instance.OpenSettings();
    }
}
