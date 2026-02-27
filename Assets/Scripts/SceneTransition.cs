using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// <c>SceneTransition</c> is a singleton class that handles the transition between scenes, including the cloud transition animation.
/// </summary>
public class SceneTransition : MonoBehaviour
{
    public static SceneTransition i { get; private set; }

    [SerializeField]
    public List<Sprite> leafAssets;

    [Header("Leaf Animation Settings")]
    [SerializeField] private int leafCount = 180;
    [SerializeField] private float leafFallTimeMin = 2.0f;
    [SerializeField] private float leafFallTimeMax = 3.0f;
    [SerializeField] private float leafRotationSpeedMin = 50f;
    [SerializeField] private float leafRotationSpeedMax = 200f;
    [SerializeField] private float leafHorizontalDriftMin = -50f;
    [SerializeField] private float leafHorizontalDriftMax = 50f;
    [SerializeField] private Vector2 leafScaleRange = new Vector2(1.5f, 2.2f);

    [Header("LeanTween Settings")]
    [SerializeField] private int leanTweenCapacity = 500;

    private List<GameObject> activeLeaves = new List<GameObject>();
    private Canvas mainCanvas;

    void Awake()
    {
        //Debug.Log($"[SceneTransition] Initializing LeanTween with capacity: {leanTweenCapacity}");
        LeanTween.init(leanTweenCapacity);
    }

    void Start()
    {
        //Debug.Log("[SceneTransition] Start() called - Initializing SceneTransition");
        
        if (i == null)
        {
            i = this;
            //Debug.Log("[SceneTransition] Singleton instance set");
        }
        else
        {
            //Debug.LogWarning("[SceneTransition] Duplicate SceneTransition instance destroyed");
            Destroy(gameObject);
            return;
        }

        SceneManager.sceneLoaded += (scene, mode) => OnNewScene();
        
        SetupCanvas();
    }

    private void SetupCanvas()
    {
        //Debug.Log("[SceneTransition] SetupCanvas() called");
        
        mainCanvas = GetComponent<Canvas>();
        
        if (mainCanvas == null)
        {
            //Debug.LogError("[SceneTransition] No Canvas component found on SceneTransition GameObject! Leaf animation will not work.");
            return;
        }
        
        //Debug.Log($"[SceneTransition] Canvas found - RenderMode: {mainCanvas.renderMode}, SortingOrder: {mainCanvas.sortingOrder}");
        
        if (mainCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            //Debug.LogWarning($"[SceneTransition] Canvas RenderMode is {mainCanvas.renderMode}, consider using ScreenSpaceOverlay for leaf animations");
        }
    }

    public void PlayAnimation()
    {
        //Debug.Log("[SceneTransition] PlayAnimation() called - Starting same-scene animation");
        StartCoroutine(AnimationSameScene());
    }

    IEnumerator AnimationSameScene()
    {
        //Debug.Log("[SceneTransition] AnimationSameScene coroutine started");
        GetComponentInChildren<Animator>().SetTrigger("Exit");
        yield return new WaitForSeconds(1.25f);
        GetComponentInChildren<Animator>().SetTrigger("Enter");
        //Debug.Log("[SceneTransition] AnimationSameScene coroutine completed");
    }

    void OnNewScene()
    {
        //Debug.Log("[SceneTransition] OnNewScene() called - Scene loaded event triggered");
        StartCoroutine(WaitForNewScene());
    }

    IEnumerator WaitForNewScene()
    {
        //Debug.Log("[SceneTransition] WaitForNewScene coroutine started");
        yield return new WaitForEndOfFrame();
        
        if (mainCanvas != null)
        {
            mainCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            mainCanvas.planeDistance = 100;
            mainCanvas.worldCamera = Camera.main;
            mainCanvas.sortingOrder = 12000;
            //Debug.Log("[SceneTransition] Canvas updated for new scene - Camera assigned and sorting updated");
        }
        
        ////Debug.Log("[SceneTransition] WaitForNewScene coroutine completed");
    }

    // Final optimized scene loading with proper leaf animation
    public void SendToScene(string sceneName)
    {
        ////Debug.Log($"[SceneTransition] SendToScene() called with sceneName: {sceneName}");
        
        if (sceneName == "Main")
        {
            //Debug.Log("[SceneTransition] Loading Main scene with natural leaf animation");
            StartCoroutine(LoadMainSceneWithNaturalLeaves());
        }
        else
        {
            ////Debug.Log($"[SceneTransition] Loading scene '{sceneName}' with natural leaf animation");
            StartCoroutine(LoadSceneWithNaturalLeaves(sceneName));
        }
    }

    // Final implementation - scene loads in background, leaves fall naturally
    IEnumerator LoadSceneWithNaturalLeaves(string sceneName)
    {
        ////Debug.Log($"[SceneTransition] LoadSceneWithNaturalLeaves coroutine started for scene: {sceneName}");

        // Start leaf animation immediately
        StartLeafAnimationOptimized();
        
        // Start scene loading immediately in parallel - true background loading
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        ////Debug.Log($"[SceneTransition] Scene '{sceneName}' loading started in background immediately");
        
        // Wait for both conditions: all leaves naturally completed AND scene is loaded
        bool sceneDone = false;
        bool leavesDone = false;
        
        while (!sceneDone || !leavesDone)
        {
            // Check scene loading progress
            if (!sceneDone && asyncLoad.isDone)
            {
                sceneDone = true;
                //Debug.Log($"[SceneTransition] Scene '{sceneName}' loaded successfully");
            }
            
            // Check if leaves are naturally completed
            if (!leavesDone && activeLeaves.Count == 0)
            {
                leavesDone = true;
                //Debug.Log("[SceneTransition] All leaves completed naturally");
            }
            
            yield return null;
        }
        
        //Debug.Log("[SceneTransition] LoadSceneWithNaturalLeaves coroutine completed - both scene and leaves finished");
    }

    // Final main scene loading with natural leaf completion
    IEnumerator LoadMainSceneWithNaturalLeaves()
    {
        //Debug.Log("[SceneTransition] LoadMainSceneWithNaturalLeaves coroutine started");

        // Start leaf animation immediately
        StartLeafAnimationOptimized();
        

        // let leaves play for a tiny bit
        yield return new WaitForSeconds(0.5f);

        // Start scene loading immediately in parallel
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Main");
        
        // Wait for both conditions: all leaves naturally completed AND scene is loaded
        bool sceneDone = false;
        bool leavesDone = false;
        
        while (!sceneDone || !leavesDone)
        {
            // Check scene loading progress
            if (!sceneDone && asyncLoad.isDone)
            {
                sceneDone = true;
                //Debug.Log("[SceneTransition] Main scene loaded successfully");
            }
            
            // Check if leaves are naturally completed
            if (!leavesDone && activeLeaves.Count == 0)
            {
                leavesDone = true;
                //Debug.Log("[SceneTransition] All leaves completed naturally");
            }
            
            yield return null;
        }        
    }

    // Original methods for backward compatibility
    IEnumerator LoadScene(string sceneName)
    {
        //Debug.Log($"[SceneTransition] LoadScene (no animation) coroutine started for: {sceneName}");
        GetComponentInChildren<Animator>().SetTrigger("Exit");
        yield return new WaitForSeconds(1f);
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        //Debug.Log($"[SceneTransition] LoadScene (no animation) completed for: {sceneName}");
    }

    IEnumerator LoadGameScene()
    {
        //Debug.Log("[SceneTransition] LoadGameScene (no animation) coroutine started");
        GetComponentInChildren<Animator>().SetTrigger("Exit");
        yield return new WaitForSeconds(1.5f);
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Main");
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        //Debug.Log("[SceneTransition] LoadGameScene (no animation) completed");
    }

    #region Leaf Asset Management

    /// <summary>
    /// Returns a random leaf sprite from the leafAssets list
    /// </summary>
    /// <returns>Random leaf sprite, or null if no assets available</returns>
    public Sprite GetRandomLeaf()
    {
        if (leafAssets == null || leafAssets.Count == 0)
        {
            //Debug.LogWarning("[SceneTransition] No leaf assets available for random selection!");
            return null;
        }
        
        return leafAssets[Random.Range(0, leafAssets.Count)];
    }

    #endregion

    // Optimized leaf animation with instant spawning
    private void StartLeafAnimationOptimized()
    {
        //Debug.Log("[SceneTransition] StartLeafAnimationOptimized() called");
        
        if (mainCanvas == null)
        {
            //Debug.LogError("[SceneTransition] Cannot start leaf animation - mainCanvas is null!");
            return;
        }
        
        if (leafAssets == null || leafAssets.Count == 0)
        {
            //Debug.LogWarning("[SceneTransition] No leaf assets assigned for scene transition!");
            return;
        }

        ////Debug.Log($"[SceneTransition] Starting optimized leaf animation with {leafAssets.Count} leaf sprites available");

        // Clear any existing leaves
        CleanupLeaves();

        // Get screen dimensions
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        
        ////Debug.Log($"[SceneTransition] Screen dimensions: {screenWidth}x{screenHeight}");
        ////Debug.Log($"[SceneTransition] Spawning {leafCount} leaves instantly");

        // Pre-calculate all leaf positions for instant spawning
        List<Vector2> leafPositions = new List<Vector2>();
        for (int i = 0; i < leafCount; i++)
        {
            float startX = Random.Range(-50f, screenWidth + 50f);
            float startY = screenHeight + Random.Range(20f, 50f);
            leafPositions.Add(new Vector2(startX, startY));
        }

        // Spawn all leaves instantly with no delays
        for (int i = 0; i < leafCount; i++)
        {
            SpawnLeafOptimized(screenWidth, screenHeight, leafPositions[i]);
        }
        
        //Debug.Log("[SceneTransition] All leaves spawned and started falling instantly");
    }

    // Optimized leaf spawning with no delays
    private void SpawnLeafOptimized(float screenWidth, float screenHeight, Vector2 position)
    {
        if (mainCanvas == null)
        {
            //Debug.LogError("[SceneTransition] Cannot spawn leaf - mainCanvas is null!");
            return;
        }
        
        // Create leaf GameObject
        GameObject leafObj = new GameObject("Leaf");
        leafObj.transform.SetParent(mainCanvas.transform, false);

        // Add UI Image component
        UnityEngine.UI.Image leafImage = leafObj.AddComponent<UnityEngine.UI.Image>();

        // Assign random leaf sprite using the new method
        Sprite selectedLeaf = GetRandomLeaf();
        
        leafImage.sprite = selectedLeaf;
        leafImage.preserveAspect = true;

        // Set random scale
        float scale = Random.Range(leafScaleRange.x, leafScaleRange.y);
        leafObj.transform.localScale = Vector3.one * scale;

        // Position the leaf
        RectTransform rectTransform = leafObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;

        // Add to active leaves list
        activeLeaves.Add(leafObj);
        
        // Start animation immediately - no delay
        AnimateLeafOptimized(leafObj, position.x, -500f);
    }

    // Optimized leaf animation - leaves control their own lifespan
    private void AnimateLeafOptimized(GameObject leafObj, float startX, float endY)
    {
        if (leafObj == null)
        {
            //Debug.LogWarning("[SceneTransition] Cannot animate leaf - leafObj is null!");
            return;
        }

        RectTransform rectTransform = leafObj.GetComponent<RectTransform>();

        // Random fall time - this determines natural lifespan
        float fallTime = Random.Range(leafFallTimeMin, leafFallTimeMax);

        // Random horizontal drift
        float horizontalDrift = Random.Range(leafHorizontalDriftMin, leafHorizontalDriftMax);
        float endX = startX + horizontalDrift;

        // Random rotation speed and direction
        float rotationSpeed = Random.Range(leafRotationSpeedMin, leafRotationSpeedMax);
        if (Random.value > 0.5f) rotationSpeed *= -1;

        try
        {
            // Animate position with easing for natural fall - leaf destroys itself when done
            LeanTween.move(rectTransform, new Vector3(endX, endY, 0), fallTime)
                .setEase(LeanTweenType.easeInQuad)
                .setOnComplete(() =>
                {
                    if (leafObj != null)
                    {
                        activeLeaves.Remove(leafObj);
                        Destroy(leafObj);
                        
                        // Log when all leaves are naturally completed
                        if (activeLeaves.Count == 0)
                        {
                            //Debug.Log("[SceneTransition] All leaves completed their natural fall");
                        }
                    }
                });

            // Animate rotation continuously
            LeanTween.rotateAround(leafObj, Vector3.forward, rotationSpeed * fallTime, fallTime)
                .setEase(LeanTweenType.linear);

            // Add subtle scale animation for flutter effect
            float scaleVariation = Random.Range(0.97f, 1.03f);
            LeanTween.scale(leafObj, leafObj.transform.localScale * scaleVariation, fallTime * 0.5f)
                .setLoopPingPong()
                .setEase(LeanTweenType.easeInOutSine);
                
        }
        catch
        {
            //Debug.LogError($"[SceneTransition] Failed to create leaf animations: {e.Message}");
            // Clean up the leaf object if animation failed
            if (leafObj != null)
            {
                activeLeaves.Remove(leafObj);
                Destroy(leafObj);
            }
        }
    }

    // Only force cleanup when absolutely necessary (like OnDestroy)
    private void CleanupLeaves()
    {
        //Debug.Log($"[SceneTransition] CleanupLeaves() called - Force cleaning up {activeLeaves.Count} active leaves");
        
        int cleanedCount = 0;
        
        // Cancel any pending LeanTween animations on leaves
        foreach (GameObject leaf in activeLeaves)
        {
            if (leaf != null)
            {
                LeanTween.cancel(leaf);
                Destroy(leaf);
                cleanedCount++;
            }
        }

        activeLeaves.Clear();
        
        if (cleanedCount > 0)
        {
            //Debug.Log($"[SceneTransition] Force cleanup completed - {cleanedCount} leaves destroyed");
        }
    }

    void OnDestroy()
    {
        //Debug.Log("[SceneTransition] OnDestroy() called - Cleaning up SceneTransition");
        CleanupLeaves();
    }
}