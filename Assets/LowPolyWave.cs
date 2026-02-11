using UnityEngine;

public class LowPolyWater : MonoBehaviour
{
    [Header("波浪几何感设置")]
    public float waveHeight = 0.3f; // 浪高 (不要太高，低一点更精致)
    public float waveSpeed = 1.5f;  // 呼吸速度
    public float waveLength = 2.0f; // 波长 (控制波峰的间距)

    [Header("锐化 (关键参数)")]
    // 这个值越大，波浪越尖锐 (像山峰)
    // 这个值越小，波浪越圆润 (像馒头)
    // 推荐 1.0 - 2.0 之间
    public float sharpness = 1.0f;

    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] workingVertices;

    void Start()
    {
        MeshFilter filter = GetComponent<MeshFilter>();
        if (filter != null)
        {
            mesh = filter.mesh;

            // 关键：为了保证 Low Poly 的硬边效果，
            // 我们不希望 Unity 自动把顶点融合了。
            // 这里我们直接读取原始数据。
            originalVertices = mesh.vertices;
            workingVertices = new Vector3[originalVertices.Length];
        }
    }

    void Update()
    {
        if (originalVertices == null) return;

        // 获取当前时间，避免在循环里重复调用
        float time = Time.time * waveSpeed;

        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 vertex = originalVertices[i];

            // --- 几何波浪公式 (Gerstner-style simplified) ---

            // 我们做两个方向的波浪，让它们垂直交叉
            // 这样会形成整齐的“方格状”起伏，非常契合 Low Poly 的网格

            // 波浪 1：沿 X 轴
            float waveX = Mathf.Sin(vertex.x * waveLength + time);

            // 波浪 2：沿 Z 轴 (稍微错开一点相位，避免完全对称)
            float waveZ = Mathf.Sin(vertex.z * waveLength + time + 0.5f);

            // --- 核心魔法：锐化波峰 ---
            // 普通的 Sin 是圆的。我们把两个波浪加起来，然后根据 sharpness 进行乘法
            // 这会让波峰变得更“陡峭”，更有棱角感
            float finalWave = (waveX + waveZ) * 0.5f; // 先平均一下

            // 如果你想要那种“尖尖的浪”，可以用这个简单的数学技巧：
            // 也就是把波浪值稍微放大一点，或者保留符号平方
            if (sharpness > 1.0f)
            {
                // 这行代码会让正数更正，负数更负，从而拉伸波形
                finalWave = Mathf.Sign(finalWave) * Mathf.Pow(Mathf.Abs(finalWave), sharpness);
            }

            // 应用高度
            vertex.y += finalWave * waveHeight;

            workingVertices[i] = vertex;
        }

        mesh.vertices = workingVertices;

        // 这一步是 Low Poly 的灵魂：
        // 重新计算法线，让每一个面都随着波浪这种“硬”的运动反光
        mesh.RecalculateNormals();
    }
}