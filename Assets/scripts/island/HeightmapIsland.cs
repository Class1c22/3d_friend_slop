using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class HeightmapIsland : MonoBehaviour
{
    [Header("Сітка")]
    public int resolution = 100;
    public float worldSize = 40f; // розмір квадратної основи (борти/дно), у яку вписано круглу гору

    [Header("Форма гори")]
    [Tooltip("Радіус круглого острова. За замовчуванням = worldSize/2, тобто коло вписане в квадрат основи.")]
    public float islandRadius = -1f; // -1 = використати worldSize/2 автоматично
    public float peakHeight = 8f;    // висота вершини гори в центрі
    [Tooltip("Дрібні нерівності поверхні (пагорби), затухають до країв разом з горою")]
    public float noiseScale = 8f;
    public float noiseAmplitude = 1f;

    [Header("Вода")]
    public float seaLevel = 0f;

    private Mesh mesh;
    private Vector3[] vertices;
    private int topVertCount;

    void Start()
    {
        GenerateMesh();
    }

    void GenerateMesh()
    {
        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        GetComponent<MeshFilter>().mesh = mesh;

        int vertsPerSide = resolution + 1;
        topVertCount = vertsPerSide * vertsPerSide;
        int perimeterCount = resolution * 4; // без дублювання кутів
        float baseDepth = -20f; // наскільки глибоко "дно" острова
        float half = worldSize / 2f;
        float radius = islandRadius > 0f ? islandRadius : half;

        Vector3[] topVertices = new Vector3[topVertCount];
        Vector2[] topUVs = new Vector2[topVertCount];

        float step = worldSize / resolution;

        // Гора: висота максимальна в центрі (peakHeight) і плавно спадає до
        // seaLevel на відстані islandRadius від центру — це і дає круглий контур
        // острова (кути квадратної сітки опиняються під водою і не видно).
        for (int z = 0; z <= resolution; z++)
        {
            for (int x = 0; x <= resolution; x++)
            {
                int i = z * vertsPerSide + x;
                float px = x * step - half;
                float pz = z * step - half;
                float dist = Mathf.Sqrt(px * px + pz * pz);

                float shape = Mathf.Clamp01(1f - dist / radius);
                shape = shape * shape * (3f - 2f * shape); // smoothstep — плавний купол гори

                float noiseValue = Mathf.PerlinNoise(x / noiseScale, z / noiseScale) - 0.5f; // -0.5..0.5
                float height = seaLevel + shape * peakHeight + noiseValue * noiseAmplitude * shape;

                topVertices[i] = new Vector3(px, height, pz);
                topUVs[i] = new Vector2((float)x / resolution, (float)z / resolution);
            }
        }

        // Периметр верхньої сітки (за годинниковою стрілкою, без повтору кутів)
        int[] perimeterIndices = new int[perimeterCount];
        int p = 0;
        for (int x = 0; x < resolution; x++) perimeterIndices[p++] = 0 * vertsPerSide + x;
        for (int z = 0; z < resolution; z++) perimeterIndices[p++] = z * vertsPerSide + resolution;
        for (int x = resolution; x > 0; x--) perimeterIndices[p++] = resolution * vertsPerSide + x;
        for (int z = resolution; z > 0; z--) perimeterIndices[p++] = z * vertsPerSide + 0;

        // Дно — плоский суцільний квадрат (4 кутові вершини), приховане під водою за межами круглої гори.
        int bottomCornerCount = 4;
        int totalVerts = topVertCount + perimeterCount + bottomCornerCount;
        vertices = new Vector3[totalVerts];
        Vector2[] uvs = new Vector2[totalVerts];
        System.Array.Copy(topVertices, vertices, topVertCount);
        System.Array.Copy(topUVs, uvs, topVertCount);

        int bottomRingStart = topVertCount;
        for (int idx = 0; idx < perimeterCount; idx++)
        {
            Vector3 v = topVertices[perimeterIndices[idx]];
            vertices[bottomRingStart + idx] = new Vector3(v.x, baseDepth, v.z);
            uvs[bottomRingStart + idx] = Vector2.zero;
        }

        // Кути квадратного дна: BL, BR, TR, TL (0..3)
        int bottomCornersStart = bottomRingStart + perimeterCount;
        vertices[bottomCornersStart + 0] = new Vector3(-half, baseDepth, -half);
        vertices[bottomCornersStart + 1] = new Vector3(half, baseDepth, -half);
        vertices[bottomCornersStart + 2] = new Vector3(half, baseDepth, half);
        vertices[bottomCornersStart + 3] = new Vector3(-half, baseDepth, half);
        for (int i = 0; i < 4; i++) uvs[bottomCornersStart + i] = Vector2.zero;

        int topTriCount = resolution * resolution * 6;
        int wallTriCount = perimeterCount * 6;
        int bottomTriCount = 6; // всього 2 трикутники на плоский квадрат дна
        int[] triangles = new int[topTriCount + wallTriCount + bottomTriCount];
        int t = 0;

        // Верхня поверхня (гора)
        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = z * vertsPerSide + x;
                triangles[t++] = i;
                triangles[t++] = i + vertsPerSide;
                triangles[t++] = i + 1;
                triangles[t++] = i + 1;
                triangles[t++] = i + vertsPerSide;
                triangles[t++] = i + vertsPerSide + 1;
            }
        }

        // Бічні стінки периметра (приховані під водою за межами гори)
        for (int idx = 0; idx < perimeterCount; idx++)
        {
            int nextIdx = (idx + 1) % perimeterCount;
            int topA = perimeterIndices[idx];
            int topB = perimeterIndices[nextIdx];
            int botA = bottomRingStart + idx;
            int botB = bottomRingStart + nextIdx;

            triangles[t++] = topA; triangles[t++] = botA; triangles[t++] = topB;
            triangles[t++] = topB; triangles[t++] = botA; triangles[t++] = botB;
        }

        // Пласке квадратне дно (2 трикутники, нормаль вниз)
        int c00 = bottomCornersStart + 0;
        int c10 = bottomCornersStart + 1;
        int c11 = bottomCornersStart + 2;
        int c01 = bottomCornersStart + 3;

        triangles[t++] = c00; triangles[t++] = c10; triangles[t++] = c01;
        triangles[t++] = c10; triangles[t++] = c11; triangles[t++] = c01;

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    // Знаходить найближчу точку на контурі квадратної основи (в локальних координатах)
    // до заданої точки — так укус завжди "прилипає" до борту, а не всередину острова.
    private Vector3 GetNearestEdgePointLocal(Vector3 localPos)
    {
        float half = worldSize / 2f;
        float px = localPos.x;
        float pz = localPos.z;

        bool insideBox = px > -half && px < half && pz > -half && pz < half;

        if (!insideBox)
        {
            return new Vector3(Mathf.Clamp(px, -half, half), 0f, Mathf.Clamp(pz, -half, half));
        }

        float distRight = half - px;
        float distLeft = px + half;
        float distTop = half - pz;
        float distBottom = pz + half;
        float minDist = Mathf.Min(Mathf.Min(distRight, distLeft), Mathf.Min(distTop, distBottom));

        if (minDist == distRight) return new Vector3(half, 0f, pz);
        if (minDist == distLeft) return new Vector3(-half, 0f, pz);
        if (minDist == distTop) return new Vector3(px, 0f, half);
        return new Vector3(px, 0f, -half);
    }

    // Плавно опускає вершини верхньої поверхні в радіусі навколо найближчого борту
    // острова до worldBitePos, нижче рівня моря. Кругла форма — за рахунок
    // radial falloff (Vector2.Distance + SmoothStep) від точки укусу.
    public void BiteAt(Vector3 worldBitePos, float radius, float targetDepthBelowSea, float duration)
    {
        Vector3 localBitePos = transform.InverseTransformPoint(worldBitePos);
        Vector3 edgeLocalPos = GetNearestEdgePointLocal(localBitePos);
        Vector3 worldEdgePos = transform.TransformPoint(edgeLocalPos);

        StartCoroutine(BiteCoroutine(worldEdgePos, radius, targetDepthBelowSea, duration));
    }

    private IEnumerator BiteCoroutine(Vector3 worldBitePos, float radius, float targetDepthBelowSea, float duration)
    {
        Vector3 localBitePos = transform.InverseTransformPoint(worldBitePos);

        // Кусаємо лише верхню поверхню (гору) — стіни і дно не чіпаємо.
        float[] startHeights = new float[topVertCount];
        for (int i = 0; i < topVertCount; i++)
            startHeights[i] = vertices[i].y;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            for (int i = 0; i < topVertCount; i++)
            {
                float dist = Vector2.Distance(
                    new Vector2(vertices[i].x, vertices[i].z),
                    new Vector2(localBitePos.x, localBitePos.z)
                );

                if (dist > radius) continue;

                float falloff = 1f - Mathf.SmoothStep(0f, 1f, dist / radius);
                float targetY = seaLevel + targetDepthBelowSea;

                vertices[i].y = Mathf.Lerp(startHeights[i], targetY, falloff * t);
            }

            mesh.vertices = vertices;
            mesh.RecalculateNormals();

            MeshCollider col = GetComponent<MeshCollider>();
            col.sharedMesh = null;
            col.sharedMesh = mesh;

            yield return null;
        }
    }

    public float WorldSize => worldSize;
}