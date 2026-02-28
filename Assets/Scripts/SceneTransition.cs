using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Handles scene transitions with falling leaf animations.
/// Singleton class that persists across scenes and manages LeanTween animations properly.
/// </summary>
public class SceneTransition : MonoBehaviour
{
    public static SceneTransition i { get; private set; }

    [SerializeField] public List<Sprite> leafAssets;

    [Header("Leaf Animation Settings")]
    [SerializeField] private int leafCount = 150;
    [SerializeField] private float leafFallTimeMin = 1.0f;
    [SerializeField] private float leafFallTimeMax = 2.0f;
    [SerializeField] private float leafRotationSpeedMin = 50f;
    [SerializeField] private float leafRotationSpeedMax = 200f;
    [SerializeField] private float leafHorizontalDriftMin = -50f;
    [SerializeField] private float leafHorizontalDriftMax = 50f;
    [SerializeField] private Vector2 leafScaleRange = new Vector2(1.2f, 1.4f);
    
    [Header("Spawn Settings")]
    [SerializeField] private int spawnBatchSize = 15;
    [SerializeField] private float spawnBatchDelay = 0.1f;
    [SerializeField] private float leafBaseSize = 50f; // Base size in pixels before scale is applied

    private Canvas mainCanvas;
    private readonly List<GameObject> activeLeaves = new List<GameObject>();
    private readonly Dictionary<GameObject, int[]> leafTweenIds = new Dictionary<GameObject, int[]>();
    private bool isTransitioning = false;

    void Awake()
    {
        LeanTween.init(1500);
    }

    void Start()
    {
        if (i != null && i != this)
        {
            Debug.LogWarning("[SceneTransition] Another instance detected. Destroying the new one.");
            Destroy(i.gameObject);
        }
        
        i = this;
        
        SceneManager.sceneLoaded += OnSceneLoaded;
        SetupCanvas();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (i == this)
        {
            i = null;
            CancelAllLeafAnimations();
        }
    }

    private void SetupCanvas()
    {
        mainCanvas = GetComponent<Canvas>();
        if (mainCanvas == null)
        {
            Debug.LogError("[SceneTransition] No Canvas component found!");
            return;
        }
        
        // Use ScreenSpaceOverlay so canvas is independent of scene cameras
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mainCanvas.sortingOrder = 12000;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Canvas is in ScreenSpaceOverlay mode, so no camera updates needed
        // Leaves will continue animating across scene loads
    }

    #region Public API

    /// <summary>
    /// Plays the transition animation without changing scenes.
    /// </summary>
    public void PlayAnimation()
    {
        StartCoroutine(AnimationSameScene());
    }

    /// <summary>
    /// Transitions to the specified scene with leaf animation.
    /// </summary>
    public void SendToScene(string sceneName)
    {
        if (isTransitioning)
        {
            CancelAllLeafAnimations();
        }

        StartCoroutine(LoadSceneWithLeaves(sceneName));
        
    }

    #endregion

    #region Scene Loading

    private IEnumerator AnimationSameScene()
    {
        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("Exit");
            yield return new WaitForSeconds(1.25f);
            animator.SetTrigger("Enter");
        }
    }

    private IEnumerator LoadSceneWithLeaves(string sceneName)
    {
        isTransitioning = true;
        
        // Start spawning leaves
        StartCoroutine(SpawnLeavesInBatches());
        
        // Immediately start loading scene in background (but don't activate yet)
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false; // Load but don't switch yet
        
        yield return new WaitForSecondsRealtime(1.5f);
        
        // Activate the scene (instant since it's already loaded)
        asyncLoad.allowSceneActivation = true;
        
        // Wait for scene activation and leaves to finish
        float timeout = Time.realtimeSinceStartup + 10f;
        while ((!asyncLoad.isDone || activeLeaves.Count > 0) && Time.realtimeSinceStartup < timeout)
        {
            yield return null;
        }
        
        // Force cleanup if timeout reached
        if (activeLeaves.Count > 0)
        {
            CancelAllLeafAnimations();
        }
        
        isTransitioning = false;
    }

    #endregion

    #region Leaf Animation

    private IEnumerator SpawnLeavesInBatches()
    {
        if (mainCanvas == null || leafAssets == null || leafAssets.Count == 0)
        {
            yield break;
        }

        CancelAllLeafAnimations();

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        int batchCount = Mathf.CeilToInt((float)leafCount / spawnBatchSize);
        for (int batch = 0; batch < batchCount; batch++)
        {
            int startIdx = batch * spawnBatchSize;
            int endIdx = Mathf.Min(startIdx + spawnBatchSize, leafCount);

            for (int i = startIdx; i < endIdx; i++)
            {
                SpawnLeaf(screenWidth, screenHeight);
            }

            if (batch < batchCount - 1)
            {
                yield return new WaitForSecondsRealtime(spawnBatchDelay);
            }
        }
    }

    private void SpawnLeaf(float screenWidth, float screenHeight)
    {
        GameObject leafObj = new GameObject("Leaf");
        leafObj.transform.SetParent(mainCanvas.transform, false);

        Image leafImage = leafObj.AddComponent<Image>();
        leafImage.sprite = GetRandomLeafSprite();
        leafImage.preserveAspect = true;

        RectTransform rectTransform = leafObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(leafBaseSize, leafBaseSize); // Set base size first
        
        float scale = Random.Range(leafScaleRange.x, leafScaleRange.y);
        leafObj.transform.localScale = Vector3.one * scale;
        
        float startX = Random.Range(-50f, screenWidth + 50f);
        float startY = screenHeight + Random.Range(50f, 90f);
        rectTransform.anchoredPosition = new Vector2(startX, startY);

        activeLeaves.Add(leafObj);
        AnimateLeaf(leafObj, startX);
    }

    private void AnimateLeaf(GameObject leafObj, float startX)
    {
        RectTransform rectTransform = leafObj.GetComponent<RectTransform>();

        float fallTime = Random.Range(leafFallTimeMin, leafFallTimeMax);
        float horizontalDrift = Random.Range(leafHorizontalDriftMin, leafHorizontalDriftMax);
        float endX = startX + horizontalDrift;
        float endY = -500f;
        float rotationSpeed = Random.Range(leafRotationSpeedMin, leafRotationSpeedMax);
        if (Random.value > 0.5f) rotationSpeed *= -1;

        int[] tweenIds = new int[2];

        tweenIds[0] = LeanTween.move(rectTransform, new Vector3(endX, endY, 0), fallTime)
            .setEase(LeanTweenType.easeInQuad)
            .setIgnoreTimeScale(true)
            .setOnComplete(() => OnLeafComplete(leafObj))
            .id;

        tweenIds[1] = LeanTween.rotateAround(leafObj, Vector3.forward, rotationSpeed * fallTime, fallTime)
            .setEase(LeanTweenType.linear)
            .setIgnoreTimeScale(true)
            .id;

        leafTweenIds[leafObj] = tweenIds;
    }

    private void OnLeafComplete(GameObject leafObj)
    {
        if (leafObj == null) return;

        if (leafTweenIds.TryGetValue(leafObj, out int[] tweenIds))
        {
            foreach (int id in tweenIds)
            {
                LeanTween.cancel(id);
            }
            leafTweenIds.Remove(leafObj);
        }

        activeLeaves.Remove(leafObj);
        Destroy(leafObj);
    }

    private void CancelAllLeafAnimations()
    {
        foreach (var kvp in leafTweenIds)
        {
            foreach (int id in kvp.Value)
            {
                LeanTween.cancel(id);
            }
        }
        leafTweenIds.Clear();

        foreach (GameObject leaf in activeLeaves)
        {
            if (leaf != null)
            {
                Destroy(leaf);
            }
        }
        activeLeaves.Clear();

        isTransitioning = false;
    }

    private Sprite GetRandomLeafSprite()
    {
        if (leafAssets == null || leafAssets.Count == 0)
        {
            return null;
        }
        return leafAssets[Random.Range(0, leafAssets.Count)];
    }

    #endregion
}