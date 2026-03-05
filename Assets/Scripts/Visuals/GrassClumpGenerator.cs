using UnityEngine;

/// <summary>
/// 简单的草丛网格生成器：在运行时生成交叉的草卡片 Mesh，
/// 并自动绑定 URP/Environment/GrassCards 材质（如果还没有材质的话）。
/// 把这个脚本挂在一个空物体上即可作为草丛 prefab 使用。
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class GrassClumpGenerator : MonoBehaviour
{
    [Header("Shape")]
    public float Width = 0.45f;
    public float Height = 1.1f;

    private void Awake()
    {
        var meshFilter = GetComponent<MeshFilter>();
        var meshRenderer = GetComponent<MeshRenderer>();

        if (meshFilter.sharedMesh == null)
        {
            meshFilter.sharedMesh = CreateTripleQuadMesh();
        }

        if (meshRenderer.sharedMaterial == null)
        {
            var shader = Shader.Find("URP/Environment/GrassCards");
            if (shader != null)
            {
                var mat = new Material(shader);
                meshRenderer.sharedMaterial = mat;
            }
        }
    }

    private Mesh CreateTripleQuadMesh()
    {
        var mesh = new Mesh();

        float halfW = Width * 0.5f;
        float h = Height;

        // 三个交叉的 quad，形成更饱满的一丛草
        Vector3[] vertices =
        {
            // Quad 1 (0°)
            new Vector3(-halfW, 0f, 0f), // 0
            new Vector3( halfW, 0f, 0f), // 1
            new Vector3(-halfW,    h, 0f), // 2
            new Vector3( halfW,    h, 0f), // 3

            // Quad 2 (60°)
            new Vector3(-halfW * 0.5f, 0f, -halfW * 0.866f), // 4
            new Vector3( halfW * 0.5f, 0f,  halfW * 0.866f), // 5
            new Vector3(-halfW * 0.5f,    h, -halfW * 0.866f), // 6
            new Vector3( halfW * 0.5f,    h,  halfW * 0.866f), // 7

            // Quad 3 (120°)
            new Vector3(-halfW * 0.5f, 0f,  halfW * 0.866f), // 8
            new Vector3( halfW * 0.5f, 0f, -halfW * 0.866f), // 9
            new Vector3(-halfW * 0.5f,    h,  halfW * 0.866f), // 10
            new Vector3( halfW * 0.5f,    h, -halfW * 0.866f)  // 11
        };

        int[] triangles =
        {
            // Quad 1
            0, 2, 1,
            1, 2, 3,

            // Quad 2
            4, 6, 5,
            5, 6, 7,

            // Quad 3
            8, 10, 9,
            9, 10, 11
        };

        Vector2[] uvs =
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),

            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),

            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}

