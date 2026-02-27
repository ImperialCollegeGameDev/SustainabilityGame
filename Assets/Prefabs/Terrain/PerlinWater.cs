using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class OceanGenerator : MonoBehaviour
{
    [Header("🌊 网格生成")]
    public int gridSize = 80;        // 网格密度 (越大越精细，也越卡)
    public float cellSize = 1.0f;    // 每个格子多大 (决定海面总大小)

    [Header("🌊 波浪 1 (大涌浪 - 决定形状)")]
    public float wave1Dir = 45f;     // 方向
    public float wave1Speed = 0.5f;
    public float wave1Length = 20f;
    public float wave1Height = 0.8f;

    [Header("🌊 波浪 2 (交叉浪 - 打破规律)")]
    public float wave2Dir = 135f;
    public float wave2Speed = 1.0f;
    public float wave2Length = 10f;
    public float wave2Height = 0.3f;

    [Header("🌊 波浪 3 (碎浪 - 增加细节)")]
    public float wave3Dir = 0f;
    public float wave3Speed = 1.5f;
    public float wave3Length = 4f;
    public float wave3Height = 0.15f;

    [Header("🎨 泡沫设置")]
    [Range(0f, 1f)]
    public float foamThreshold = 0.65f; // 超过这个高度显示泡沫
    [Range(0f, 1f)]
    public float sharpess = 0.2f;       // 浪尖尖锐度 (防止变线条，限制在 0.3 以下)

    private Mesh mesh;
    private Vector3[] baseVertices; // 原始平面位置
    private Vector3[] currentVertices; // 动起来后的位置
    private Color[] foamColors; // 存泡沫数据

    void Start()
    {
        GenerateMesh();
    }

    void Update()
    {
        if (mesh == null) return;
        UpdateWaves();
    }

    // 1. 生成网格 (不用改，既然这个能用就用这个)
    void GenerateMesh()
    {
        mesh = new Mesh();
        mesh.name = "ProceduralOcean";

        int totalVerts = gridSize * gridSize * 6;
        baseVertices = new Vector3[totalVerts];
        currentVertices = new Vector3[totalVerts];
        foamColors = new Color[totalVerts];
        int[] triangles = new int[totalVerts];

        int vIndex = 0;
        float offset = (gridSize * cellSize) / 2.0f;

        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                Vector3 p0 = new Vector3(x * cellSize - offset, 0, z * cellSize - offset);
                Vector3 p1 = new Vector3((x + 1) * cellSize - offset, 0, z * cellSize - offset);
                Vector3 p2 = new Vector3(x * cellSize - offset, 0, (z + 1) * cellSize - offset);
                Vector3 p3 = new Vector3((x + 1) * cellSize - offset, 0, (z + 1) * cellSize - offset);

                // 三角形 1
                baseVertices[vIndex + 0] = p0;
                baseVertices[vIndex + 1] = p2;
                baseVertices[vIndex + 2] = p1;
                triangles[vIndex + 0] = vIndex + 0;
                triangles[vIndex + 1] = vIndex + 1;
                triangles[vIndex + 2] = vIndex + 2;

                // 三角形 2
                baseVertices[vIndex + 3] = p1;
                baseVertices[vIndex + 4] = p2;
                baseVertices[vIndex + 5] = p3;
                triangles[vIndex + 3] = vIndex + 3;
                triangles[vIndex + 4] = vIndex + 4;
                triangles[vIndex + 5] = vIndex + 5;

                vIndex += 6;
            }
        }

        mesh.vertices = baseVertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    // 2. 更新波浪 (多层 Gerstner 叠加)
    void UpdateWaves()
    {
        float time = Time.time;
        float maxPossibleHeight = wave1Height + wave2Height + wave3Height;

        for (int i = 0; i < baseVertices.Length; i++)
        {
            Vector3 v = baseVertices[i]; // 拿原始位置
            Vector3 displacement = Vector3.zero;

            // 叠加三层波浪
            displacement += CalculateGerstner(v, wave1Dir, wave1Speed, wave1Length, wave1Height, time);
            displacement += CalculateGerstner(v, wave2Dir, wave2Speed, wave2Length, wave2Height, time);
            displacement += CalculateGerstner(v, wave3Dir, wave3Speed, wave3Length, wave3Height, time);

            // 应用位置 (原始 + 偏移)
            currentVertices[i] = v + displacement;

            // --- 计算泡沫 ---
            // 逻辑：如果当前波浪高度很高，或者是浪尖(Y值大)，就显示泡沫
            // 归一化高度 (0 到 1)
            float normalizedHeight = Mathf.Clamp01((displacement.y + maxPossibleHeight) / (maxPossibleHeight * 2f));

            float foam = 0f;
            if (normalizedHeight > foamThreshold)
            {
                // 超过阈值，产生泡沫。用 (当前高度 - 阈值) 算强度
                foam = (normalizedHeight - foamThreshold) / (1.0f - foamThreshold);
            }

            // 存入顶点颜色 R 通道
            foamColors[i] = new Color(foam, foam, foam, 1);
        }

        mesh.vertices = currentVertices;
        mesh.colors = foamColors; // 把泡沫数据传给 Shader
        mesh.RecalculateNormals(); // 必须重算，才有 Low Poly 棱角
        mesh.RecalculateBounds();
    }

    Vector3 CalculateGerstner(Vector3 p, float angle, float speed, float length, float height, float time)
    {
        float k = 2 * Mathf.PI / length;
        float c = Mathf.Sqrt(9.8f / k) * speed;
        float rad = angle * Mathf.Deg2Rad;
        Vector2 d = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

        float f = k * (d.x * p.x + d.y * p.z - c * time);

        // 这里的 sharpess 是水平位移量。太大会导致模型变线条，所以我在上面限制了它的范围。
        float a = sharpess / k;

        return new Vector3(
            d.x * (a * Mathf.Cos(f)),
            height * Mathf.Sin(f),
            d.y * (a * Mathf.Cos(f))
        );
    }
}