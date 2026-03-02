using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public sealed class EndGame : MonoBehaviour
{
    public GameObject background;
    public GameObject message;
    public GameObject button;

    [Header("Animation Settings")]
    [SerializeField] private float openAnimationDuration = 0.5f;
    [SerializeField] private float closeAnimationDuration = 0.3f;
    [SerializeField] private LeanTweenType openEaseType = LeanTweenType.easeOutBack;
    [SerializeField] private LeanTweenType closeEaseType = LeanTweenType.easeInBack;
    
    private List<int> activeTweenIds = new List<int>();
    private Dictionary<GameObject, Vector3> originalScales = new Dictionary<GameObject, Vector3>();
    private bool isClosing = false;

    void Start()
    {
        PlayOpenAnimation();
    }

    void OnDestroy()
    {
        CleanupAnimations();
    }

    private void CleanupAnimations()
    {
        foreach (int tweenId in activeTweenIds)
        {
            LeanTween.cancel(tweenId);
        }
        activeTweenIds.Clear();
        originalScales.Clear();
    }

    private void PlayOpenAnimation()
    {
        // Store original scales and hide elements
        if (background != null)
        {
            originalScales[background] = background.transform.localScale;
            background.transform.localScale = Vector3.zero;
        }
        if (message != null)
        {
            originalScales[message] = message.transform.localScale;
            message.transform.localScale = Vector3.zero;
        }
        if (button != null)
        {
            originalScales[button] = button.transform.localScale;
            button.transform.localScale = Vector3.zero;
        }

        // Animate background first
        if (background != null)
        {
            Vector3 bgTargetScale = originalScales[background];
            int bgTweenId = LeanTween.scale(background, bgTargetScale, openAnimationDuration)
                .setEase(LeanTweenType.easeOutCubic)
                .setIgnoreTimeScale(true)
                .id;
            activeTweenIds.Add(bgTweenId);
        }

        // Animate message
        if (message != null)
        {
            Vector3 msgTargetScale = originalScales[message];
            int msgTweenId = LeanTween.scale(message, msgTargetScale, openAnimationDuration)
                .setEase(openEaseType)
                .setDelay(0.1f)
                .setIgnoreTimeScale(true)
                .id;
            activeTweenIds.Add(msgTweenId);
        }

        // Animate button
        if (button != null)
        {
            Vector3 btnTargetScale = originalScales[button];
            int btnTweenId = LeanTween.scale(button, btnTargetScale, openAnimationDuration)
                .setEase(openEaseType)
                .setDelay(0.2f)
                .setIgnoreTimeScale(true)
                .id;
            activeTweenIds.Add(btnTweenId);
        }
    }

    private void PlayCloseAnimation()
    {
        if (isClosing) return;
        isClosing = true;

        CleanupAnimations(); // Cancel any ongoing animations

        // Animate elements to scale zero in reverse order
        if (button != null)
        {
            int btnTweenId = LeanTween.scale(button, Vector3.zero, closeAnimationDuration)
                .setEase(closeEaseType)
                .setIgnoreTimeScale(true)
                .id;
            activeTweenIds.Add(btnTweenId);
        }

        if (message != null)
        {
            int msgTweenId = LeanTween.scale(message, Vector3.zero, closeAnimationDuration)
                .setEase(closeEaseType)
                .setDelay(0.05f)
                .setIgnoreTimeScale(true)
                .id;
            activeTweenIds.Add(msgTweenId);
        }

        if (background != null)
        {
            int bgTweenId = LeanTween.scale(background, Vector3.zero, closeAnimationDuration)
                .setEase(LeanTweenType.easeInCubic)
                .setDelay(0.1f)
                .setIgnoreTimeScale(true)
                .setOnComplete(OnCloseAnimationComplete)
                .id;
            activeTweenIds.Add(bgTweenId);
        }
        else
        {
            // If no background, just complete immediately
            OnCloseAnimationComplete();
        }
    }

    private void OnCloseAnimationComplete()
    {
        Main.Instance.SaveGame();
        Main.Instance.ReturnHome();
    }

    public void OnSaveAndExit()
    {
        PlayCloseAnimation();
    }
}