using UnityEngine;

/// <summary>
/// 在指定区域内批量生成草丛实例，用于营造大片动态草地的效果。
/// 只负责外观，不和格子/建筑系统交互（避免复杂耦合）。
/// </summary>
public class GrassFieldSpawner : MonoBehaviour
{
    public static GrassFieldSpawner Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            Debugger.LogWarning("Multiple instances of GrassFieldSpawner detected. Destroying duplicate.");
            return;
        }
        Instance = this;
    }

    [Header("Area")]
    public Vector2 Size = new Vector2(40f, 40f); // XZ 平面上的宽高
    [Tooltip("是否根据 GridManager 的宽高自动覆盖整块可建造区域。")]
    public bool UseGridSize = true;
    public float YOffset = 0f;

    [Header("Density")]
    public int Instances = 800;
    [Tooltip("如果为 true，则按面积自动计算实例数量（Instances 将被覆盖）。")]
    public bool AutoDensity = true;
    public float DensityPerUnit = 0.4f; // 每平米多少丛草
    public int RandomSeed = 1234;

    [Header("Variation")]
    public float MinScale = 0.7f;
    public float MaxScale = 1.3f;

    [Header("Placement")]
    [Tooltip("从多高的位置往下发射射线，用于贴合地形表面。")]
    public float RaycastHeight = 20f;
    [Tooltip("哪些图层被认为是地面。留空则使用所有图层。")]
    public LayerMask GroundMask = ~0;

    private void Start()
    {
        // 现在的草系统准备重做，这里只负责清理场景中已有的草丛，不再自动生成新的。
        ClearChildren();
    }

    public void Generate()
    { 
        var rng = new System.Random(RandomSeed);

        int created = 0;
        int safety = Instances * 3; // 避免极端情况下死循环

        while (created < Instances && safety-- > 0)
        {
            Vector3 offsetXZ = new Vector3(
                (float)(rng.NextDouble() - 0.5) * Size.x,
                0f,
                (float)(rng.NextDouble() - 0.5) * Size.y
            );

            Vector3 rayOrigin = transform.position + offsetXZ + Vector3.up * RaycastHeight;

            if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, RaycastHeight * 2f, GroundMask))
            {
                // 没有打到任何地形，跳过这一点
                continue;
            }

            Vector3 spawnPos = hit.point + Vector3.up * YOffset;

            var go = new GameObject("GrassClump");
            go.transform.position = spawnPos;
            go.transform.SetParent(transform);

            float s = Mathf.Lerp(MinScale, MaxScale, (float)rng.NextDouble());
            go.transform.localScale = new Vector3(s, s, s);

            // 随机朝向，让草丛看起来更自然
            float angle = (float)(rng.NextDouble() * 360.0);
            go.transform.localRotation = Quaternion.Euler(0f, angle, 0f);

            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            go.AddComponent<GrassClumpGenerator>();

            created++;
        }
    }

    public void ClearChildren()
    {
        // 在编辑器和运行时安全地清理已有子物体
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
#if UNITY_EDITOR
            if (!Application.isPlaying)
                Object.DestroyImmediate(child.gameObject);
            else
                Destroy(child.gameObject);
#else
            Destroy(child.gameObject);
#endif
        }
    }
}

