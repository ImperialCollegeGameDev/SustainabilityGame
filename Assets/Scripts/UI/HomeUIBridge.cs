using UnityEngine;

public class HomeUIBridge : MonoBehaviour
{
    // How long (seconds) to ignore button presses after the app regains focus.
    // This prevents the refocus-click from simultaneously firing a network action.
    private const float FocusIgnoreWindow = 0.2f;
    private float _focusRegainedTime = -1f;
    private bool _isProcessing = false;

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            _focusRegainedTime = Time.unscaledTime;
    }

    private bool ShouldBlock()
    {
        if (_isProcessing) return true;
        if (_focusRegainedTime >= 0f && (Time.unscaledTime - _focusRegainedTime) < FocusIgnoreWindow) return true;
        return false;
    }

    public void NewGame()
    {
        if (ShouldBlock()) return;
        _isProcessing = true;
        Main.Instance.StartNewGame();
    }

    public void LoadGame()
    {
        if (ShouldBlock()) return;
        _isProcessing = true;
        Main.Instance.LoadGame();
    }

    public void VisitLB()
    {
        if (ShouldBlock()) return;
        _isProcessing = true;
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
