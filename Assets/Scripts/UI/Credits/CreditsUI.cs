using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CreditsUI : MonoBehaviour
{
    public GameObject spawnparent;

    [Header("Animation Settings")]
    [SerializeField] private float entryDelay = 0.15f; // Delay between each entry
    [SerializeField] private float slideDistance = 50f; // How far to slide up from
    [SerializeField] private float animationDuration = 0.6f;
    [SerializeField] private LeanTweenType easeType = LeanTweenType.easeOutCubic;

    [Header("Icon Pulse Settings")]
    [SerializeField] private float iconPulseScale = 1.25f; // How much to scale up
    [SerializeField] private float iconPulseDuration = 1.1f; // Duration of one pulse cycle
    [SerializeField] private float iconPulseDelay = 0.3f; // Stagger between icon pulses
    
    [Header("Link Pulse Settings")]
    [SerializeField] private float linkPulseScale = 1.05f; // Subtle pulse for links
    [SerializeField] private float linkPulseDuration = 1.5f;

    private List<int> activeTweenIds = new List<int>();
    private Dictionary<GameObject, Vector3> originalScales = new Dictionary<GameObject, Vector3>();

    void Start()
    {
        HideAllEntries();
        StartCoroutine(AnimateCreditEntries());
    }

    private void HideAllEntries()
    {
        if (spawnparent == null) return;

        int childCount = spawnparent.transform.childCount;
        // Start from index 1 to skip the padding (first child)
        for (int i = 1; i < childCount; i++)
        {
            Transform entry = spawnparent.transform.GetChild(i);
            cred_entry credEntry = entry.GetComponent<cred_entry>();
            
            if (credEntry != null)
            {
                // Store original scales and hide each component by scaling to 0
                if (credEntry.icon != null)
                {
                    originalScales[credEntry.icon] = credEntry.icon.transform.localScale;
                    credEntry.icon.transform.localScale = Vector3.zero;
                }
                if (credEntry.nameObj != null)
                {
                    originalScales[credEntry.nameObj] = credEntry.nameObj.transform.localScale;
                    credEntry.nameObj.transform.localScale = Vector3.zero;
                }
                if (credEntry.link1 != null)
                {
                    originalScales[credEntry.link1] = credEntry.link1.transform.localScale;
                    credEntry.link1.transform.localScale = Vector3.zero;
                }
                if (credEntry.link2 != null)
                {
                    originalScales[credEntry.link2] = credEntry.link2.transform.localScale;
                    credEntry.link2.transform.localScale = Vector3.zero;
                }
            }
            else
            {
                // Hide entire entry if it's not a cred_entry
                originalScales[entry.gameObject] = entry.gameObject.transform.localScale;
                entry.gameObject.transform.localScale = Vector3.zero;
            }
        }
    }

    void OnDestroy()
    {
        CleanupAllAnimations();
    }

    private void CleanupAllAnimations()
    {
        foreach (int tweenId in activeTweenIds)
        {
            LeanTween.cancel(tweenId);
        }
        activeTweenIds.Clear();
        originalScales.Clear();
    }

    private IEnumerator AnimateCreditEntries()
    {
        if (spawnparent == null)
        {
            Debug.LogWarning("[CreditsUI] spawnparent is not assigned!");
            yield break;
        }

        int childCount = spawnparent.transform.childCount;
        if (childCount <= 1) yield break; // Only padding, nothing to animate

        yield return new WaitForSeconds(2f); // Initial delay before starting animations


        // Start from index 1 to skip the padding (first child)
        for (int i = 1; i < childCount; i++)
        {
            Transform entry = spawnparent.transform.GetChild(i);
            AnimateEntry(entry.gameObject, i - 1); // i-1 so first entry has delay 0
        }
    }

    private void AnimateEntry(GameObject entry, int index)
    {
        cred_entry credEntry = entry.GetComponent<cred_entry>();
        if (credEntry == null)
        {
            // Fallback: animate the entire entry if it's not a cred_entry
            AnimateSingleObject(entry, index * entryDelay, false, false);
            return;
        }

        // Calculate base delay for this entry
        float baseDelay = index * entryDelay;
        float subDelay = 0.08f; // Delay between parts within an entry

        // Animate each part of the credit entry with sub-staggered timing
        int partIndex = 0;
        if (credEntry.icon != null)
        {
            // Icons get special pulsing animation
            AnimateSingleObject(credEntry.icon, baseDelay + (partIndex * subDelay), true, false);
            StartIconPulse(credEntry.icon, index);
            partIndex++;
        }
        if (credEntry.nameObj != null)
        {
            AnimateSingleObject(credEntry.nameObj, baseDelay + (partIndex * subDelay), false, false);
            partIndex++;
        }
        if (credEntry.link1 != null && credEntry.link1.activeSelf)
        {
            // Links get animated and subtle pulse
            AnimateLinkEntry(credEntry.link1, baseDelay + (partIndex * subDelay), index);
            partIndex++;
        }
        if (credEntry.link2 != null && credEntry.link2.activeSelf)
        {
            // Links get animated and subtle pulse
            AnimateLinkEntry(credEntry.link2, baseDelay + (partIndex * subDelay), index);
            partIndex++;
        }
    }

    private void StartIconPulse(GameObject icon, int index)
    {
        // Wait until the initial animation completes, then start pulsing
        float initialDelay = animationDuration + (index * iconPulseDelay);
        
        // Get the target scale for pulsing
        Vector3 targetScale = originalScales.ContainsKey(icon) ? originalScales[icon] : Vector3.one;
        
        // Continuous pulse animation
        int tweenId = LeanTween.scale(icon, targetScale * iconPulseScale, iconPulseDuration)
            .setEase(LeanTweenType.easeInOutSine)
            .setLoopPingPong()
            .setDelay(initialDelay)
            .setIgnoreTimeScale(true)
            .id;
        
        activeTweenIds.Add(tweenId);
    }

    private void StartLinkPulse(GameObject link, int index)
    {
        // Wait until the initial animation completes, then start pulsing
        float initialDelay = animationDuration + (index * iconPulseDelay);
        
        // Get the target scale for pulsing
        Vector3 targetScale = originalScales.ContainsKey(link) ? originalScales[link] : Vector3.one;
        
        // Subtle pulse animation for links
        int tweenId = LeanTween.scale(link, targetScale * linkPulseScale, linkPulseDuration)
            .setEase(LeanTweenType.easeInOutSine)
            .setLoopPingPong()
            .setDelay(initialDelay)
            .setIgnoreTimeScale(true)
            .id;
        
        activeTweenIds.Add(tweenId);
    }

    private void AnimateLinkEntry(GameObject link, float delay, int index)
    {
        // Get the original scale to animate to
        Vector3 targetScale = originalScales.ContainsKey(link) ? originalScales[link] : Vector3.one;
        
        // Animate scale from 0 to original scale
        int scaleId = LeanTween.scale(link, targetScale, animationDuration)
            .setEase(LeanTweenType.easeOutCubic)
            .setDelay(delay)
            .setIgnoreTimeScale(true)
            .id;
        activeTweenIds.Add(scaleId);

        // Start pulse after animation completes
        StartLinkPulse(link, index);
    }

    private void AnimateSingleObject(GameObject obj, float delay, bool isIcon, bool isLink)
    {
        // Get the original scale to animate to
        Vector3 targetScale = originalScales.ContainsKey(obj) ? originalScales[obj] : Vector3.one;
        
        if (isIcon)
        {
            // Icons pop in with bounce - scale to original size
            int scaleId = LeanTween.scale(obj, targetScale, animationDuration)
                .setEase(LeanTweenType.easeOutBack)
                .setDelay(delay)
                .setIgnoreTimeScale(true)
                .id;
            activeTweenIds.Add(scaleId);
        }
        else
        {
            // Standard animation for text - scale to original size
            int scaleId = LeanTween.scale(obj, targetScale, animationDuration)
                .setEase(easeType)
                .setDelay(delay)
                .setIgnoreTimeScale(true)
                .id;
            activeTweenIds.Add(scaleId);
        }
    }

    public void returnhome()
    {
        Debug.Log("[CreditsUI] Attempting to return home...");
        CleanupAllAnimations();
        SceneTransition.i.SendToScene("Home");
    }

    void Update()
    {
        
    }
}
